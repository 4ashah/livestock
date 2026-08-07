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

    public async Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type, CancellationToken ct = default)
    {
        var prefix = GetPrefix(type);
        var lastValue = await GenerateNumberAsync(companyId, prefix, ct);
        return prefix + lastValue.ToString("D5");
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await _dbContext.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company == null)
            throw new SequenceGenerationFailedException("Company not found for invoice number generation.");

        var prefix = string.IsNullOrWhiteSpace(company.InvoicePrefix) ? "INV" : company.InvoicePrefix.Trim();
        var year = DateTime.UtcNow.Year.ToString();
        var scopedKey = $"{prefix}:{year}";
        var lastValue = await GenerateNumberAsync(companyId, scopedKey, ct);
        return $"{prefix}-{year}-{lastValue:D5}";
    }

    public async Task<string> GenerateReceiptNumberAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await _dbContext.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (company == null)
            throw new SequenceGenerationFailedException("Company not found for receipt number generation.");

        var prefix = string.IsNullOrWhiteSpace(company.ReceiptPrefix) ? "RCP" : company.ReceiptPrefix.Trim();
        var year = DateTime.UtcNow.Year.ToString();
        var scopedKey = $"{prefix}:{year}";
        var lastValue = await GenerateNumberAsync(companyId, scopedKey, ct);
        return $"{prefix}-{year}-{lastValue:D5}";
    }

    public async Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

        if (prefix.Length is >= 3 and <= 4 || prefix.Contains(':'))
        {
            var parts = prefix.Split(':');
            var basePrefix = parts[0];
            if (basePrefix is "PUR" or "PAY" or "INV" or "RCP" ||
                (basePrefix.Length >= 3 && parts.Length == 2))
            {
                string year;
                string scopedKey;
                if (parts.Length == 2)
                {
                    year = parts[1];
                    scopedKey = prefix;
                }
                else
                {
                    year = DateTime.UtcNow.Year.ToString();
                    scopedKey = $"{prefix}:{year}";
                }
                var lastValue = await GenerateNumberAsync(companyId, scopedKey, ct);
                return $"{basePrefix}-{year}-{lastValue:D5}";
            }
        }

        var rawLastValue = await GenerateNumberAsync(companyId, prefix, ct);
        return prefix + rawLastValue.ToString("D5");
    }

    private async Task<long> GenerateNumberAsync(Guid companyId, string prefix, CancellationToken ct)
    {
        const int maxRetries = 5;
        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                var conn = _dbContext.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct);

                using var dbTxn = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

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
                    var p1 = new SqlParameter("@Prefix", SqlDbType.NVarChar, 100) { Value = prefix };
                    updateCmd.Parameters.Add(c1);
                    updateCmd.Parameters.Add(p1);

                    var resultRaw = await updateCmd.ExecuteScalarAsync(ct);
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
                        var p2 = new SqlParameter("@Prefix", SqlDbType.NVarChar, 100) { Value = prefix };
                        insertCmd.Parameters.Add(idParam);
                        insertCmd.Parameters.Add(c2);
                        insertCmd.Parameters.Add(p2);

                        var insertRaw = await insertCmd.ExecuteScalarAsync(ct);
                        if (insertRaw != null && insertRaw != DBNull.Value)
                            result = Convert.ToInt64(insertRaw);

                        if (result == 0)
                            result = 1;
                    }

                    await dbTxn.CommitAsync(ct);
                    return result;
                }
                catch
                {
                    await dbTxn.RollbackAsync(ct);
                    throw;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (retry == maxRetries)
                    throw new SequenceGenerationFailedException("Failed to generate sequence number after retries due to concurrency conflict.");
                await Task.Delay(Random.Shared.Next(10, 61), ct);
            }
            catch (DbUpdateException)
            {
                if (retry == maxRetries)
                    throw new SequenceGenerationFailedException("Failed to generate sequence number after retries due to database update conflict.");
                await Task.Delay(Random.Shared.Next(10, 61), ct);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627 or 1205)
            {
                if (retry == maxRetries)
                    throw new SequenceGenerationFailedException($"Failed to generate sequence number after retries due to SQL conflict (error {ex.Number}).", ex);
                await Task.Delay(Random.Shared.Next(10, 61), ct);
            }
        }

        throw new SequenceGenerationFailedException("Failed to generate sequence number.");
    }
}
