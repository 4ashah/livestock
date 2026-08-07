namespace LivestockManager.Application.DTOs.Customers;

public class CustomerSummaryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBusiness { get; set; }
    public string? TaxNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
}
