using LivestockManager.Application.DTOs.Payments;

namespace LivestockManager.Application.Services.Payments;

public interface IPaymentService
{
    Task<PaymentDetailDto> PostAsync(PaymentCreateDto dto, Guid companyId, CancellationToken ct);
    Task ReverseAsync(Guid paymentId, string reason, Guid companyId, CancellationToken ct);
    Task<IList<PaymentSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<PaymentDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
}
