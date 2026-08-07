using LivestockManager.Application.DTOs.Customers;

namespace LivestockManager.Application.Services.Customers;

public interface ICustomerService
{
    Task<CustomerDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<CustomerSummaryDto>> ListAsync(Guid companyId, CancellationToken ct);
    Task<IList<CustomerSummaryDto>> SearchAsync(Guid companyId, string keyword, CancellationToken ct);
    Task<CustomerDetailDto> CreateAsync(CustomerCreateDto dto, CancellationToken ct);
    Task<CustomerDetailDto> UpdateAsync(Guid id, CustomerUpdateDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
