namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockWeightHistoryDto
{
    public Guid Id { get; set; }
    public Guid LivestockId { get; set; }
    public decimal Weight { get; set; }
    public WeightUnit Unit { get; set; }
    public DateTimeOffset WeighedAt { get; set; }
    public string? Notes { get; set; }
}
