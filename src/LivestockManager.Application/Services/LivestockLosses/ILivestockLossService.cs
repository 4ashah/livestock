using LivestockManager.Application.DTOs.LivestockLosses;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.Services.LivestockLosses;

public interface ILivestockLossService
{
    Task<LivestockLossDetailDto> CreateAsync(LivestockLossCreateDto dto, Guid companyId, CancellationToken ct);
    Task<LivestockLossDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<LivestockLossSummaryDto>> ListAsync(Guid companyId, Guid? farmId, Guid? livestockId,
        DischargeCondition? lossType, DateTimeOffset? from, DateTimeOffset? to, bool includeReversed,
        CancellationToken ct);
    Task<LivestockLossDetailDto> ReverseAsync(Guid id, string reason, Guid companyId, CancellationToken ct);
}
