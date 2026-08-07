namespace LivestockManager.Application.DTOs.Purchases;

public class PurchaseCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public string? SupplierReference { get; set; }
    public Currency? Currency { get; set; }
    public decimal? DiscountPct { get; set; }
    public decimal? TaxRate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Notes { get; set; }
    public Guid? DocumentId { get; set; }
    public IList<PurchaseItemCreateDto> Items { get; set; } = new List<PurchaseItemCreateDto>();
}
