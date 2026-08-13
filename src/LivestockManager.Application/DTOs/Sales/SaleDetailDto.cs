namespace LivestockManager.Application.DTOs.Sales;

public class SaleDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public string? FarmName { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SaleNumber { get; set; }
    public DateTimeOffset Date { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal ChargeTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public SaleStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public IList<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
}
