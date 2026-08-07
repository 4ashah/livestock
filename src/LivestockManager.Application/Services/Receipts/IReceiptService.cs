using LivestockManager.Application.DTOs.Receipts;

namespace LivestockManager.Application.Services.Receipts;

public interface IReceiptService
{
    Task<ReceiptDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<ReceiptSummaryDto>> ListForCustomerAsync(Guid companyId, Guid customerId, CancellationToken ct);
    Task<ReceiptDetailDto> GenerateForPaymentAsync(Guid paymentId, string? notes, CancellationToken ct);
    Task<ReceiptDetailDto> ReverseReceiptAsync(Guid receiptId, string reason, Guid reversedByUserId, CancellationToken ct);
}
