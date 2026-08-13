using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Purchase : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? FarmId { get; set; }

    public Guid SupplierId { get; set; }

    [MaxLength(50)]
    public string? PurchaseNumber { get; set; }

    [MaxLength(200)]
    public string? SupplierReference { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset PurchaseDate { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

    public Currency Currency { get; set; } = Currency.USD;

    [Precision(5, 2)]
    public decimal DiscountPct { get; set; }

    [Precision(5, 2)]
    public decimal TaxRate { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal TaxTotal { get; set; }

    [Precision(18, 2)]
    public decimal GrandTotal { get; set; }

    [Precision(18, 2)]
    public decimal AmountPaid { get; set; }

    [Precision(18, 2)]
    public decimal OutstandingAmount { get; set; }

    public CostAllocationMethod CostAllocationMethod { get; set; } = CostAllocationMethod.Equal;

    [Precision(18, 2)]
    public decimal TotalLivestockPurchaseCost { get; set; }

    [Precision(18, 2)]
    public decimal TotalCommission { get; set; }

    [Precision(18, 2)]
    public decimal TotalTax { get; set; }

    [Precision(18, 2)]
    public decimal TotalTransportation { get; set; }

    [Precision(18, 2)]
    public decimal TotalOtherCost { get; set; }

    [MaxLength(500)]
    public string? OtherCostDescription { get; set; }

    [Precision(18, 2)]
    public decimal AdditionalAcquisitionCost { get; set; }

    [Precision(18, 2)]
    public decimal TotalAcquisitionCost { get; set; }

    [MaxLength(50)]
    public string? PaymentStatus { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? DocumentId { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(FarmId))]
    public virtual Farm? Farm { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    public virtual List<PurchaseItem> Items { get; set; } = new();

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }

    protected Purchase()
    {
    }

    public Purchase(Guid companyId, Guid supplierId, DateTimeOffset purchaseDate)
    {
        CompanyId = companyId;
        SupplierId = supplierId;
        PurchaseDate = purchaseDate;
    }

    public Purchase(Guid companyId, Guid? farmId, Guid supplierId, DateTimeOffset purchaseDate)
        : this(companyId, supplierId, purchaseDate)
    {
        FarmId = farmId;
    }
}
