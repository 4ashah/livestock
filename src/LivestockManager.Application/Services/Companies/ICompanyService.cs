using LivestockManager.Application.DTOs.Companies;

namespace LivestockManager.Application.Services.Companies;

public interface ICompanyService
{
    Task<CompanyDetailDto> GetDefaultAsync(Guid companyId, CancellationToken ct);
    Task<CompanyDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<CompanySummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<CompanyDetailDto> UpdateAsync(Guid id, CompanyUpdateDto dto, Guid companyId, CancellationToken ct);
    Task SwitchCompanyAsync(Guid companyId, CancellationToken ct);
}
