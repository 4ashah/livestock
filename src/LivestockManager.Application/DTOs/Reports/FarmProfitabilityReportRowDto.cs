namespace LivestockManager.Application.DTOs.Reports;

public class FarmProfitabilityReportRowDto
{
    public Guid FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public int ActiveLivestockCount { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
}
