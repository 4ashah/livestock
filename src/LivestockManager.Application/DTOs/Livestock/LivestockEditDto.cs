namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockEditDto
{
    public Guid? FarmId { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public string? Comments { get; set; }
}
