using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Abstractions;

public interface ISequenceGenerator
{
    Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type, CancellationToken ct = default);

    Task<string> GenerateInvoiceNumberAsync(Guid companyId, CancellationToken ct = default);

    Task<string> GenerateReceiptNumberAsync(Guid companyId, CancellationToken ct = default);

    Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix, CancellationToken ct = default);
}
