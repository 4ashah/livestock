using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Sale : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? FarmId { get; set; }

    public Guid CustomerId { get; set; }

    [MaxLength(50)]
    public string? SaleNumber { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset Date { get; set; }

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

    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(FarmId))]
    public virtual Farm? Farm { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public virtual List<SaleItem> Items { get; set; } = new();

    public virtual List<Invoice> Invoices { get; set; } = new();

    protected Sale()
    {
    }

    public Sale(Guid companyId, Guid customerId, DateTimeOffset date)
    {
        CompanyId = companyId;
        CustomerId = customerId;
        Date = date;
    }
}
