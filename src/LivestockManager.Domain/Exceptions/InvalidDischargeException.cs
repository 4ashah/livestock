namespace LivestockManager.Domain.Exceptions;

public class InvalidDischargeException : DomainException
{
    public InvalidDischargeException()
    {
    }

    public InvalidDischargeException(string message)
        : base(message)
    {
    }

    public InvalidDischargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
