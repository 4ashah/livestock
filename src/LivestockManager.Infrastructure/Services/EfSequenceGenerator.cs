using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Persistence;
using System.Data;
using System.Data.Common;

namespace LivestockManager.Infrastructure.Services;

public class EfSequenceGenerator : ISequenceGenerator
{
    private readonly AppDbContext _dbContext;

    public EfSequenceGenerator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static string GetPrefix(LivestockType type)
    {
        return type switch
        {
            LivestockType.PurchasedCastratedRam => "Ah",
            LivestockType.UncastratedRam => "Su",
            LivestockType.PurchasedEwe => "Sa",
            LivestockType.BredCastratedRam => "Ad",
            LivestockType.BredEwe => "Sd",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public async Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type)
    {
        var prefix = GetPrefix(type);
        var lastValue = await GenerateNumberAsync(companyId, prefix);
        return prefix + lastValue.ToString("D5");
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        var company = await _dbContext.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
            throw new SequenceGenerationFailedException("Company not found for invoice number generation.");

        var prefix = string.IsNullOrWhiteSpace(company.InvoicePrefix) ? "INV" : company.InvoicePrefix;
        var lastValue = await GenerateNumberAsync(companyId, prefix);
        return prefix + lastValue.ToString("D5");
    }

    public async Task<string> GenerateReceiptNumberAsync(Guid companyId)
    {
        var company = await _dbContext.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
            throw new SequenceGenerationFailedException("Company not found for receipt number generation.");

        var prefix = string.IsNullOrWhiteSpace(company.ReceiptPrefix) ? "RCT" : company.ReceiptPrefix;
        var lastValue = await GenerateNumberAsync(companyId, prefix);
        return prefix + lastValue.ToString("D5");
    }

    public async Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

        var lastValue = await GenerateNumberAsync(companyId, prefix);
        return prefix + lastValue.ToString("D5");
    }

    private async Task<long> GenerateNumberAsync(Guid companyId, string prefix)
    {
        const int maxRetries = 1;
        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                using var dbTxn = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead);

                try
                {
                    await using var updateCmd = conn.CreateCommand();
                    updateCmd.Transaction = dbTxn;
                    updateCmd.CommandText = @"
UPDATE SequenceCounters WITH (ROWLOCK, UPDLOCK, HOLDLOCK)
SET LastValue = LastValue + 1, LastUpdatedAt = GETUTCDATE()
OUTPUT INSERTED.LastValue
WHERE CompanyId = @CompanyId AND Prefix = @Prefix;";
                    var c1 = new SqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = companyId };
                    var p1 = new SqlParameter("@Prefix", SqlDbType.NVarChar, 50) { Value = prefix };
                    updateCmd.Parameters.Add(c1);
                    updateCmd.Parameters.Add(p1);

                    var resultRaw = await updateCmd.ExecuteScalarAsync();
                    long result = 0;
                    if (resultRaw != null && resultRaw != DBNull.Value)
                        result = Convert.ToInt64(resultRaw);

                    if (result == 0)
                    {
                        await using var insertCmd = conn.CreateCommand();
                        insertCmd.Transaction = dbTxn;
                        insertCmd.CommandText = @"
INSERT INTO SequenceCounters (Id, CompanyId, Prefix, LastValue, LastUpdatedAt, CreatedAt, IsDeleted)
OUTPUT INSERTED.LastValue
VALUES (@Id, @CompanyId, @Prefix, 1, GETUTCDATE(), GETUTCDATE(), 0);";
                        var idParam = new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() };
                        var c2 = new SqlParameter("@CompanyId", SqlDbType.UniqueIdentifier) { Value = companyId };
                        var p2 = new SqlParameter("@Prefix", SqlDbType.NVarChar, 50) { Value = prefix };
                        insertCmd.Parameters.Add(idParam);
                        insertCmd.Parameters.Add(c2);
                        insertCmd.Parameters.Add(p2);

                        var insertRaw = await insertCmd.ExecuteScalarAsync();
                        if (insertRaw != null && insertRaw != DBNull.Value)
                            result = Convert.ToInt64(insertRaw);

                        if (result == 0)
                            result = 1;
                    }

                    await dbTxn.CommitAsync();
                    return result;
                }
                catch
                {
                    await dbTxn.RollbackAsync();
                    throw;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (retry == maxRetries)
                    throw new SequenceGenerationFailedException("Failed to generate sequence number after retries due to concurrency conflict.");
                await Task.Delay(50);
            }
            catch (DbUpdateException)
            {
                if (retry == maxRetries)
                    throw new SequenceGenerationFailedException("Failed to generate sequence number after retries due to database update conflict.");
                await Task.Delay(50);
            }
        }

        throw new SequenceGenerationFailedException("Failed to generate sequence number.");
    }
}
