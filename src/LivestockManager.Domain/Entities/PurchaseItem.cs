using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class PurchaseItem : BaseAuditableEntity
{
    public Guid PurchaseId { get; set; }

    public int LineNo { get; set; }

    public PurchaseItemType ItemType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Quantity { get; set; }

    [Precision(18, 4)]
    public decimal UnitWeight { get; set; }

    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [Precision(18, 2)]
    public decimal UnitCost { get; set; }

    [Precision(5, 2)]
    public decimal DiscountPct { get; set; }

    [Precision(5, 2)]
    public decimal TaxRate { get; set; }

    [Precision(18, 2)]
    public decimal LineTotal { get; set; }

    [Precision(18, 2)]
    public decimal LivestockPurchaseCost { get; set; }

    [Precision(18, 2)]
    public decimal CommissionAmount { get; set; }

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(18, 2)]
    public decimal TransportationAmount { get; set; }

    [Precision(18, 2)]
    public decimal OtherCostAmount { get; set; }

    [MaxLength(500)]
    public string? OtherCostDescription { get; set; }

    [Precision(18, 2)]
    public decimal AdditionalAcquisitionCost { get; set; }

    [Precision(18, 2)]
    public decimal TotalAcquisitionCost { get; set; }

    public CostAllocationMethod CostAllocationMethod { get; set; } = CostAllocationMethod.Equal;

    public Guid? LivestockId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public virtual Purchase? Purchase { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    protected PurchaseItem()
    {
    }

    public PurchaseItem(Guid purchaseId, int lineNo, PurchaseItemType itemType, int quantity, decimal unitCost)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitCost < 0)
            throw new ArgumentException("Unit cost cannot be negative.", nameof(unitCost));
        PurchaseId = purchaseId;
        LineNo = lineNo;
        ItemType = itemType;
        Quantity = quantity;
        UnitCost = unitCost;
    }
}
