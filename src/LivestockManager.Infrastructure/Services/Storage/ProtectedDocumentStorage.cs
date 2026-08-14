using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
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
            ? "./App_Data/ProtectedDocuments"
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
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Company ID is required.");

        if (content == null)
            throw new ArgumentNullException(nameof(content));
        if (maxSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSizeBytes), "Max size must be positive.");
        if (allowedExtensions == null || allowedExtensions.Length == 0)
            throw new ArgumentException("At least one allowed extension must be provided.", nameof(allowedExtensions));

        var canonicalMax = ProtectedFileUploadValidator.MaxFileSizeBytes;
        var enforcedMax = Math.Min(maxSizeBytes, canonicalMax);

        if (content.CanSeek)
        {
            if (content.Length <= 0)
                throw new InvalidOperationException("Empty / zero-byte files are not allowed.");
            if (content.Length > enforcedMax)
                throw new InvalidOperationException($"File exceeds maximum allowed size of {enforcedMax} bytes.");
        }

        var serverAllowList = ProtectedFileUploadValidator.AllowedExtensions;
        foreach (var ext in allowedExtensions)
        {
            if (!serverAllowList.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Storage allow-list contains extension '{ext}' which is not in the canonical server allow-list.");
            }
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
                headerRead = await ReadBufferedWithMax(content, tempCopy, headerBuffer, enforcedMax, ct);
            }

            totalBytesRead = tempCopy.Length;

            if (totalBytesRead <= 0)
                throw new InvalidOperationException("Empty / zero-byte files are not allowed.");
            if (totalBytesRead > enforcedMax)
                throw new InvalidOperationException($"File exceeds maximum allowed size of {enforcedMax} bytes (read {totalBytesRead}).");
        }
        finally
        {
            if (content.CanSeek)
                content.Position = 0;
        }

        await VerifyMagicBytesEnhanced(safeExt, headerBuffer, (int)Math.Min(16, totalBytesRead), tempCopy, ct);

        var now = DateTimeOffset.UtcNow;
        var companySub = $"{companyId:N}";
        var subFolder = Path.Combine(_rootFolder, companySub);
        var directoryInfo = Directory.CreateDirectory(subFolder);

        var internalFileName = $"{Guid.NewGuid():N}.{safeExt}";
        if (ProtectedFileUploadValidator.ContainsPathTraversal(internalFileName))
            throw new InvalidOperationException("Invalid stored file name: path traversal detected.");
        if (ProtectedFileUploadValidator.ContainsInvalidFilenameChars(internalFileName))
            throw new InvalidOperationException("Invalid stored file name: invalid characters.");

        var fullPath = Path.GetFullPath(Path.Combine(subFolder, internalFileName));
        var safeInternalPath = NormalizeInternalPath(fullPath);

        var finalDir = Path.GetFullPath(Path.GetDirectoryName(fullPath)!);
        var resolvedRoot = Path.GetFullPath(_rootFolder);
        if (!finalDir.StartsWith(resolvedRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path: attempted path traversal detected.");

        var expectedCompanyDir = Path.Combine(resolvedRoot, companySub);
        var resolvedCompanyDir = Path.GetFullPath(expectedCompanyDir);
        if (!finalDir.StartsWith(resolvedCompanyDir, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage path: file must be stored under company-specific folder.");

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
        if (companyId == Guid.Empty)
            throw new UnauthorizedAccessException("Company ID is required.");

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

        var expectedCompanyDir = Path.Combine(resolvedRoot, $"{companyId:N}");
        var resolvedCompanyDir = Path.GetFullPath(expectedCompanyDir);
        if (!filePathDir.StartsWith(resolvedCompanyDir, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Document storage path violates company isolation.");

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

    public static string SanitizeDisplayName(string input)
    {
        return ProtectedFileUploadValidator.SanitizeDisplayName(input);
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

    private static async Task VerifyMagicBytesEnhanced(string safeExt, byte[] header, int headerLength, MemoryStream fullContent, CancellationToken ct)
    {
        if (headerLength <= 0)
            throw new InvalidOperationException("File is empty.");

        if (headerLength >= 2 && header[0] == 0x4D && header[1] == 0x5A)
        {
            bool isZipExt = safeExt == "docx" || safeExt == "xlsx";
            if (!isZipExt)
            {
                throw new InvalidOperationException("File signature matches executable (MZ header) which is not allowed.");
            }
        }

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
                if (headerLength < 3 ||
                    header[0] != 0xFF ||
                    header[1] != 0xD8 ||
                    header[2] != 0xFF)
                {
                    throw new InvalidOperationException("File signature does not match extension (.jpg/.jpeg).");
                }
                if (fullContent.Length > 2)
                {
                    fullContent.Position = fullContent.Length - 2;
                    byte[] trailer = new byte[2];
                    int tr = await fullContent.ReadAsync(trailer, 0, 2, ct);
                    if (tr < 2 || trailer[0] != 0xFF || trailer[1] != 0xD9)
                    {
                        throw new InvalidOperationException("JPEG/JPG file missing valid end-of-image (FF D9) marker.");
                    }
                    fullContent.Position = 0;
                }
                break;

            case "png":
                if (headerLength < 8 ||
                    header[0] != 0x89 ||
                    header[1] != 0x50 ||
                    header[2] != 0x4E ||
                    header[3] != 0x47 ||
                    header[4] != 0x0D ||
                    header[5] != 0x0A ||
                    header[6] != 0x1A ||
                    header[7] != 0x0A)
                {
                    throw new InvalidOperationException("File signature does not match extension (.png).");
                }
                break;

            case "doc":
            case "xls":
                if (headerLength < 8 ||
                    header[0] != 0xD0 ||
                    header[1] != 0xCF ||
                    header[2] != 0x11 ||
                    header[3] != 0xE0 ||
                    header[4] != 0xA1 ||
                    header[5] != 0xB1 ||
                    header[6] != 0x1A ||
                    header[7] != 0xE1)
                {
                    throw new InvalidOperationException("File signature does not match OLE Compound Document (.doc/.xls).");
                }
                break;

            case "docx":
            case "xlsx":
                if (headerLength < 4 ||
                    header[0] != 0x50 ||
                    header[1] != 0x4B ||
                    header[2] != 0x03 ||
                    header[3] != 0x04)
                {
                    throw new InvalidOperationException("File signature does not match Office Open XML (.docx/.xlsx requires ZIP PK header).");
                }
                await ValidateOOXMLInternal(fullContent, ct);
                break;

            case "csv":
                await ValidateCsvInternal(fullContent, ct);
                break;

            default:
                break;
        }
    }

    private static Task ValidateOOXMLInternal(MemoryStream stream, CancellationToken ct)
    {
        try
        {
            stream.Position = 0;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            bool found = false;
            int entriesChecked = 0;
            long totalRead = 0;
            const long maxBytes = 1024 * 1024;
            const int maxEntries = 256;

            foreach (var entry in zip.Entries)
            {
                entriesChecked++;
                if (entriesChecked > maxEntries || totalRead > maxBytes)
                    break;
                totalRead += entry.Length;
                if (entry.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException("OOXML package missing [Content_Types].xml.");

            return Task.CompletedTask;
        }
        catch (InvalidDataException)
        {
            throw new InvalidOperationException("Invalid ZIP format for Office Open XML file.");
        }
    }

    private static async Task ValidateCsvInternal(MemoryStream stream, CancellationToken ct)
    {
        stream.Position = 0;
        byte[] buf = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buf, 0, buf.Length, ct)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                if (buf[i] == 0x00)
                    throw new InvalidOperationException("CSV contains NUL bytes.");
            }
        }
    }

    private static string MapContentType(string safeExt)
    {
        return safeExt.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "csv" => "text/csv",
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
