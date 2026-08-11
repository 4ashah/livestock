namespace LivestockManager.Application.DTOs.Purchases;

public class PurchaseSummaryDto
{
    public Guid Id { get; set; }
    public string? PurchaseNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public PurchaseStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PurchaseListItemDto
{
    public Guid Id { get; set; }
    public string? PurchaseNumber { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public string? SupplierName { get; set; }
    public decimal GrandTotal { get; set; }
    public PurchaseStatus Status { get; set; }
    public int ItemCount { get; set; }
}
