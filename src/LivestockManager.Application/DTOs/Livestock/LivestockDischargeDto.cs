namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockDischargeDto
{
    public DischargeCondition Condition { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Details { get; set; }
    public decimal? SoldAmount { get; set; }
}
