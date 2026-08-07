using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;

namespace LivestockManager.Domain.Entities;

public class InvoiceItem : BaseAuditableEntity
{
    public Guid InvoiceId { get; set; }

    public Guid? SaleItemId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Precision(18, 4)]
    public decimal Quantity { get; set; }

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(5, 4)]
    public decimal DiscountPercent { get; set; }

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; }

    [Precision(5, 4)]
    public decimal TaxPercent { get; set; }

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(18, 2)]
    public decimal LineTotal
    {
        get
        {
            var baseAmount = Quantity * UnitPrice - DiscountAmount;
            return baseAmount + TaxAmount;
        }
    }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice? Invoice { get; set; }

    [ForeignKey(nameof(SaleItemId))]
    public virtual SaleItem? SaleItem { get; set; }

    protected InvoiceItem()
    {
    }

    public InvoiceItem(Guid invoiceId, string description, decimal quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
