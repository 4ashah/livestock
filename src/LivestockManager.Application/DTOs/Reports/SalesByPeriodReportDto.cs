namespace LivestockManager.Application.DTOs.Reports;

public class SalesByPeriodReportDto
{
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public int TotalNumberSold { get; set; }
    public decimal TotalGrossSalesAmount { get; set; }
    public decimal TotalAdditionalSaleCosts { get; set; }
    public decimal TotalNetSaleProceeds { get; set; }
    public IList<SalesByPeriodFarmSummaryDto> FarmSummaries { get; set; } = new List<SalesByPeriodFarmSummaryDto>();
    public IList<SalesByPeriodSaleDetailDto> SaleDetails { get; set; } = new List<SalesByPeriodSaleDetailDto>();
}
