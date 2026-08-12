using LivestockManager.Application.DTOs.StockAddition;

namespace LivestockManager.Application.Services.StockAddition;

public interface IStockAdditionIdempotencyCache
{
    StockAdditionResultDto? TryGet(Guid companyId, Guid idempotencyKey);
    void Set(Guid companyId, Guid idempotencyKey, StockAdditionResultDto result);
}

public class StockAdditionIdempotencyCache : IStockAdditionIdempotencyCache
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CacheEntry> _cache = new();

    private record CacheEntry(StockAdditionResultDto Result, DateTimeOffset ExpiresAt);

    public StockAdditionResultDto? TryGet(Guid companyId, Guid idempotencyKey)
    {
        var key = BuildKey(companyId, idempotencyKey);
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTimeOffset.UtcNow <= entry.ExpiresAt)
            {
                return entry.Result;
            }
            _cache.TryRemove(key, out _);
        }
        return null;
    }

    public void Set(Guid companyId, Guid idempotencyKey, StockAdditionResultDto result)
    {
        var key = BuildKey(companyId, idempotencyKey);
        var entry = new CacheEntry(result, DateTimeOffset.UtcNow.AddMinutes(30));
        _cache.AddOrUpdate(key, entry, (_, _) => entry);

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(35));
            _cache.TryRemove(key, out _);
        });
    }

    private static string BuildKey(Guid companyId, Guid idempotencyKey)
        => $"{companyId:N}_{idempotencyKey:N}";
}
