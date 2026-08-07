using LivestockManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockIndexViewModel
{
    public Guid? FarmId { get; set; }
    public LivestockType? LivestockTypeId { get; set; }
    public LivestockStatus? Status { get; set; }
    public string? SearchString { get; set; }
    public List<LivestockListItemDto> Items { get; set; } = new();
    public SelectList? FarmOptions { get; set; }
}
