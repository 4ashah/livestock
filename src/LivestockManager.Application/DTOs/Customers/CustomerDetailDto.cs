using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Customers;

public class CustomerDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBusiness { get; set; }
    public string? TaxNumber { get; set; }
    public Address? BillingAddress { get; set; }
    public Address? DeliveryAddress { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
