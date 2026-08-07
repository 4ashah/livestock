namespace LivestockManager.Application.DTOs.Payments;

public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public bool IsReversed { get; set; }
}
