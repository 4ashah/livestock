using LivestockManager.Application.DTOs.Purchases;

namespace LivestockManager.Application.Services.Purchases;

public interface IPurchaseService
{
    Task<PurchaseDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<PurchaseSummaryDto>> ListAsync(Guid companyId, Guid? supplierId, PurchaseStatus? status, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<PurchaseDetailDto> CreateDraftAsync(PurchaseCreateDto dto, CancellationToken ct);
    Task<PurchaseDetailDto> AddItemAsync(Guid purchaseId, PurchaseItemCreateDto item, CancellationToken ct);
    Task<PurchaseDetailDto> RemoveItemAsync(Guid purchaseId, Guid purchaseItemId, CancellationToken ct);
    Task<PurchaseDetailDto> UpdateTotalsAsync(Guid purchaseId, CancellationToken ct);
    Task<PurchaseDetailDto> PostPurchaseAsync(PurchasePostDto dto, CancellationToken ct);
    Task<PurchaseDetailDto> VoidPurchaseAsync(Guid id, string reason, CancellationToken ct);
}
