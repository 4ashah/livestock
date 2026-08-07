using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;

namespace LivestockManager.Domain.Entities;

public class InvoiceAdditionalCharge : BaseAuditableEntity
{
    public Guid InvoiceId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(5, 2)]
    public decimal TaxRate { get; set; }

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice? Invoice { get; set; }

    protected InvoiceAdditionalCharge()
    {
    }

    public InvoiceAdditionalCharge(Guid invoiceId, string description, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        InvoiceId = invoiceId;
        Description = description;
        Amount = amount;
    }
}
