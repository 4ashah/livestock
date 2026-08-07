using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.FarmViewModels;

public class FarmDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string? Street1 { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public Currency Currency { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public bool IsActive { get; set; }

    public string? ManagerInfo { get; set; }

    public int LivestockCount { get; set; }
    public int ActiveLivestockCount { get; set; }
}
