using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Customers;

public class CustomerCreateDto
{
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBusiness { get; set; } = false;
    public string? TaxNumber { get; set; }
    public Address? BillingAddress { get; set; }
    public Address? DeliveryAddress { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; } = 30;
    public decimal OpeningBalance { get; set; } = 0;
    public string? Notes { get; set; }
}
