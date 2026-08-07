namespace LivestockManager.Application.DTOs.Receipts;

public class ReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTimeOffset ReceiptDate { get; set; }
    public Guid PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal AmountReceived { get; set; }
    public Currency Currency { get; set; }
    public decimal RunningInvoiceBalance { get; set; }
    public ReceiptStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
