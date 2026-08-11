using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.LivestockLosses;

public class LivestockLossSummaryDto
{
    public Guid Id { get; set; }
    public string LossNumber { get; set; } = string.Empty;
    public DateTimeOffset LossDate { get; set; }
    public DischargeCondition LossType { get; set; }
    public Guid? FarmId { get; set; }
    public string? FarmName { get; set; }
    public Guid LivestockId { get; set; }
    public string LivestockCode { get; set; } = string.Empty;
    public string? LivestockDisplayId { get; set; }
    public LivestockType LivestockType { get; set; }
    public decimal BookValue { get; set; }
    public decimal? SalvageValue { get; set; }
    public decimal LossAmount { get; set; }
    public string? Reason { get; set; }
    public bool IsReversed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
