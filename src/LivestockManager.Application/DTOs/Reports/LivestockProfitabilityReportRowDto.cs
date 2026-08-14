namespace LivestockManager.Application.DTOs.Reports;

public class LivestockProfitabilityReportRowDto
{
    public Guid LivestockId { get; set; }
    public string LivestockDisplayId { get; set; } = string.Empty;
    public LivestockType Type { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal? SoldAmount { get; set; }
    public decimal DirectExpenses { get; set; }
    public decimal? BasicProfitLoss { get; set; }
    public decimal? CompleteProfitLoss { get; set; }
    public LivestockStatus Status { get; set; }
    public Guid? SaleFarmId { get; set; }
    public string? SaleFarmName { get; set; }
    public DateTimeOffset? SaleDate { get; set; }
    public decimal AdditionalAcquisitionCosts { get; set; }
    public decimal TotalAcquisitionCosts { get; set; }
    public decimal AdditionalSaleCosts { get; set; }
    public decimal NetSaleProceeds { get; set; }
}
