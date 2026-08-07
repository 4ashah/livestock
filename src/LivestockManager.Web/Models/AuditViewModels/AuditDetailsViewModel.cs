namespace LivestockManager.Web.Models.AuditViewModels;

public class AuditDetailsViewModel
{
    public Guid Id { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
}
