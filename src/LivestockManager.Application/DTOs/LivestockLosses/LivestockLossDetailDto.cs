using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.LivestockLosses;

public class LivestockLossDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public string? FarmName { get; set; }
    public Guid LivestockId { get; set; }
    public string? LivestockCode { get; set; }
    public string? LivestockDisplayId { get; set; }
    public LivestockType LivestockType { get; set; }
    public string LossNumber { get; set; } = string.Empty;
    public DischargeCondition LossType { get; set; }
    public DateTimeOffset LossDate { get; set; }
    public Currency Currency { get; set; }
    public decimal BookValue { get; set; }
    public decimal? SalvageValue { get; set; }
    public decimal LossAmount { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
