namespace LivestockManager.Application.DTOs.StockAddition;

public class ParentSearchItemDto
{
    public Guid Id { get; set; }

    public string LivestockId { get; set; } = string.Empty;

    public LivestockType Type { get; set; }

    public string TypeDescription { get; set; } = string.Empty;

    public Guid? FarmId { get; set; }

    public string? FarmName { get; set; }

    public LivestockStatus Status { get; set; }
}
