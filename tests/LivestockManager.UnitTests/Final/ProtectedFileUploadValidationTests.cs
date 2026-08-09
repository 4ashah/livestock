using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Text;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Services.Storage;

namespace LivestockManager.UnitTests.Final;

public class ProtectedFileUploadValidationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppDbContext _dbContext;
    private readonly ProtectedDocumentStorage _storage;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _uploaderId;

    public ProtectedFileUploadValidationTests()
    {
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _uploaderId = Guid.NewGuid();

        _tempRoot = Path.Combine(Path.GetTempPath(), "livestock_phase6_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Phase6Tests_" + Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new AppDbContext(dbOptions);

        var configValues = new Dictionary<string, string?>
        {
            { "ProtectedStorage:Root", _tempRoot }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _storage = new ProtectedDocumentStorage(_dbContext, configuration);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch
        {
        }
    }

    private static IFormFile BuildFormFile(string fileName, Stream stream, string contentType = "application/octet-stream")
    {
        stream.Position = 0;
        var formFile = new FormFile(stream, 0, stream.Length, "Files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return formFile;
    }

    private static Stream BuildPdfStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A });
        writer.Write(Encoding.ASCII.GetBytes("test pdf content filler text\n"));
        writer.Write(new byte[] { 0x25, 0x25, 0x45, 0x4F, 0x46 });
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildJpegStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 });
        writer.Write(new byte[256]);
        writer.Write(new byte[] { 0xFF, 0xD9 });
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildPngStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 });
        writer.Write(new byte[256]);
        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 });
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildDocOleStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
        writer.Write(new byte[512]);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildOOXMLStream()
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("[Content_Types].xml");
            using (var sw = new StreamWriter(entry.Open()))
            {
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types/>");
            }
            var entry2 = zip.CreateEntry("_rels/.rels");
            using (var sw = new StreamWriter(entry2.Open()))
            {
                sw.Write("<Relationships/>");
            }
            var entry3 = zip.CreateEntry("docProps/core.xml");
            using (var sw = new StreamWriter(entry3.Open()))
            {
                sw.Write("<cp:coreProperties/>");
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildCsvStream(bool withBom = true, bool withNull = false)
    {
        var ms = new MemoryStream();
        if (withBom)
        {
            ms.WriteByte(0xEF);
            ms.WriteByte(0xBB);
            ms.WriteByte(0xBF);
        }
        var content = "Name,Age,City\nJohn,30,NYC\nJane,25,LA\n";
        var bytes = Encoding.UTF8.GetBytes(content);
        ms.Write(bytes, 0, bytes.Length);
        if (withNull)
        {
            ms.WriteByte(0x00);
            ms.WriteByte(0x00);
        }
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildRandomBytesStream(int length)
    {
        var ms = new MemoryStream();
        var rng = new Random(42);
        var buf = new byte[length];
        rng.NextBytes(buf);
        ms.Write(buf, 0, buf.Length);
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildPEExeStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0x4D, 0x5A });
        writer.Write(new byte[128]);
        writer.Write(new byte[] { 0x50, 0x45, 0x00, 0x00 });
        writer.Write(new byte[512]);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task V1_ValidPdf_Passes()
    {
        using var stream = BuildPdfStream();
        var formFile = BuildFormFile("report.pdf", stream, "application/pdf");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".pdf", result.NormalizedExtension);
        Assert.Equal("report.pdf", result.SanitizedDisplayName);
        Assert.NotNull(result.StoredFileName);
        Assert.EndsWith(".pdf", result.StoredFileName);
        Assert.Contains(_companyA.ToString("N"), result.ProposedStoredPath);
    }

    [Fact]
    public async Task V2_ValidJpeg_Passes()
    {
        using var stream = BuildJpegStream();
        var formFile = BuildFormFile("photo.JPG", stream, "image/jpeg");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".jpg", result.NormalizedExtension);
        Assert.Equal("photo.JPG", result.SanitizedDisplayName);
    }

    [Fact]
    public async Task V3_ValidPng_Passes()
    {
        using var stream = BuildPngStream();
        var formFile = BuildFormFile("diagram.png", stream, "image/png");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".png", result.NormalizedExtension);
    }

    [Fact]
    public async Task V4_ValidDoc_OLE_Passes()
    {
        using var stream = BuildDocOleStream();
        var formFile = BuildFormFile("invoice.doc", stream, "application/msword");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".doc", result.NormalizedExtension);
    }

    [Fact]
    public async Task V5_ValidDocX_OOXML_Passes()
    {
        using var stream = BuildOOXMLStream();
        var formFile = BuildFormFile("contract.docx", stream,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".docx", result.NormalizedExtension);
    }

    [Fact]
    public async Task V6_ValidXlsx_OOXML_Passes()
    {
        using var stream = BuildOOXMLStream();
        var formFile = BuildFormFile("budget.xlsx", stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".xlsx", result.NormalizedExtension);
    }

    [Fact]
    public async Task V7_ValidCsv_PlainText_Passes()
    {
        using var stream = BuildCsvStream(withBom: true);
        var formFile = BuildFormFile("data.csv", stream, "text/csv");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(".csv", result.NormalizedExtension);
    }

    [Fact]
    public async Task I1_InvalidExtension_Exe_Fails()
    {
        using var stream = BuildRandomBytesStream(256);
        var formFile = BuildFormFile("bad.exe", stream);

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(".exe", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not allowed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("bad.cmd")]
    [InlineData("page.html")]
    [InlineData("script.js")]
    [InlineData("archive.zip")]
    public async Task I1_InvalidExtension_Others_Fail(string fileName)
    {
        using var stream = BuildRandomBytesStream(256);
        var formFile = BuildFormFile(fileName, stream);

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("not allowed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I2_DoubleExtension_EvilPdfExe_Fails()
    {
        using var stream = BuildPEExeStream();
        var formFile = BuildFormFile("evil.pdf.exe", stream);

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(".exe", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I3_PE_Executable_RenamedToJpg_MzHeader_Fails()
    {
        using var exeStream = BuildPEExeStream();
        var formFile = BuildFormFile("renamed.jpg", exeStream, "image/jpeg");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        bool mentionsMZ = result.ErrorMessage != null && (
            result.ErrorMessage.Contains("MZ") ||
            result.ErrorMessage.Contains("executable", StringComparison.OrdinalIgnoreCase));
        Assert.True(mentionsMZ, $"Expected mention of MZ/executable in error, got: {result.ErrorMessage}");
    }

    [Fact]
    public async Task I4_InvalidMagic_RandomBytesForPdf_Fails()
    {
        using var randomStream = BuildRandomBytesStream(1024);
        var formFile = BuildFormFile("fake.pdf", randomStream, "application/pdf");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("signature", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I5_OversizedFile_Over25Mb_Fails()
    {
        int oversizedBytes = (int)ProtectedFileUploadValidator.MaxFileSizeBytes + 10;
        using var bigStream = BuildPdfStream();
        var ms = new MemoryStream();
        bigStream.CopyTo(ms);
        ms.Position = ms.Length;
        var filler = new byte[oversizedBytes - (int)ms.Length];
        ms.Write(filler, 0, filler.Length);
        ms.Position = 0;

        var formFile = BuildFormFile("big.pdf", ms, "application/pdf");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("exceeds", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I6_EmptyFile_ZeroBytes_Fails()
    {
        using var emptyStream = new MemoryStream();
        var formFile = BuildFormFile("empty.pdf", emptyStream);

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid);
        bool mentionsEmpty = result.ErrorMessage != null && (
            result.ErrorMessage.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorMessage.Contains("zero", StringComparison.OrdinalIgnoreCase));
        Assert.True(mentionsEmpty, $"Expected empty/zero mention, got: {result.ErrorMessage}");
    }

    [Theory]
    [InlineData("résumé.pdf")]
    [InlineData("报告.docx")]
    [InlineData("faktura_Łódź.csv")]
    public async Task I7_UnicodeFilename_ValidExtension_Accepted(string fileName)
    {
        using var pdfStream = BuildPdfStream();
        string actualName = fileName;
        Stream streamToUse;
        if (fileName.EndsWith(".docx"))
            streamToUse = BuildOOXMLStream();
        else if (fileName.EndsWith(".csv"))
            streamToUse = BuildCsvStream();
        else
            streamToUse = BuildPdfStream();

        using (streamToUse)
        {
            var formFile = BuildFormFile(actualName, streamToUse);
            var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

            Assert.True(result.IsValid, $"Expected valid for {actualName}, error: {result.ErrorMessage}");
            Assert.NotNull(result.SanitizedDisplayName);
        }
    }

    [Theory]
    [InlineData("../../../etc/passwd.pdf")]
    [InlineData("..\\..\\..\\windows\\win.ini.pdf")]
    [InlineData("folder/../escape.pdf")]
    public async Task I8_PathTraversal_Filename_Rejected(string traversalName)
    {
        using var pdfStream = BuildPdfStream();
        var formFile = BuildFormFile(traversalName, pdfStream, "application/pdf");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, _companyA, CancellationToken.None);

        Assert.False(result.IsValid, $"Expected traversal to be rejected for: {traversalName}");
    }

    [Fact]
    public async Task I9_CrossCompany_EmptyCompanyId_Fails()
    {
        using var pdfStream = BuildPdfStream();
        var formFile = BuildFormFile("a.pdf", pdfStream, "application/pdf");

        var result = await ProtectedFileUploadValidator.ValidateUploadAsync(formFile, Guid.Empty, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("Company ID", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I10_SoftDeleted_DocumentAccess_Rejected()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = ProtectedFileUploadValidator.AllowedExtensions;

        var stored = await _storage.StoreAsync(
            _companyA,
            "soft-delete-test.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            ProtectedFileUploadValidator.MaxFileSizeBytes,
            allowedExts,
            _uploaderId,
            CancellationToken.None);

        var doc = await _dbContext.Set<Document>().FindAsync(stored.DocumentId);
        Assert.NotNull(doc);
        doc.IsDeleted = true;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.DownloadAsync(stored.DocumentId, _companyA, CancellationToken.None));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I11_MissingPhysicalFile_NotFound()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = ProtectedFileUploadValidator.AllowedExtensions;

        var stored = await _storage.StoreAsync(
            _companyA,
            "physical-missing-test.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            ProtectedFileUploadValidator.MaxFileSizeBytes,
            allowedExts,
            _uploaderId,
            CancellationToken.None);

        var fullFilePath = Path.Combine(_tempRoot, stored.SafeInternalPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullFilePath));
        File.Delete(fullFilePath);
        Assert.False(File.Exists(fullFilePath));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.DownloadAsync(stored.DocumentId, _companyA, CancellationToken.None));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task I9b_Storage_CrossCompany_Download_ThrowsUnauthorized()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = ProtectedFileUploadValidator.AllowedExtensions;

        var stored = await _storage.StoreAsync(
            _companyA,
            "cross-company-test.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            ProtectedFileUploadValidator.MaxFileSizeBytes,
            allowedExts,
            _uploaderId,
            CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _storage.DownloadAsync(stored.DocumentId, _companyB, CancellationToken.None));
    }

    [Fact]
    public void AllowedExtensions_HasExactlyNine()
    {
        var expected = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".csv" };
        Assert.Equal(9, ProtectedFileUploadValidator.AllowedExtensions.Length);
        foreach (var e in expected)
        {
            Assert.Contains(e, ProtectedFileUploadValidator.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MaxFileSize_Is25Mb()
    {
        Assert.Equal(25 * 1024 * 1024, ProtectedFileUploadValidator.MaxFileSizeBytes);
    }

    [Fact]
    public void CheckConfigAgainstAllowList_Match_NoWarning()
    {
        var okConfig = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".csv" };
        var (mismatch, warning) = ProtectedFileUploadValidator.CheckConfigAgainstAllowList(okConfig);
        Assert.False(mismatch);
        Assert.Null(warning);
    }

    [Fact]
    public void CheckConfigAgainstAllowList_Mismatch_Warning()
    {
        var badConfig = new[] { ".pdf", ".gif", ".txt" };
        var (mismatch, warning) = ProtectedFileUploadValidator.CheckConfigAgainstAllowList(badConfig);
        Assert.True(mismatch);
        Assert.NotNull(warning);
        Assert.Contains("code-constant", warning, StringComparison.OrdinalIgnoreCase);
    }
}
