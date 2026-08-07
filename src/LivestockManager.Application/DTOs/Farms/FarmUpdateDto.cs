using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Farms;

public class FarmUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public bool IsActive { get; set; } = true;
}
