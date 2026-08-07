using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Domain.Entities;

public class Customer : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TaxNumber { get; set; }

    public Address? BillingAddress { get; set; }

    public Address? DeliveryAddress { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsBusiness { get; set; } = false;

    [Precision(18, 2)]
    public decimal CreditLimit { get; set; }

    public int PaymentTermsDays { get; set; } = 30;

    [Precision(18, 2)]
    public decimal OpeningBalance { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    public virtual List<Sale> Sales { get; set; } = new();

    public virtual List<Invoice> Invoices { get; set; } = new();

    public virtual List<Payment> Payments { get; set; } = new();

    protected Customer()
    {
    }

    public Customer(Guid companyId, string customerCode, string name)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            throw new ArgumentException("Customer code cannot be empty.", nameof(customerCode));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty.", nameof(name));
        CompanyId = companyId;
        CustomerCode = customerCode;
        Name = name;
    }
}
