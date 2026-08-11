using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class LivestockLoss : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? FarmId { get; set; }

    public Guid LivestockId { get; set; }

    [Required]
    [MaxLength(32)]
    public string LossNumber { get; set; } = string.Empty;

    public DischargeCondition LossType { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset LossDate { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    [Precision(18, 2)]
    public decimal BookValue { get; set; }

    [Precision(18, 2)]
    public decimal? SalvageValue { get; set; }

    [Precision(18, 2)]
    public decimal LossAmount { get; set; }

    [MaxLength(2000)]
    public string? Reason { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool IsReversed { get; set; }

    [MaxLength(2000)]
    public string? ReversalReason { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? ReversedAt { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(FarmId))]
    public virtual Farm? Farm { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    protected LivestockLoss()
    {
    }

    public LivestockLoss(
        Guid companyId,
        Guid livestockId,
        string lossNumber,
        DischargeCondition lossType,
        DateTimeOffset lossDate,
        decimal bookValue,
        decimal? salvageValue,
        Guid? farmId = null)
    {
        if (string.IsNullOrWhiteSpace(lossNumber))
            throw new ArgumentException("Loss number cannot be empty.", nameof(lossNumber));
        if (bookValue < 0)
            throw new ArgumentException("Book value cannot be negative.", nameof(bookValue));
        if (salvageValue.HasValue && salvageValue.Value < 0)
            throw new ArgumentException("Salvage value cannot be negative.", nameof(salvageValue));
        if (lossType == DischargeCondition.Sold)
            throw new ArgumentException("LossType cannot be Sold. Use Sales module for sales.", nameof(lossType));

        CompanyId = companyId;
        FarmId = farmId;
        LivestockId = livestockId;
        LossNumber = lossNumber;
        LossType = lossType;
        LossDate = lossDate;
        BookValue = bookValue;
        SalvageValue = salvageValue;
        LossAmount = bookValue - (salvageValue ?? 0m);
    }

    public void Reverse(string reason)
    {
        if (IsReversed)
            throw new InvalidOperationException("Loss record has already been reversed.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reversal reason is required.", nameof(reason));

        IsReversed = true;
        ReversalReason = reason;
        ReversedAt = DateTimeOffset.UtcNow;
    }
}
