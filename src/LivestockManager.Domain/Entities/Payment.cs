using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Payment : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? InvoiceId { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset PaymentDate { get; set; }

    public PaymentMethod Method { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Reference { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public bool IsReversed { get; set; } = false;

    [MaxLength(1000)]
    public string? ReversalReason { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? ReversedAt { get; set; }

    public Guid? ReversedByUserId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice? Invoice { get; set; }

    protected Payment()
    {
    }

    public Payment(Guid companyId, Guid customerId, DateTimeOffset paymentDate, PaymentMethod method, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));
        CompanyId = companyId;
        CustomerId = customerId;
        PaymentDate = paymentDate;
        Method = method;
        Amount = amount;
    }

    public Payment(Guid companyId, Guid customerId, Guid? invoiceId, DateTimeOffset paymentDate, PaymentMethod method, decimal amount)
        : this(companyId, customerId, paymentDate, method, amount)
    {
        InvoiceId = invoiceId;
    }
}
