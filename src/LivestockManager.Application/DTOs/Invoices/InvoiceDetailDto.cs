namespace LivestockManager.Application.DTOs.Invoices;

public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public Currency Currency { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ChargeTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public IList<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
}
