namespace LivestockManager.Application.DTOs.Livestock;

public class LivestockDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public string? FarmName { get; set; }
    public string LivestockId { get; set; } = string.Empty;
    public LivestockType LivestockTypeId { get; set; }
    public StockSource? StockSource { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal AllocatedCommission { get; set; }
    public decimal AllocatedTax { get; set; }
    public decimal AllocatedTransportation { get; set; }
    public decimal AllocatedOtherCost { get; set; }
    public string? OtherCostDescription { get; set; }
    public decimal TotalAcquisitionCost { get; set; }
    public decimal? CurrentWeight { get; set; }
    public DateTimeOffset? CurrentWeightDate { get; set; }
    public LivestockStatus Status { get; set; }
    public string? Comments { get; set; }
    public string? MotherLivestockIdCode { get; set; }
    public string? FatherLivestockIdCode { get; set; }
    public DateTimeOffset? DischargeDate { get; set; }
    public DischargeCondition? DischargeCondition { get; set; }
    public string? DischargeDetails { get; set; }
    public decimal? SoldAmount { get; set; }
    public decimal? BasicProfitLoss { get; set; }
    public decimal? CompleteProfitLoss { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public IList<LivestockWeightHistoryDto> WeightHistory { get; set; } = new List<LivestockWeightHistoryDto>();
    public IList<LivestockActivityDto> Activities { get; set; } = new List<LivestockActivityDto>();
}
