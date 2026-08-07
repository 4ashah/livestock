using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Abstractions;

public interface ISequenceGenerator
{
    Task<string> GenerateLivestockIdAsync(Guid companyId, LivestockType type);

    Task<string> GenerateInvoiceNumberAsync(Guid companyId);

    Task<string> GenerateReceiptNumberAsync(Guid companyId);

    Task<string> GenerateDocumentNumberAsync(Guid companyId, string prefix);
}
