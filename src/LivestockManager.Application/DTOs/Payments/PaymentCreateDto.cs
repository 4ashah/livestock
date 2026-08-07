namespace LivestockManager.Application.DTOs.Payments;

public class PaymentCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public IList<PaymentAllocationDto> Allocations { get; set; } = new List<PaymentAllocationDto>();
}
