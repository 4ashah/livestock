using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class SaleItem : BaseAuditableEntity
{
    public Guid SaleId { get; set; }

    public Guid? LivestockId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Precision(18, 4)]
    public decimal Quantity { get; set; } = 1;

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(5, 4)]
    public decimal DiscountPercent { get; set; } = 0;

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; } = 0;

    [Precision(5, 4)]
    public decimal TaxPercent { get; set; } = 0;

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; } = 0;

    [Precision(18, 2)]
    public decimal? SuggestedPrice { get; set; }

    public SuggestedPricingMethod? SuggestedPriceMethod { get; set; }

    [Precision(18, 4)]
    public decimal? SuggestedWeight { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? SuggestedWeightDate { get; set; }

    [Precision(18, 4)]
    public decimal? SuggestedRate { get; set; }

    [Precision(18, 2)]
    public decimal FinalSalePrice { get; set; }

    public PriceSource PriceSource { get; set; } = PriceSource.NotSet;

    [Precision(18, 2)]
    public decimal AllocatedCommission { get; set; }

    [Precision(18, 2)]
    public decimal AllocatedSellerTax { get; set; }

    [Precision(18, 2)]
    public decimal AllocatedTransportation { get; set; }

    [Precision(18, 2)]
    public decimal AllocatedOtherCost { get; set; }

    [Precision(18, 2)]
    public decimal NetSaleProceeds { get; set; }

    [Precision(18, 2)]
    public decimal LineTotal
    {
        get
        {
            var baseAmount = Quantity * UnitPrice - DiscountAmount;
            return baseAmount + TaxAmount;
        }
    }

    [ForeignKey(nameof(SaleId))]
    public virtual Sale? Sale { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    protected SaleItem()
    {
    }

    public SaleItem(Guid saleId, decimal unitPrice, decimal quantity = 1, string? description = null)
    {
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        SaleId = saleId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Description = description;
    }
}
