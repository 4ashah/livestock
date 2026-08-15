using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockWeightHistoryItemViewModel
{
    public Guid LivestockWeightId { get; set; }
    public decimal Weight { get; set; }
    public WeightUnit Unit { get; set; }
    public DateTimeOffset WeighedAt { get; set; }
    public string? Notes { get; set; }
}
