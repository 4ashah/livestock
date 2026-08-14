namespace LivestockManager.Application.DTOs.Reports;

public class SalesByPeriodSaleDetailDto
{
    public DateTimeOffset SaleDate { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public Guid? FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int NumberSold { get; set; }
    public decimal GrossSaleAmount { get; set; }
    public SaleStatus Status { get; set; }
    public Guid SaleId { get; set; }
}
