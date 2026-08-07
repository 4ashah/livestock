using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Domain.Entities;

public class Company : BaseAuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(100)]
    public string? TaxNumber { get; set; }

    public Address? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? LogoBlobName { get; set; }

    public Currency Currency { get; set; } = Currency.USD;

    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [MaxLength(10)]
    public string InvoicePrefix { get; set; } = "INV";

    [MaxLength(10)]
    public string ReceiptPrefix { get; set; } = "RCT";

    public TaxSettings? TaxSettings { get; set; }

    [Precision(5, 4)]
    [Range(0.00, 1.00)]
    public decimal TaxRate { get; set; } = 0.15m;

    [Range(1, 12)]
    public int FinancialYearStartMonth { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public virtual List<Farm> Farms { get; set; } = new();

    public virtual List<Livestock> Livestock { get; set; } = new();

    public virtual List<Customer> Customers { get; set; } = new();

    public virtual List<Sale> Sales { get; set; } = new();

    public virtual List<Invoice> Invoices { get; set; } = new();

    public virtual List<Payment> Payments { get; set; } = new();

    public virtual List<SequenceCounter> SequenceCounters { get; set; } = new();

    protected Company()
    {
    }

    public Company(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name cannot be empty.", nameof(name));
        Name = name;
    }
}
