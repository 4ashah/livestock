namespace LivestockManager.Application.DTOs.Receipts;

public class ReceiptDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTimeOffset ReceiptDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerTaxNumber { get; set; }
    public decimal AmountReceived { get; set; }
    public Currency Currency { get; set; }
    public decimal RunningInvoiceBalance { get; set; }
    public string? Notes { get; set; }
    public Guid? GeneratedByUserId { get; set; }
    public ReceiptStatus Status { get; set; }
    public string? ReversalReason { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public Guid? ReversedByUserId { get; set; }
    public string? InvoiceNumber { get; set; }
}
