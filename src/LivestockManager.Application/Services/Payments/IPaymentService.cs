using LivestockManager.Application.DTOs.Payments;

namespace LivestockManager.Application.Services.Payments;

public interface IPaymentService
{
    Task<PaymentDetailDto> PostAsync(PaymentCreateDto dto, CancellationToken ct);
    Task ReverseAsync(Guid paymentId, string reason, CancellationToken ct);
    Task<IList<PaymentSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<PaymentDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
}
