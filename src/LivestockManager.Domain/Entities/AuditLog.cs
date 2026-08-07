using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;

namespace LivestockManager.Domain.Entities;

public class AuditLog : BaseAuditableEntity
{
    public Guid? CompanyId { get; set; }

    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EntityType { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(8000)]
    public string? OldValuesJson { get; set; }

    [MaxLength(8000)]
    public string? NewValuesJson { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public new DateTimeOffset CreatedAt { get; set; }

    protected AuditLog()
    {
    }

    public AuditLog(string action, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        Action = action;
        CreatedAt = createdAt;
    }
}
