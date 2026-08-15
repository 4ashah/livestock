using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockListItemViewModel
{
    public Guid Id { get; set; }
    public string LivestockId { get; set; } = string.Empty;
    public LivestockType LivestockType { get; set; }
    public string? FarmName { get; set; }
    public StockSource StockSource { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public DateTimeOffset AcquisitionDate { get; set; }
    public decimal InitialWeight { get; set; }
    public decimal? CurrentWeight { get; set; }
    public LivestockStatus Status { get; set; }
    public int DaysInHerd { get; set; }
    public decimal? SoldAmount { get; set; }
    public decimal? BasicProfitLoss { get; set; }
}
