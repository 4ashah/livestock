namespace LivestockManager.Application.DTOs.Reports;

public class SalesByPeriodFarmSummaryDto
{
    public Guid? FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public int NumberSold { get; set; }
    public decimal GrossSalesAmount { get; set; }
    public decimal? AdditionalSaleCosts { get; set; }
    public decimal? NetSaleProceeds { get; set; }
}
