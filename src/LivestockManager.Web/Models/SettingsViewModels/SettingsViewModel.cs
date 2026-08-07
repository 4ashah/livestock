using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.SettingsViewModels;

public class SettingsViewModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }

    [MaxLength(100)]
    [Display(Name = "Tax Number")]
    public string? TaxNumber { get; set; }

    [MaxLength(50)]
    [Phone]
    public string? Phone { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(10)]
    [Display(Name = "Invoice Prefix")]
    public string InvoicePrefix { get; set; } = "INV";

    [MaxLength(10)]
    [Display(Name = "Receipt Prefix")]
    public string ReceiptPrefix { get; set; } = "RCT";

    [Range(0.00, 1.00)]
    [Display(Name = "Default Tax Rate")]
    public decimal TaxRate { get; set; } = 0.15m;

    [Range(1, 12)]
    [Display(Name = "Financial Year Start Month")]
    public int FinancialYearStartMonth { get; set; } = 1;

    public Currency Currency { get; set; } = Currency.USD;

    [Display(Name = "Weight Unit")]
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [Display(Name = "Company Active")]
    public bool IsActive { get; set; } = true;
}
