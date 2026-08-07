using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Companies;

public class CompanyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public Address? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Currency Currency { get; set; } = Currency.USD;
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;
    public string InvoicePrefix { get; set; } = "INV";
    public string ReceiptPrefix { get; set; } = "RCT";
    public TaxSettings? TaxSettings { get; set; }
    public int FinancialYearStartMonth { get; set; } = 1;
}
