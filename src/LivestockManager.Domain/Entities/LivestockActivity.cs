using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class LivestockActivity : BaseAuditableEntity
{
    public Guid LivestockId { get; set; }

    public LivestockActivityType ActivityType { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset PerformedAt { get; set; }

    public Guid? PerformedByUserId { get; set; }

    [MaxLength(4000)]
    public string? Metadata { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    protected LivestockActivity()
    {
    }

    public LivestockActivity(Guid livestockId, LivestockActivityType activityType, DateTimeOffset performedAt, string? description = null)
    {
        LivestockId = livestockId;
        ActivityType = activityType;
        PerformedAt = performedAt;
        Description = description;
    }
}
