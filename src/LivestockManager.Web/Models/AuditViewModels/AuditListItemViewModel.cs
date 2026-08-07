namespace LivestockManager.Web.Models.AuditViewModels;

public class AuditListItemViewModel
{
    public Guid Id { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
}
