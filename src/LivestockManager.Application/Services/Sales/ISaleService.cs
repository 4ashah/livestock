using LivestockManager.Application.DTOs.Sales;

namespace LivestockManager.Application.Services.Sales;

public interface ISaleService
{
    Task<SaleDetailDto> CreateDraftAsync(SaleCreateDto dto, Guid companyId, CancellationToken ct);
    Task<SaleDetailDto> ConfirmAsync(Guid saleId, SaleConfirmDto dto, Guid companyId, CancellationToken ct);
    Task CancelAsync(Guid saleId, string reason, Guid companyId, CancellationToken ct);
    Task<SaleDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<SaleSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
}
