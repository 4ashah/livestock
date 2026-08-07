using LivestockManager.Application.DTOs.Customers;

namespace LivestockManager.Application.Services.Customers;

public interface ICustomerBalanceService
{
    Task<CustomerBalanceDto> GetBalanceAsync(Guid companyId, Guid customerId, CancellationToken ct);
    Task<IList<CustomerBalanceDto>> ListBalancesAsync(Guid companyId, CancellationToken ct);
    Task<CustomerStatementDto> GetStatementAsync(Guid companyId, Guid customerId, DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken ct);
    Task<IList<AgingBucketDto>> GetAgingBucketsAsync(Guid companyId, Guid customerId, CancellationToken ct);
}
