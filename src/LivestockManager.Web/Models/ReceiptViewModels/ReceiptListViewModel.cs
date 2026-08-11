using LivestockManager.Application.DTOs.Receipts;

namespace LivestockManager.Web.Models.ReceiptViewModels;

public class ReceiptListViewModel
{
    public IList<ReceiptSummaryDto> Items { get; set; } = new List<ReceiptSummaryDto>();
}
