using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.LivestockLosses;

public class LivestockLossCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid LivestockId { get; set; }
    public DischargeCondition LossType { get; set; }
    public DateTimeOffset LossDate { get; set; }
    public Currency? Currency { get; set; }
    public decimal BookValue { get; set; }
    public decimal? SalvageValue { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
