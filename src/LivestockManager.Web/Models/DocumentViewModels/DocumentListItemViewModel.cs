using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.DocumentViewModels;

public class DocumentListItemViewModel
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long SizeBytes { get; set; }

    public string SizeFriendly { get; set; } = string.Empty;

    public string? Extension { get; set; }

    public DocumentType DocumentType { get; set; }

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public Guid? UploadedByUserId { get; set; }
}
