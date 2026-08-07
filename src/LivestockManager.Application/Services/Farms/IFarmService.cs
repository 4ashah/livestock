using LivestockManager.Application.DTOs.Farms;

namespace LivestockManager.Application.Services.Farms;

public interface IFarmService
{
    Task<FarmDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<FarmSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<IList<FarmSummaryDto>> ListByCompanyAsync(Guid companyId, string? search, CancellationToken ct);
    Task<FarmDetailDto> CreateAsync(FarmCreateDto dto, Guid companyId, CancellationToken ct);
    Task<FarmDetailDto> UpdateAsync(Guid id, FarmUpdateDto dto, Guid companyId, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct);
}
