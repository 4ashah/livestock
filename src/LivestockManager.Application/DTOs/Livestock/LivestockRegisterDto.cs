namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockRegisterDto
{
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public LivestockType LivestockType { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;
    public decimal PurchaseAmount { get; set; }
    public string? Comments { get; set; }
}
