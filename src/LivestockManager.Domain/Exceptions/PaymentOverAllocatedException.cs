namespace LivestockManager.Domain.Exceptions;

public class PaymentOverAllocatedException : DomainException
{
    public PaymentOverAllocatedException()
    {
    }

    public PaymentOverAllocatedException(string message)
        : base(message)
    {
    }

    public PaymentOverAllocatedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
