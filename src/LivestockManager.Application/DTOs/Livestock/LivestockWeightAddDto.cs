namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockWeightAddDto
{
    public decimal Weight { get; set; }
    public WeightUnit Unit { get; set; } = WeightUnit.Kg;
    public DateTimeOffset WeighedAt { get; set; }
    public string? Notes { get; set; }
}
