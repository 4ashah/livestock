namespace LivestockManager.Application.DTOs.Reports;

public class OutstandingInvoiceRowDto
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTimeOffset InvoiceDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal OutstandingAmount { get; set; }
}
