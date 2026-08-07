namespace LivestockManager.Application.DTOs.Reports;

public class WeightChangeRowDto
{
    public Guid LivestockId { get; set; }
    public string LivestockDisplayId { get; set; } = string.Empty;
    public LivestockType Type { get; set; }
    public decimal InitialWeight { get; set; }
    public decimal CurrentWeight { get; set; }
    public decimal WeightGain { get; set; }
    public decimal WeightGainPercent { get; set; }
    public int DaysOwned { get; set; }
    public decimal AverageDailyGain { get; set; }
}
