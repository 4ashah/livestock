namespace LivestockManager.Application.DTOs.Sales;

public class SaleSummaryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SaleNumber { get; set; }
    public DateTimeOffset Date { get; set; }
    public decimal GrandTotal { get; set; }
    public SaleStatus Status { get; set; }
}
