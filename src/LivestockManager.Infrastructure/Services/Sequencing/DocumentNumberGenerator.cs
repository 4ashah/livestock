using Microsoft.EntityFrameworkCore;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Services.Sequencing;

public class DocumentNumberGenerator
{
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly AppDbContext _dbContext;

    public DocumentNumberGenerator(ISequenceGenerator sequenceGenerator, AppDbContext dbContext)
    {
        _sequenceGenerator = sequenceGenerator;
        _dbContext = dbContext;
    }

    public async Task<string> GeneratePurchaseNumberAsync(Guid companyId)
    {
        var year = DateTimeOffset.UtcNow.Year.ToString();
        var scopedKey = $"PUR:{year}";
        var raw = await _sequenceGenerator.GenerateDocumentNumberAsync(companyId, scopedKey);
        var number = ExtractNumericSuffix(raw, scopedKey);
        return $"PUR-{year}-{number:D5}";
    }

    public async Task<string> GeneratePaymentNumberAsync(Guid companyId)
    {
        var year = DateTimeOffset.UtcNow.Year.ToString();
        var scopedKey = $"PAY:{year}";
        var raw = await _sequenceGenerator.GenerateDocumentNumberAsync(companyId, scopedKey);
        var number = ExtractNumericSuffix(raw, scopedKey);
        return $"PAY-{year}-{number:D5}";
    }

    public async Task<string> GenerateInvoiceNumberYearScopedAsync(Guid companyId)
    {
        var year = DateTimeOffset.UtcNow.Year.ToString();
        var prefix = await GetInvoicePrefixSafeAsync(companyId);
        var scopedKey = $"{prefix}:{year}";
        var raw = await _sequenceGenerator.GenerateDocumentNumberAsync(companyId, scopedKey);
        var number = ExtractNumericSuffix(raw, scopedKey);
        return $"{prefix}-{year}-{number:D5}";
    }

    public async Task<string> GenerateReceiptNumberYearScopedAsync(Guid companyId)
    {
        var year = DateTimeOffset.UtcNow.Year.ToString();
        var prefix = await GetReceiptPrefixSafeAsync(companyId);
        var scopedKey = $"{prefix}:{year}";
        var raw = await _sequenceGenerator.GenerateDocumentNumberAsync(companyId, scopedKey);
        var number = ExtractNumericSuffix(raw, scopedKey);
        return $"{prefix}-{year}-{number:D5}";
    }

    private async Task<string> GetInvoicePrefixSafeAsync(Guid companyId)
    {
        try
        {
            var company = await _dbContext.Set<Domain.Entities.Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                throw new SequenceGenerationFailedException("Company not found for invoice prefix lookup.");

            var prefix = string.IsNullOrWhiteSpace(company.InvoicePrefix) ? "INV" : company.InvoicePrefix.Trim();
            return prefix.Length > 10 ? prefix[..10] : prefix;
        }
        catch (SequenceGenerationFailedException)
        {
            throw;
        }
        catch
        {
            return "INV";
        }
    }

    private async Task<string> GetReceiptPrefixSafeAsync(Guid companyId)
    {
        try
        {
            var company = await _dbContext.Set<Domain.Entities.Company>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                throw new SequenceGenerationFailedException("Company not found for receipt prefix lookup.");

            var prefix = string.IsNullOrWhiteSpace(company.ReceiptPrefix) ? "RCT" : company.ReceiptPrefix.Trim();
            return prefix.Length > 10 ? prefix[..10] : prefix;
        }
        catch (SequenceGenerationFailedException)
        {
            throw;
        }
        catch
        {
            return "RCT";
        }
    }

    private static long ExtractNumericSuffix(string rawDocumentNumber, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(rawDocumentNumber))
            return 1;

        var prefix = expectedPrefix ?? string.Empty;
        var suffix = rawDocumentNumber.StartsWith(prefix, StringComparison.Ordinal)
            ? rawDocumentNumber[prefix.Length..]
            : rawDocumentNumber;

        long result;
        if (long.TryParse(suffix, out result))
            return result;

        var digits = new string(suffix.Where(char.IsDigit).ToArray());
        if (digits.Length > 0 && long.TryParse(digits, out result))
            return result;

        return 1;
    }
}
