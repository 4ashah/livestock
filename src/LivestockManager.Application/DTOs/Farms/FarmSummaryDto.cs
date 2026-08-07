namespace LivestockManager.Application.DTOs.Farms;

public class FarmSummaryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? City { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public bool IsActive { get; set; }
}
