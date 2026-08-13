namespace LivestockManager.Application.DTOs.StockAddition;

public class StockAdditionResultDto
{
    public bool Succeeded { get; set; }

    public string? ErrorMessage { get; set; }

    public Guid? LivestockEntityId { get; set; }

    public string? LivestockId { get; set; }

    public StockSource StockSource { get; set; }

    public string? FarmName { get; set; }

    public string? SupplierName { get; set; }

    public DateTimeOffset? PurchaseDate { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public decimal StartingWeight { get; set; }

    public WeightUnit WeightUnit { get; set; }

    public decimal? PurchaseCost { get; set; }

    public decimal CommissionAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TransportationAmount { get; set; }

    public decimal OtherCostAmount { get; set; }

    public string? OtherCostDescription { get; set; }

    public decimal AdditionalAcquisitionCost { get; set; }

    public decimal TotalAcquisitionCost { get; set; }

    public string? PurchaseNumber { get; set; }

    public string? MotherLivestockId { get; set; }

    public string? FatherLivestockId { get; set; }

    public bool IsDuplicate { get; set; }
}

