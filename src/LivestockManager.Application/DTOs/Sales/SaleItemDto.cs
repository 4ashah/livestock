using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.Sales;

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid? LivestockId { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public decimal? SuggestedPrice { get; set; }
    public SuggestedPricingMethod? SuggestedPriceMethod { get; set; }
    public decimal? SuggestedWeight { get; set; }
    public DateTimeOffset? SuggestedWeightDate { get; set; }
    public decimal? SuggestedRate { get; set; }
    public decimal FinalSalePrice { get; set; }
    public PriceSource PriceSource { get; set; } = PriceSource.NotSet;
    public decimal AllocatedCommission { get; set; }
    public decimal AllocatedSellerTax { get; set; }
    public decimal AllocatedTransportation { get; set; }
    public decimal AllocatedOtherCost { get; set; }
    public decimal NetSaleProceeds { get; set; }
}
