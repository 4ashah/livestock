namespace LivestockManager.Domain.Exceptions;

public class LivestockAlreadySoldException : DomainException
{
    public Guid LivestockId { get; }

    public LivestockAlreadySoldException()
    {
    }

    public LivestockAlreadySoldException(Guid livestockId)
        : base($"Livestock with ID {livestockId} has already been sold.")
    {
        LivestockId = livestockId;
    }

    public LivestockAlreadySoldException(string message)
        : base(message)
    {
    }

    public LivestockAlreadySoldException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
