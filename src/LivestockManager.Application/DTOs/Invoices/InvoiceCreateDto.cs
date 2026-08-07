namespace LivestockManager.Application.DTOs.Invoices;

public class InvoiceCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public Currency Currency { get; set; } = Currency.USD;
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public IList<InvoiceItemDto> Items { get; set; } = new List<InvoiceItemDto>();
}
