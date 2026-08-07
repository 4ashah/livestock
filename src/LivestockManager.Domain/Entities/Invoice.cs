using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;

namespace LivestockManager.Domain.Entities;

public class Invoice : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? SaleId { get; set; }

    public Guid CustomerId { get; set; }

    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset InvoiceDate { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset DueDate { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal DiscountTotal { get; set; }

    [Precision(18, 2)]
    public decimal TaxTotal { get; set; }

    [Precision(18, 2)]
    public decimal ChargeTotal { get; set; }

    [Precision(18, 2)]
    public decimal GrandTotal { get; set; }

    [Precision(18, 2)]
    public decimal PaidAmount { get; set; } = 0;

    [Precision(18, 2)]
    public decimal OutstandingAmount => GrandTotal - PaidAmount;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [MaxLength(4000)]
    public string? CustomerSnapshot { get; set; }

    [MaxLength(4000)]
    public string? CompanySnapshot { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    [MaxLength(500)]
    public string? PdfBlobName { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(SaleId))]
    public virtual Sale? Sale { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public virtual List<InvoiceItem> Items { get; set; } = new();

    public virtual List<Payment> Payments { get; set; } = new();

    protected Invoice()
    {
    }

    public Invoice(Guid companyId, Guid customerId, DateTimeOffset invoiceDate, DateTimeOffset dueDate)
    {
        if (dueDate < invoiceDate)
            throw new ArgumentException("Due date cannot be earlier than invoice date.", nameof(dueDate));
        CompanyId = companyId;
        CustomerId = customerId;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
    }

    public void ApplyPayment(decimal paymentAmount)
    {
        if (paymentAmount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(paymentAmount));

        if (paymentAmount - OutstandingAmount > 0.0001m)
            throw new PaymentOverAllocatedException($"Payment of {paymentAmount} exceeds outstanding amount of {OutstandingAmount}.");

        PaidAmount += paymentAmount;

        if (GrandTotal - PaidAmount <= 0.0001m)
        {
            Status = InvoiceStatus.Paid;
        }
        else if (PaidAmount > 0.0001m)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }
    }
}
