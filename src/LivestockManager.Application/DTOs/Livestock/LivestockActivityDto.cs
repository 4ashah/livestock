namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockActivityDto
{
    public Guid Id { get; set; }
    public Guid LivestockId { get; set; }
    public LivestockActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
    public string? Metadata { get; set; }
}
