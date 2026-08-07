using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Receipt : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid PaymentId { get; set; }

    public Guid CustomerId { get; set; }

    [MaxLength(50)]
    public string? ReceiptNumber { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset ReceiptDate { get; set; }

    [MaxLength(200)]
    public string? CustomerName { get; set; }

    [MaxLength(500)]
    public string? CustomerAddress { get; set; }

    [MaxLength(50)]
    public string? CustomerTaxNumber { get; set; }

    [Precision(18, 2)]
    public decimal AmountReceived { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    [Precision(18, 2)]
    public decimal RunningInvoiceBalance { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? GeneratedByUserId { get; set; }

    public ReceiptStatus Status { get; set; } = ReceiptStatus.Issued;

    [MaxLength(1000)]
    public string? ReversalReason { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset? ReversedAt { get; set; }

    public Guid? ReversedByUserId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public virtual Payment? Payment { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    protected Receipt()
    {
    }

    public Receipt(Guid companyId, Guid paymentId, Guid customerId, DateTimeOffset receiptDate, decimal amountReceived)
    {
        if (amountReceived <= 0)
            throw new ArgumentException("Amount received must be greater than zero.", nameof(amountReceived));
        CompanyId = companyId;
        PaymentId = paymentId;
        CustomerId = customerId;
        ReceiptDate = receiptDate;
        AmountReceived = amountReceived;
    }
}
