namespace LivestockManager.Application.DTOs.Companies;

public class CompanySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public bool IsActive { get; set; }
}
