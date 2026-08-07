using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Domain.Entities;

public class Expense : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    public Guid? FarmId { get; set; }

    public Guid? SupplierId { get; set; }

    public Guid? LivestockId { get; set; }

    public ExpenseCategory Category { get; set; }

    [Column(TypeName = "datetimeoffset")]
    public DateTimeOffset ExpenseDate { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(5, 2)]
    public decimal TaxRate { get; set; }

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Reference { get; set; }

    public Guid? DocumentId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    [ForeignKey(nameof(FarmId))]
    public virtual Farm? Farm { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(LivestockId))]
    public virtual Livestock? Livestock { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document? Document { get; set; }

    protected Expense()
    {
    }

    public Expense(Guid companyId, ExpenseCategory category, DateTimeOffset expenseDate, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        CompanyId = companyId;
        Category = category;
        ExpenseDate = expenseDate;
        Amount = amount;
    }
}
