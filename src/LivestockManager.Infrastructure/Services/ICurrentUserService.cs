namespace LivestockManager.Infrastructure.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}