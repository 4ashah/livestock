using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Companies;

public class CompanyDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public Address? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoBlobName { get; set; }
    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public string InvoicePrefix { get; set; } = string.Empty;
    public string ReceiptPrefix { get; set; } = string.Empty;
    public TaxSettings? TaxSettings { get; set; }
    public int FinancialYearStartMonth { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
