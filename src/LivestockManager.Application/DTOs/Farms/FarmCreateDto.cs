using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Farms;

public class FarmCreateDto
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Currency Currency { get; set; } = Currency.USD;
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;
}
