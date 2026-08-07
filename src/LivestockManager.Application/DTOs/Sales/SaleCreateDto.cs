namespace LivestockManager.Application.DTOs.Sales;

public class SaleCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Notes { get; set; }
    public IList<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();
}
