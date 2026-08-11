using LivestockManager.Application.DTOs.Receipts;

namespace LivestockManager.Application.Services.Receipts;

public interface IReceiptService
{
    Task<ReceiptDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<ReceiptSummaryDto>> ListForCustomerAsync(Guid companyId, Guid customerId, CancellationToken ct);
    Task<IList<ReceiptSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<ReceiptDetailDto> GenerateForPaymentAsync(Guid paymentId, string? notes, Guid companyId, CancellationToken ct);
    Task<ReceiptDetailDto> ReverseReceiptAsync(Guid receiptId, string reason, Guid reversedByUserId, Guid companyId, CancellationToken ct);
    Task<byte[]> GetPdfAsync(Guid receiptId, Guid companyId, CancellationToken ct);
}
