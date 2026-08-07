using LivestockManager.Application.DTOs.Livestock;

namespace LivestockManager.Application.Services.Livestock;

public interface ILivestockService
{
    Task<LivestockDetailDto> RegisterAsync(LivestockRegisterDto dto, Guid companyId, CancellationToken ct);
    Task<LivestockDetailDto> UpdateAsync(Guid livestockId, LivestockEditDto dto, Guid companyId, CancellationToken ct);
    Task AddWeightAsync(Guid livestockId, LivestockWeightAddDto dto, Guid companyId, CancellationToken ct);
    Task<LivestockDetailDto> DischargeAsync(Guid livestockId, LivestockDischargeDto dto, Guid companyId, CancellationToken ct);
    Task<decimal> CalculateCompleteProfitLossAsync(Guid livestockId, Guid companyId, CancellationToken ct);
    Task<IList<LivestockSummaryDto>> SearchAsync(string farmId, string type, string status, string keyword, Guid companyId, CancellationToken ct);
    Task<byte[]> ExportCsvAsync(Guid companyId, CancellationToken ct);
    Task<IList<LivestockWeightHistoryDto>> GetWeightHistoryAsync(Guid livestockId, Guid companyId, CancellationToken ct);
    Task AddActivityAsync(Guid livestockId, LivestockActivityDto dto, Guid companyId, CancellationToken ct);
    Task<LivestockDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
}
