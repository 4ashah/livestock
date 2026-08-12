using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using DomainEnums = LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;

namespace LivestockManager.Domain.Entities;

public class Livestock : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? FarmId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LivestockId { get; set; } = string.Empty;

    public DomainEnums.LivestockType LivestockTypeId { get; set; }

    public DomainEnums.StockSource StockSource { get; set; } = DomainEnums.StockSource.Purchased;

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? DateOfBirth { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset AcquisitionDate { get; set; }

    public Guid? MotherLivestockId { get; set; }

    public Guid? FatherLivestockId { get; set; }

    [Precision(18, 4)]
    public decimal InitialWeight { get; set; }

    public DomainEnums.WeightUnit WeightUnit { get; set; }

    [Precision(18, 2)]
    public decimal PurchaseAmount { get; set; }

    [Precision(18, 4)]
    public decimal? CurrentWeight { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? CurrentWeightDate { get; set; }

    public DomainEnums.LivestockStatus Status { get; set; } = DomainEnums.LivestockStatus.Active;

    [MaxLength(2000)]
    public string? Comments { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? DischargeDate { get; set; }

    public DomainEnums.DischargeCondition? DischargeCondition { get; set; }

    [MaxLength(2000)]
    public string? DischargeDetails { get; set; }

    [Precision(18, 2)]
    public decimal? SoldAmount { get; set; }

    public Guid? SaleItemId { get; set; }

    public Guid? SoldViaSaleItemId
    {
        get => SaleItemId;
        set => SaleItemId = value;
    }

    [Precision(18, 2)]
    public decimal? BasicProfitLoss
    {
        get
        {
            if (Status != DomainEnums.LivestockStatus.DischargedSold || !SoldAmount.HasValue)
                return null;
            return SoldAmount.Value - PurchaseAmount;
        }
    }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(FarmId))]
    public virtual Farm? Farm { get; set; }

    [ForeignKey(nameof(MotherLivestockId))]
    public virtual Livestock? Mother { get; set; }

    [ForeignKey(nameof(FatherLivestockId))]
    public virtual Livestock? Father { get; set; }

    public virtual SaleItem? SaleItem { get; set; }

    public virtual List<LivestockWeight> Weights { get; set; } = new();

    public virtual List<LivestockActivity> Activities { get; set; } = new();

    protected Livestock()
    {
    }

    public Livestock(
        Guid companyId,
        string livestockId,
        DomainEnums.LivestockType type,
        DateTimeOffset acquisitionDate,
        decimal initialWeight,
        DomainEnums.WeightUnit weightUnit,
        decimal purchaseAmount,
        Guid? farmId = null)
    {
        if (string.IsNullOrWhiteSpace(livestockId))
            throw new ArgumentException("Livestock ID cannot be empty.", nameof(livestockId));
        if (initialWeight <= 0)
            throw new ArgumentException("Initial weight must be greater than zero.", nameof(initialWeight));
        if (purchaseAmount < 0)
            throw new ArgumentException("Purchase amount cannot be negative.", nameof(purchaseAmount));
        if (type == DomainEnums.LivestockType.BredCastratedRam || type == DomainEnums.LivestockType.BredEwe)
        {
            if (purchaseAmount != 0)
                throw new ArgumentException("Purchase amount must be zero for bred livestock types.", nameof(purchaseAmount));
        }
        else if (type == DomainEnums.LivestockType.PurchasedCastratedRam || type == DomainEnums.LivestockType.UncastratedRam || type == DomainEnums.LivestockType.PurchasedEwe)
        {
            if (purchaseAmount <= 0)
                throw new ArgumentException("Purchase amount must be greater than zero for purchased livestock types.", nameof(purchaseAmount));
        }

        CompanyId = companyId;
        FarmId = farmId;
        LivestockId = livestockId;
        LivestockTypeId = type;
        AcquisitionDate = acquisitionDate;
        InitialWeight = initialWeight;
        WeightUnit = weightUnit;
        PurchaseAmount = purchaseAmount;
        Status = DomainEnums.LivestockStatus.Active;
    }

    public void Discharge(DomainEnums.DischargeCondition condition, DateTimeOffset date, decimal? soldAmount = null, string? details = null)
    {
        if (Status != DomainEnums.LivestockStatus.Active)
            throw new InvalidDischargeException("Livestock has already been discharged.");

        if (condition == DomainEnums.DischargeCondition.Sold)
        {
            if (!soldAmount.HasValue || soldAmount.Value <= 0)
                throw new ArgumentException("Sold livestock requires a positive SoldAmount.", nameof(soldAmount));
            SoldAmount = soldAmount.Value;
        }
        else
        {
            SoldAmount = null;
        }

        Status = condition switch
        {
            DomainEnums.DischargeCondition.Sold => DomainEnums.LivestockStatus.DischargedSold,
            DomainEnums.DischargeCondition.Deceased => DomainEnums.LivestockStatus.DischargedDeceased,
            DomainEnums.DischargeCondition.Lost => DomainEnums.LivestockStatus.DischargedLost,
            DomainEnums.DischargeCondition.Stolen => DomainEnums.LivestockStatus.DischargedStolen,
            _ => DomainEnums.LivestockStatus.DischargedOther
        };

        DischargeDate = date;
        DischargeCondition = condition;
        DischargeDetails = details;
    }
}
