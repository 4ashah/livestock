using LivestockManager.Domain.ValueObjects;

namespace LivestockManager.Application.DTOs.Farms;

public class FarmDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public bool IsActive { get; set; }
    public int LivestockCount { get; set; }
    public int ActiveLivestockCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
