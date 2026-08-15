using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;
using LivestockManager.Application.DTOs.Livestock;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockDetailsViewModel
{
    public Guid Id { get; set; }
    public string LivestockId { get; set; } = string.Empty;
    public LivestockType Type { get; set; }
    public string? FarmName { get; set; }
    public Guid? FarmId { get; set; }
    public StockSource StockSource { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public Guid? MotherLivestockId { get; set; }
    public Guid? FatherLivestockId { get; set; }
    public string? MotherLivestockIdCode { get; set; }
    public string? FatherLivestockIdCode { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public WeightUnit InitialWeightUnit { get; set; }
    public decimal? CurrentWeight { get; set; }
    public DateTimeOffset? CurrentWeightDate { get; set; }
    public LivestockStatus Status { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal AllocatedCommission { get; set; }
    public decimal AllocatedTax { get; set; }
    public decimal AllocatedTransportation { get; set; }
    public decimal AllocatedOtherCost { get; set; }
    public string? OtherCostDescription { get; set; }
    public decimal TotalAcquisitionCost { get; set; }
    public decimal? SoldAmount { get; set; }
    public decimal? BasicProfitLoss { get; set; }
    public string? Comments { get; set; }
    public DateTimeOffset? DischargeDate { get; set; }
    public DischargeCondition? DischargeCondition { get; set; }
    public string? DischargeDetails { get; set; }

    public List<LivestockWeightHistoryItemViewModel> Weights { get; set; } = new();
    public List<LivestockActivityDto> Activities { get; set; } = new();

    [System.ComponentModel.DataAnnotations.MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters.")]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
    public string? NewComment { get; set; }
}
