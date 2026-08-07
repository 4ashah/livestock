namespace LivestockManager.Application.DTOs.Payments;

public class PaymentDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public IList<PaymentAllocationDto> Allocations { get; set; } = new List<PaymentAllocationDto>();
}

public class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset AllocatedAt { get; set; }
}
