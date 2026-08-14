using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using System.Text;

namespace LivestockManager.Infrastructure.Services.Storage;

public class ProtectedFileUploadValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NormalizedExtension { get; set; }
    public string? SanitizedDisplayName { get; set; }
    public string? ProposedStoredPath { get; set; }
    public string? StoredFileName { get; set; }
}

public static class ProtectedFileUploadValidator
{
    public static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".csv" };
    public const long MaxFileSizeBytes = 26_214_400;
    private const string ProtectedDocsRootRelative = "App_Data/ProtectedDocuments";

    public static async Task<ProtectedFileUploadValidationResult> ValidateUploadAsync(
        IFormFile file,
        Guid companyId,
        CancellationToken ct)
    {
        var result = new ProtectedFileUploadValidationResult();

        if (companyId == Guid.Empty)
        {
            result.IsValid = false;
            result.ErrorMessage = "Company ID is required for document upload.";
            return result;
        }

        if (file == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "File cannot be null.";
            return result;
        }

        if (file.Length == 0)
        {
            result.IsValid = false;
            result.ErrorMessage = "Empty files are not allowed.";
            return result;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.ErrorMessage = $"File exceeds maximum allowed size of {MaxFileSizeBytes} bytes (25 MB).";
            return result;
        }

        var originalName = file.FileName;
        if (string.IsNullOrWhiteSpace(originalName))
        {
            result.IsValid = false;
            result.ErrorMessage = "Filename is required.";
            return result;
        }

        if (ContainsInvalidFilenameChars(originalName))
        {
            result.IsValid = false;
            result.ErrorMessage = "Filename contains invalid characters.";
            return result;
        }

        if (ContainsPathTraversal(originalName))
        {
            result.IsValid = false;
            result.ErrorMessage = "Path traversal detected in filename.";
            return result;
        }

        var sanitizedName = SanitizeDisplayName(originalName);
        result.SanitizedDisplayName = sanitizedName;

        var ext = Path.GetExtension(sanitizedName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ext))
        {
            result.IsValid = false;
            result.ErrorMessage = "File extension is required.";
            return result;
        }

        var normalizedExt = ext.ToLowerInvariant();
        if (!AllowedExtensions.Contains(normalizedExt, StringComparer.OrdinalIgnoreCase))
        {
            result.IsValid = false;
            result.ErrorMessage = $"File extension '{normalizedExt}' is not allowed.";
            return result;
        }

        result.NormalizedExtension = normalizedExt;

        using var stream = file.OpenReadStream();
        var magicCheck = await ValidateMagicBytesAndContentAsync(stream, normalizedExt, ct);
        if (!magicCheck.IsValid)
        {
            result.IsValid = false;
            result.ErrorMessage = magicCheck.ErrorMessage;
            return result;
        }

        var storedFileName = Guid.NewGuid().ToString("N") + normalizedExt;
        result.StoredFileName = storedFileName;

        var relativeCompanyPath = Path.Combine(ProtectedDocsRootRelative, companyId.ToString("N"));
        var proposedStoredPath = Path.Combine(relativeCompanyPath, storedFileName).Replace('\\', '/');
        result.ProposedStoredPath = proposedStoredPath;

        result.IsValid = true;
        return result;
    }

    public static bool ContainsInvalidFilenameChars(string name)
    {
        if (string.IsNullOrEmpty(name))
            return true;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c <= '\u001F')
                return true;

            switch (c)
            {
                case '<':
                case '>':
                case '"':
                case '|':
                case '?':
                case '*':
                    return true;
                case ':':
                    if (i != 1 || name.Length < 3 || !char.IsLetter(name[0]) || name[2] != '\\' && name[2] != '/')
                        return true;
                    break;
            }
        }

        return false;
    }

    public static bool ContainsPathTraversal(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Contains("../", StringComparison.Ordinal) ||
            name.Contains("..\\", StringComparison.Ordinal))
            return true;

        var fileName = Path.GetFileName(name);
        if (!string.Equals(fileName, name, StringComparison.Ordinal))
            return true;

        return false;
    }

    public static string SanitizeDisplayName(string input)
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

    public static async Task<(bool IsValid, string? ErrorMessage)> ValidateMagicBytesAndContentAsync(
        Stream stream,
        string normalizedExtension,
        CancellationToken ct)
    {
        if (stream == null)
            return (false, "Stream cannot be null.");

        if (!stream.CanSeek)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;
            stream = ms;
        }
        else
        {
            stream.Position = 0;
        }

        byte[] header16 = new byte[16];
        int headerRead = await stream.ReadAsync(header16, 0, 16, ct);
        stream.Position = 0;

        if (headerRead < 2)
            return (false, "File too small to validate signature.");

        if (headerRead >= 2 && header16[0] == 0x4D && header16[1] == 0x5A)
        {
            bool isZipExtension = normalizedExtension == ".docx" || normalizedExtension == ".xlsx";
            if (!isZipExtension)
            {
                return (false, "File signature matches executable (MZ header) which is not allowed for this file type.");
            }
        }

        switch (normalizedExtension)
        {
            case ".pdf":
                if (headerRead < 4 ||
                    header16[0] != 0x25 ||
                    header16[1] != 0x50 ||
                    header16[2] != 0x44 ||
                    header16[3] != 0x46)
                {
                    return (false, "File signature does not match .pdf (%PDF header required).");
                }
                break;

            case ".jpg":
            case ".jpeg":
                if (headerRead < 3 ||
                    header16[0] != 0xFF ||
                    header16[1] != 0xD8 ||
                    header16[2] != 0xFF)
                {
                    return (false, "File signature does not match .jpg/.jpeg (FF D8 FF header required).");
                }

                if (stream.Length > 2)
                {
                    stream.Position = stream.Length - 2;
                    byte[] trailer = new byte[2];
                    int trailerRead = await stream.ReadAsync(trailer, 0, 2, ct);
                    if (trailerRead < 2 || trailer[0] != 0xFF || trailer[1] != 0xD9)
                    {
                        return (false, "JPEG/JPG file missing valid end-of-image (FF D9) marker.");
                    }
                    stream.Position = 0;
                }
                break;

            case ".png":
                if (headerRead < 8 ||
                    header16[0] != 0x89 ||
                    header16[1] != 0x50 ||
                    header16[2] != 0x4E ||
                    header16[3] != 0x47 ||
                    header16[4] != 0x0D ||
                    header16[5] != 0x0A ||
                    header16[6] != 0x1A ||
                    header16[7] != 0x0A)
                {
                    return (false, "File signature does not match .png.");
                }
                break;

            case ".doc":
            case ".xls":
                if (headerRead < 8 ||
                    header16[0] != 0xD0 ||
                    header16[1] != 0xCF ||
                    header16[2] != 0x11 ||
                    header16[3] != 0xE0 ||
                    header16[4] != 0xA1 ||
                    header16[5] != 0xB1 ||
                    header16[6] != 0x1A ||
                    header16[7] != 0xE1)
                {
                    return (false, "File signature does not match OLE Compound Document format (.doc/.xls).");
                }
                break;

            case ".docx":
            case ".xlsx":
                if (headerRead < 4 ||
                    header16[0] != 0x50 ||
                    header16[1] != 0x4B ||
                    header16[2] != 0x03 ||
                    header16[3] != 0x04)
                {
                    return (false, "File signature does not match Office Open XML format (ZIP PK\\03\\04 header required for .docx/.xlsx).");
                }

                var ooxmlCheck = await ValidateOOXMLPackageAsync(stream, ct);
                if (!ooxmlCheck.IsValid)
                    return ooxmlCheck;
                break;

            case ".csv":
                var csvCheck = await ValidateCsvContentAsync(stream, ct);
                if (!csvCheck.IsValid)
                    return csvCheck;
                break;
        }

        return (true, null);
    }

    private static Task<(bool IsValid, string? ErrorMessage)> ValidateOOXMLPackageAsync(
        Stream stream,
        CancellationToken ct)
    {
        try
        {
            stream.Position = 0;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            bool foundContentTypes = false;
            int entriesChecked = 0;
            long totalSizeRead = 0;
            const long maxTotalBytes = 1024 * 1024;
            const int maxEntries = 256;

            foreach (var entry in zip.Entries)
            {
                entriesChecked++;
                if (entriesChecked > maxEntries || totalSizeRead > maxTotalBytes)
                    break;

                totalSizeRead += entry.Length;

                if (entry.FullName.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                {
                    foundContentTypes = true;
                    break;
                }
            }

            if (!foundContentTypes)
            {
                return Task.FromResult<(bool, string?)>((false, "Office Open XML package is missing required [Content_Types].xml entry."));
            }

            return Task.FromResult<(bool, string?)>((true, null));
        }
        catch (InvalidDataException)
        {
            return Task.FromResult<(bool, string?)>((false, "Invalid ZIP archive format for Office Open XML file."));
        }
        catch
        {
            return Task.FromResult<(bool, string?)>((false, "Failed to validate Office Open XML package structure."));
        }
    }

    private static async Task<(bool IsValid, string? ErrorMessage)> ValidateCsvContentAsync(
        Stream stream,
        CancellationToken ct)
    {
        stream.Position = 0;
        const int checkSize = 4096;
        byte[] buffer = new byte[checkSize];
        int totalRead = 0;
        bool hasAnyNul = false;

        while (totalRead < checkSize)
        {
            int toRead = Math.Min(buffer.Length - totalRead, checkSize - totalRead);
            int read = await stream.ReadAsync(buffer, totalRead, toRead, ct);
            if (read == 0)
                break;
            totalRead += read;

            for (int i = 0; i < totalRead; i++)
            {
                if (buffer[i] == 0x00)
                {
                    hasAnyNul = true;
                    break;
                }
            }
            if (hasAnyNul)
                break;
        }

        if (!hasAnyNul && stream.Length > checkSize)
        {
            stream.Position = 0;
            byte[] fullBuffer = new byte[81920];
            int fRead;
            while ((fRead = await stream.ReadAsync(fullBuffer, 0, fullBuffer.Length, ct)) > 0)
            {
                for (int i = 0; i < fRead; i++)
                {
                    if (fullBuffer[i] == 0x00)
                    {
                        hasAnyNul = true;
                        break;
                    }
                }
                if (hasAnyNul)
                    break;
            }
        }

        if (hasAnyNul)
        {
            return (false, "CSV file contains NUL bytes which are not allowed.");
        }

        return (true, null);
    }

    public static (bool Mismatch, string? Warning) CheckConfigAgainstAllowList(string[]? configExtensions)
    {
        if (configExtensions == null || configExtensions.Length == 0)
            return (true, "Config is missing FileStorage:AllowedExtensions (documentation only; server uses code-constant allow-list).");

        var normalizedConfig = configExtensions
            .Select(e => (e ?? string.Empty).Trim().ToLowerInvariant())
            .Where(e => !string.IsNullOrEmpty(e))
            .OrderBy(e => e)
            .ToList();

        var normalizedConstant = AllowedExtensions
            .Select(e => e.ToLowerInvariant())
            .OrderBy(e => e)
            .ToList();

        bool match = normalizedConfig.SequenceEqual(normalizedConstant);

        if (!match)
        {
            return (true, $"Config FileStorage:AllowedExtensions [{string.Join(",", normalizedConfig)}] differs from server code-constant [{string.Join(",", normalizedConstant)}]; server always uses code-constant (config is documentation only).");
        }

        return (false, null);
    }
}
