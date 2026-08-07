namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockSummaryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public string LivestockId { get; set; } = string.Empty;
    public LivestockType LivestockTypeId { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal? CurrentWeight { get; set; }
    public DateTimeOffset? CurrentWeightDate { get; set; }
    public LivestockStatus Status { get; set; }
}
