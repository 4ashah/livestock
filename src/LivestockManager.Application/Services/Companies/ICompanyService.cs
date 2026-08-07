using LivestockManager.Application.DTOs.Companies;

namespace LivestockManager.Application.Services.Companies;

public interface ICompanyService
{
    Task<CompanyDetailDto> GetDefaultAsync(CancellationToken ct);
    Task<CompanyDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<CompanySummaryDto>> ListAsync(CancellationToken ct);
    Task<CompanyDetailDto> UpdateAsync(Guid id, CompanyUpdateDto dto, CancellationToken ct);
    Task SwitchCompanyAsync(Guid companyId, CancellationToken ct);
}
