namespace LivestockManager.Application.DTOs.Sales;

public class SaleBulkAddResultDto
{
    public int AddedCount { get; set; }
    public int AlreadyPresentCount { get; set; }
    public int IneligibleCount { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<SaleItemDto>? Items { get; set; }
}
