using LivestockManager.Application.DTOs.Livestock;

namespace LivestockManager.Application.Services.Livestock;

public interface ILivestockService
{
    Task<LivestockDetailDto> RegisterAsync(LivestockRegisterDto dto, CancellationToken ct);
    Task<LivestockDetailDto> UpdateAsync(Guid livestockId, LivestockEditDto dto, CancellationToken ct);
    Task AddWeightAsync(Guid livestockId, LivestockWeightAddDto dto, CancellationToken ct);
    Task<LivestockDetailDto> DischargeAsync(Guid livestockId, LivestockDischargeDto dto, CancellationToken ct);
    Task<decimal> CalculateCompleteProfitLossAsync(Guid livestockId, CancellationToken ct);
    Task<IList<LivestockSummaryDto>> SearchAsync(string farmId, string type, string status, string keyword, CancellationToken ct);
    Task<byte[]> ExportCsvAsync(Guid companyId, CancellationToken ct);
    Task<IList<LivestockWeightHistoryDto>> GetWeightHistoryAsync(Guid livestockId, CancellationToken ct);
    Task AddActivityAsync(Guid livestockId, LivestockActivityDto dto, CancellationToken ct);
    Task<LivestockDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
}
