using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivestockManager.Domain.Common;

public abstract class BaseAuditableEntity
{
    [Key]
    public Guid Id { get; protected set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Timestamp]
    public byte[]? Version { get; set; }

    protected BaseAuditableEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
