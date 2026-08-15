namespace LivestockManager.Infrastructure.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    Guid? CompanyId { get; }

    bool IsSystemAdministrator { get; }

    Task<List<Guid>> GetAccessibleFarmIdsAsync(CancellationToken cancellationToken);
}
