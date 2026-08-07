namespace LivestockManager.Application.DTOs.Purchases;

public class PurchaseItemLineDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public PurchaseItemType ItemType { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitWeight { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? LivestockId { get; set; }
}
