using LivestockManager.Application.DTOs.Customers;

namespace LivestockManager.Application.Services.Customers;

public interface ICustomerService
{
    Task<CustomerDetailDto> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);
    Task<IList<CustomerSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<IList<CustomerSummaryDto>> SearchAsync(Guid companyId, string keyword, CancellationToken ct);
    Task<CustomerDetailDto> CreateAsync(CustomerCreateDto dto, Guid companyId, CancellationToken ct);
    Task<CustomerDetailDto> UpdateAsync(Guid id, CustomerUpdateDto dto, Guid companyId, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid companyId, CancellationToken ct);
}
