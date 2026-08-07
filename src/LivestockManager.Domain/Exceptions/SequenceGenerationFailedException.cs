namespace LivestockManager.Domain.Exceptions;

public class SequenceGenerationFailedException : DomainException
{
    public string? Prefix { get; }

    public SequenceGenerationFailedException()
    {
    }

    public SequenceGenerationFailedException(string prefix)
        : base($"Failed to generate sequence for prefix '{prefix}'.")
    {
        Prefix = prefix;
    }

    public SequenceGenerationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
