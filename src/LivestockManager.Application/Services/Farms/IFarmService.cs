using LivestockManager.Application.DTOs.Farms;

namespace LivestockManager.Application.Services.Farms;

public interface IFarmService
{
    Task<FarmDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<FarmSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<IList<FarmSummaryDto>> ListByCompanyAsync(Guid companyId, string? search, CancellationToken ct);
    Task<FarmDetailDto> CreateAsync(FarmCreateDto dto, CancellationToken ct);
    Task<FarmDetailDto> UpdateAsync(Guid id, FarmUpdateDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
