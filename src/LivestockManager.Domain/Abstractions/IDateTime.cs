namespace LivestockManager.Domain.Abstractions;

public interface IDateTime
{
    DateTimeOffset Now { get; }
}
