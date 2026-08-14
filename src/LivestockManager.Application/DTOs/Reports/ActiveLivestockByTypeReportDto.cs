namespace LivestockManager.Application.DTOs.Reports;

public class ActiveLivestockByTypeReportDto
{
    public LivestockType LivestockType { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public int CountFilteredTotalContext { get; set; } = 0;
}
