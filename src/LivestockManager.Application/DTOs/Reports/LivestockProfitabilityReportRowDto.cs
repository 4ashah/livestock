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
}
