using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;

namespace LivestockManager.Domain.Entities;

public class SequenceCounter : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Prefix { get; set; } = string.Empty;

    public long LastValue { get; set; } = 0;

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset LastUpdatedAt { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    protected SequenceCounter()
    {
    }

    public SequenceCounter(Guid companyId, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));
        CompanyId = companyId;
        Prefix = prefix;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }
}
