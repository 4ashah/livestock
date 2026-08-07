namespace LivestockManager.Application.Common;

public interface IDbContextTransactionFacade : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
