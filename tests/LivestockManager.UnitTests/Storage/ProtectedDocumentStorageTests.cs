using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Services.Storage;

namespace LivestockManager.UnitTests.Storage;

public class ProtectedDocumentStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppDbContext _dbContext;
    private readonly ProtectedDocumentStorage _storage;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _uploaderId;

    public ProtectedDocumentStorageTests()
    {
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _uploaderId = Guid.NewGuid();

        _tempRoot = Path.Combine(Path.GetTempPath(), "livestock_storage_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "StorageTests_" + Guid.NewGuid().ToString("N"))
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

    private static Stream BuildPdfStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A });
        writer.Write(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 test content filler for minimum size\n"));
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static Stream BuildJpegStream()
    {
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 });
        writer.Write(new byte[128]);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task StoreAsync_WritesFileAndReturnsMetadata()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = new[] { ".pdf", ".jpg" };

        var result = await _storage.StoreAsync(
            _companyA,
            "my-report.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            10 * 1024 * 1024,
            allowedExts,
            _uploaderId,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.True(result.SizeBytes > 0);
        Assert.EndsWith(".pdf", result.SafeInternalPath, StringComparison.OrdinalIgnoreCase);

        var storedDoc = await _dbContext.Set<Document>().FindAsync(result.DocumentId);
        Assert.NotNull(storedDoc);
        Assert.Equal(_companyA, storedDoc.CompanyId);
        Assert.Equal("my-report.pdf", storedDoc.DisplayName);
        Assert.Equal(DocumentType.InvoicePdf, storedDoc.DocumentType);
        Assert.Equal(_uploaderId, storedDoc.UploadedByUserId);
        Assert.Equal("pdf", storedDoc.Extension);
        Assert.Equal("application/pdf", storedDoc.ContentType);

        var fullFilePath = Path.Combine(_tempRoot, result.SafeInternalPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullFilePath), "Physical file should be written to disk.");
    }

    [Fact]
    public async Task StoreAsync_InvalidExtension_Throws()
    {
        using var stream = BuildPdfStream();
        var allowedExts = new[] { ".jpg", ".png" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.StoreAsync(
                _companyA,
                "file.pdf",
                stream,
                DocumentType.Other,
                10 * 1024 * 1024,
                allowedExts,
                null,
                CancellationToken.None));

        Assert.Contains(".pdf", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StoreAsync_PdfWithJpegMagic_ThrowsSignatureMismatch()
    {
        using var jpegStream = BuildJpegStream();
        var allowedExts = new[] { ".pdf", ".jpg" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storage.StoreAsync(
                _companyA,
                "fake.pdf",
                jpegStream,
                DocumentType.InvoicePdf,
                10 * 1024 * 1024,
                allowedExts,
                null,
                CancellationToken.None));

        Assert.Contains("signature", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadAsync_CrossCompanyLeakage_Throws()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = new[] { ".pdf" };

        var stored = await _storage.StoreAsync(
            _companyA,
            "report.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            10 * 1024 * 1024,
            allowedExts,
            null,
            CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _storage.DownloadAsync(stored.DocumentId, _companyB, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_DoesNotDeletePhysicalFile()
    {
        using var pdfStream = BuildPdfStream();
        var allowedExts = new[] { ".pdf" };

        var stored = await _storage.StoreAsync(
            _companyA,
            "audit.pdf",
            pdfStream,
            DocumentType.InvoicePdf,
            10 * 1024 * 1024,
            allowedExts,
            null,
            CancellationToken.None);

        var storedDoc = await _dbContext.Set<Document>().FindAsync(stored.DocumentId);
        Assert.NotNull(storedDoc);

        var fullFilePath = Path.Combine(_tempRoot, stored.SafeInternalPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullFilePath), "File must exist before delete test.");

        storedDoc.IsDeleted = true;
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        Assert.True(File.Exists(fullFilePath), "Physical file must still exist after soft-delete (7-year audit policy).");

        var reloaded = await _dbContext.Set<Document>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == stored.DocumentId);
        Assert.NotNull(reloaded);
        Assert.True(reloaded.IsDeleted);
    }
}
