using LivestockManager.Application.DTOs.StockAddition;

namespace LivestockManager.Application.Services.StockAddition;

public interface IStockAdditionService
{
    Task<StockAdditionResultDto> AddPurchasedLivestockAsync(
        StockAdditionPurchasedDto dto,
        Guid companyId,
        Guid userId,
        CancellationToken ct);

    Task<StockAdditionResultDto> AddNewbornLivestockAsync(
        StockAdditionNewbornDto dto,
        Guid companyId,
        Guid userId,
        CancellationToken ct);

    Task<IList<ParentSearchItemDto>> SearchEligibleEwesAsync(
        string keyword,
        Guid companyId,
        int limit = 25,
        CancellationToken ct = default);

    Task<IList<ParentSearchItemDto>> SearchEligibleRamsAsync(
        string keyword,
        Guid companyId,
        int limit = 25,
        CancellationToken ct = default);
}
