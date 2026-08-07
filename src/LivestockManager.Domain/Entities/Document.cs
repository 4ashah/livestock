using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Document : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    [MaxLength(20)]
    public string? Extension { get; set; }

    public DocumentType DocumentType { get; set; }

    public Guid? UploadedByUserId { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset UploadedAt { get; set; }

    [MaxLength(50)]
    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    protected Document()
    {
    }

    public Document(Guid companyId, string displayName, string storagePath, DocumentType documentType, DateTimeOffset uploadedAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty.", nameof(storagePath));
        CompanyId = companyId;
        DisplayName = displayName;
        StoragePath = storagePath;
        DocumentType = documentType;
        UploadedAt = uploadedAt;
    }
}
