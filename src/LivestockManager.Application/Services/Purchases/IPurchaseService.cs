using LivestockManager.Application.DTOs.Purchases;

namespace LivestockManager.Application.Services.Purchases;

public interface IPurchaseService
{
    Task<PurchaseDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<PurchaseSummaryDto>> ListAsync(Guid companyId, Guid? supplierId, PurchaseStatus? status, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<PurchaseDetailDto> CreateDraftAsync(PurchaseCreateDto dto, Guid companyId, CancellationToken ct);
    Task<PurchaseDetailDto> AddItemAsync(Guid purchaseId, PurchaseItemCreateDto item, Guid companyId, CancellationToken ct);
    Task<PurchaseDetailDto> RemoveItemAsync(Guid purchaseId, Guid purchaseItemId, Guid companyId, CancellationToken ct);
    Task<PurchaseDetailDto> UpdateTotalsAsync(Guid purchaseId, Guid companyId, CancellationToken ct);
    Task<PurchaseDetailDto> PostPurchaseAsync(PurchasePostDto dto, Guid companyId, CancellationToken ct);
    Task<PurchaseDetailDto> VoidPurchaseAsync(Guid id, string reason, Guid companyId, CancellationToken ct);
}
