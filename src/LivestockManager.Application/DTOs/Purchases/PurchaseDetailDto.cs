namespace LivestockManager.Application.DTOs.Purchases;

public class PurchaseDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid SupplierId { get; set; }
    public string? PurchaseNumber { get; set; }
    public string? SupplierReference { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public PurchaseStatus Status { get; set; }
    public Currency Currency { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Notes { get; set; }
    public Guid? DocumentId { get; set; }
    public IList<PurchaseItemLineDto> Items { get; set; } = new List<PurchaseItemLineDto>();
}
