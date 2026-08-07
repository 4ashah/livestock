using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class LivestockWeight : BaseAuditableEntity
{
    public Guid LivestockId { get; set; }

    [Precision(18, 4)]
    public decimal Weight { get; set; }

    public WeightUnit Unit { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset WeighedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    protected LivestockWeight()
    {
    }

    public LivestockWeight(Guid livestockId, decimal weight, WeightUnit unit, DateTimeOffset weighedAt)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        LivestockId = livestockId;
        Weight = weight;
        Unit = unit;
        WeighedAt = weighedAt;
    }

    public static LivestockWeight CreateValidated(Guid livestockId, decimal weight, WeightUnit unit, DateTimeOffset weighedAt)
    {
        if (weight <= 0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(weight));
        if (weighedAt > DateTimeOffset.Now.AddMinutes(1))
            throw new ArgumentException("WeighedAt cannot be in the future.", nameof(weighedAt));

        return new LivestockWeight(livestockId, weight, unit, weighedAt);
    }
}
