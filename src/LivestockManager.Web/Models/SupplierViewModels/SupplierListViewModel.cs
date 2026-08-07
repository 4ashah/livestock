using LivestockManager.Application.DTOs.Suppliers;

namespace LivestockManager.Web.Models.SupplierViewModels;

public class SupplierListViewModel
{
    public IList<SupplierSummaryDto> Items { get; set; } = new List<SupplierSummaryDto>();
    public string? SearchTerm { get; set; }
    public bool? OnlyActive { get; set; }
}
