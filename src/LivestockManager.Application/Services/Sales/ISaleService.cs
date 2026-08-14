using LivestockManager.Application.DTOs.Sales;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.Services.Sales;

public interface ISaleService
{
    Task<SaleDetailDto> CreateDraftAsync(SaleCreateDto dto, Guid companyId, CancellationToken ct);
    Task<SaleDetailDto> ConfirmAsync(Guid saleId, SaleConfirmDto dto, Guid companyId, CancellationToken ct);
    Task CancelAsync(Guid saleId, string reason, Guid companyId, CancellationToken ct);
    Task<SaleDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<SaleSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);

    Task<(decimal SuggestedPrice, SuggestedPricingMethod Method, decimal? Weight, DateTimeOffset? WeightDate, decimal? Rate)>
        GetSuggestedSalePriceAsync(Guid livestockId, Guid companyId, CancellationToken ct);

    Task<SaleBulkAddResultDto> BulkAddLivestockToDraftSaleAsync(
        Guid saleId, Guid companyId, IEnumerable<Guid> livestockIds, Guid? actingUserId, CancellationToken ct);

    Task<SaleDetailDto> ReverseSaleAsync(
        SaleReversalDto dto, Guid companyId, Guid actingUserId, string? actingUserRole, CancellationToken ct);
}
