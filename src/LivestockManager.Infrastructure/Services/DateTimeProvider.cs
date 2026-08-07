using LivestockManager.Domain.Abstractions;

namespace LivestockManager.Infrastructure.Services;

public class DateTimeProvider : IDateTime
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
