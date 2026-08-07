using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Services.Storage;

public class ProtectedDocumentStorage : IProtectedDocumentStorage
{
    private readonly AppDbContext _dbContext;
    private readonly string _rootFolder;

    public ProtectedDocumentStorage(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;

        var configuredRoot = configuration?["ProtectedStorage:Root"];
        var relativeRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? "./App_Data/Documents"
            : configuredRoot.Trim();

        var combined = Path.Combine(AppContext.BaseDirectory, relativeRoot);
        _rootFolder = Path.GetFullPath(combined);

        if (!_rootFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            _rootFolder += Path.DirectorySeparatorChar;
    }

    public async Task<(Guid DocumentId, string SafeInternalPath, long SizeBytes)> StoreAsync(
        Guid companyId, string sanitizedDisplayName, Stream content, DocumentType allowedType,
        long maxSizeBytes, string[] allowedExtensions, Guid? uploadedByUserId, CancellationToken ct)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (maxSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSizeBytes), "Max size must be positive.");
        if (allowedExtensions == null || allowedExtensions.Length == 0)
            throw new ArgumentException("At least one allowed extension must be provided.", nameof(allowedExtensions));

        if (content.CanSeek)
        {
            if (content.Length > maxSizeBytes)
                throw new InvalidOperationException($"File exceeds maximum allowed size of {maxSizeBytes} bytes.");
        }

        var sanitizedName = SanitizeDisplayName(sanitizedDisplayName);
        var (safeExt, originalExt) = ValidateAndNormalizeExtension(sanitizedName, allowedExtensions);

        byte[] headerBuffer;
        long totalBytesRead;
        var tempCopy = new MemoryStream();

        try
        {
            headerBuffer = new byte[16];
            int headerRead = 0;
            if (content.CanSeek)
            {
                var savedPos = content.Position;
                content.Position = 0;
                headerRead = await content.ReadAsync(headerBuffer, 0, headerBuffer.Length, ct);
                content.Position = 0;
                await content.CopyToAsync(tempCopy, 81920, ct);
            }
            else
            {
                headerRead = await ReadBufferedWithMax(content, tempCopy, headerBuffer, maxSizeBytes, ct);
            }

            totalBytesRead = tempCopy.Length;

            if (totalBytesRead > maxSizeBytes)
                throw new InvalidOperationException($"File exceeds maximum allowed size of {maxSizeBytes} bytes (read {totalBytesRead}).");
        }
        finally
        {
            if (content.CanSeek)
                content.Position = 0;
        }

        VerifyMagicBytes(safeExt, headerBuffer, (int)Math.Min(16, totalBytesRead));

        var now = DateTimeOffset.UtcNow;
        var monthSub = $"{now:yyyy-MM}";
        var companySub = $"{companyId:N}";
        var subFolder = Path.Combine(_rootFolder, companySub, monthSub);
        var directoryInfo = Directory.CreateDirectory(subFolder);

        var internalFileName = $"{Guid.NewGuid():N}.{safeExt}";
        var fullPath = Path.GetFullPath(Path.Combine(subFolder, internalFileName));
        var safeInternalPath = NormalizeInternalPath(fullPath);

        var finalDir = Path.GetFullPath(Path.GetDirectoryName(fullPath)!);
        var resolvedRoot = Path.GetFullPath(_rootFolder);
        if (!finalDir.StartsWith(resolvedRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path: attempted path traversal detected.");

        tempCopy.Position = 0;
        using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await tempCopy.CopyToAsync(fs, 81920, ct);
            await fs.FlushAsync(ct);
        }

        var actualSize = new FileInfo(fullPath).Length;

        var contentType = MapContentType(safeExt);
        var document = new Document(companyId, sanitizedName, safeInternalPath, allowedType, now)
        {
            ContentType = contentType,
            SizeBytes = actualSize,
            Extension = safeExt,
            UploadedByUserId = uploadedByUserId
        };

        _dbContext.Set<Document>().Add(document);
        await _dbContext.SaveChangesAsync(ct);

        return (document.Id, safeInternalPath, actualSize);
    }

    public async Task<(Stream FileStream, Document Metadata)> DownloadAsync(
        Guid documentId, Guid companyId, CancellationToken ct)
    {
        var document = await _dbContext.Set<Document>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct);

        if (document == null)
            throw new InvalidOperationException("Document not found.");

        if (document.CompanyId != companyId)
            throw new UnauthorizedAccessException("Document does not belong to the requested company.");

        var resolvedRoot = Path.GetFullPath(_rootFolder);
        var fullPath = Path.GetFullPath(ResolveFullPath(document.StoragePath));
        var filePathDir = Path.GetDirectoryName(fullPath)!;

        if (!fullPath.StartsWith(resolvedRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path: attempted path traversal detected.");

        if (!File.Exists(fullPath))
            throw new InvalidOperationException("Document file not found on storage.");

        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return (fileStream, document);
    }

    private static async Task<int> ReadBufferedWithMax(
        Stream source, Stream destination, byte[] headerBuffer,
        long maxSizeBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long total = 0;
        int headerFilled = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            total += bytesRead;
            if (total > maxSizeBytes)
                throw new InvalidOperationException($"File exceeds maximum allowed size of {maxSizeBytes} bytes.");

            if (headerFilled < headerBuffer.Length)
            {
                var toCopy = Math.Min(bytesRead, headerBuffer.Length - headerFilled);
                Array.Copy(buffer, 0, headerBuffer, headerFilled, toCopy);
                headerFilled += toCopy;
            }

            await destination.WriteAsync(buffer, 0, bytesRead, ct);
        }

        return headerFilled;
    }

    private static string SanitizeDisplayName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Display name cannot be empty.", nameof(input));

        var invalid = Path.GetInvalidFileNameChars();
        var chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '-';
        }

        var sanitized = new string(chars);
        sanitized = Path.GetFileName(sanitized);
        sanitized = sanitized.Trim();

        while (sanitized.Length > 0 && (sanitized[0] == '.' || sanitized[0] == ' '))
            sanitized = sanitized[1..];
        while (sanitized.Length > 0 && (sanitized[^1] == '.' || sanitized[^1] == ' '))
            sanitized = sanitized[..^1];

        if (sanitized.Length > 255)
            sanitized = sanitized[..255];

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Display name is invalid after sanitization.", nameof(input));

        return sanitized;
    }

    private static (string SafeExt, string OriginalExt) ValidateAndNormalizeExtension(
        string sanitizedDisplayName, string[] allowedExtensions)
    {
        var originalExt = Path.GetExtension(sanitizedDisplayName) ?? string.Empty;
        var trimmedExt = originalExt.TrimStart('.').Trim();

        if (string.IsNullOrWhiteSpace(trimmedExt))
            throw new InvalidOperationException("File extension is required.");

        string? matched = null;
        foreach (var allowed in allowedExtensions)
        {
            var a = allowed.Trim().TrimStart('.');
            if (string.Equals(a, trimmedExt, StringComparison.InvariantCultureIgnoreCase))
            {
                matched = a.ToLowerInvariant();
                break;
            }
        }

        if (matched == null)
            throw new InvalidOperationException($"File extension '.{trimmedExt}' is not in the allowed list.");

        return (matched, originalExt);
    }

    private static void VerifyMagicBytes(string safeExt, byte[] header, int headerLength)
    {
        if (headerLength <= 0)
            throw new InvalidOperationException("File is empty.");

        switch (safeExt.ToLowerInvariant())
        {
            case "pdf":
                if (headerLength < 4 ||
                    header[0] != 0x25 ||
                    header[1] != 0x50 ||
                    header[2] != 0x44 ||
                    header[3] != 0x46)
                {
                    throw new InvalidOperationException("File signature does not match extension (.pdf requires %PDF header).");
                }
                break;

            case "jpg":
            case "jpeg":
                if (headerLength < 2 ||
                    header[0] != 0xFF ||
                    header[1] != 0xD8)
                {
                    throw new InvalidOperationException("File signature does not match extension (.jpg/.jpeg).");
                }
                break;

            case "png":
                if (headerLength < 8 ||
                    header[0] != 137 ||
                    header[1] != 80 ||
                    header[2] != 78 ||
                    header[3] != 71 ||
                    header[4] != 13 ||
                    header[5] != 10 ||
                    header[6] != 26 ||
                    header[7] != 10)
                {
                    throw new InvalidOperationException("File signature does not match extension (.png).");
                }
                break;

            case "gif":
                if (headerLength < 3 ||
                    header[0] != 0x47 ||
                    header[1] != 0x49 ||
                    header[2] != 0x46)
                {
                    throw new InvalidOperationException("File signature does not match extension (.gif).");
                }
                break;

            case "txt":
                break;

            default:
                break;
        }
    }

    private static string MapContentType(string safeExt)
    {
        return safeExt.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private string NormalizeInternalPath(string fullPath)
    {
        var resolvedRoot = Path.GetFullPath(_rootFolder);
        var resolvedFull = Path.GetFullPath(fullPath);
        if (resolvedFull.StartsWith(resolvedRoot, StringComparison.Ordinal))
        {
            var relative = resolvedFull[resolvedRoot.Length..];
            if (relative.StartsWith(Path.DirectorySeparatorChar.ToString()))
                relative = relative[1..];
            return relative.Replace('\\', '/');
        }
        return resolvedFull.Replace('\\', '/');
    }

    private string ResolveFullPath(string internalPath)
    {
        var slashNormalized = internalPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(_rootFolder, slashNormalized));
    }
}
