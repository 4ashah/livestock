using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.FarmViewModels;

public class FarmListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ManagerInfo { get; set; }
    public string? City { get; set; }
    public Currency Currency { get; set; }
    public bool IsActive { get; set; }
}
