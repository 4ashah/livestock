using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.StockAddition;

public class StockAdditionPurchasedDto
{
    [Required]
    public Guid IdempotencyKey { get; set; }

    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    public DateTimeOffset PurchaseDate { get; set; }

    [Required]
    public Guid FarmId { get; set; }

    [Required]
    public LivestockType LivestockTypeId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Purchase weight must be greater than zero.")]
    public decimal PurchaseWeight { get; set; }

    [Required]
    public WeightUnit WeightUnit { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Purchase cost cannot be negative.")]
    public decimal PurchaseCost { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Commission cannot be negative.")]
    public decimal CommissionAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Taxes cannot be negative.")]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Transportation cannot be negative.")]
    public decimal TransportationAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Other costs cannot be negative.")]
    public decimal OtherCostAmount { get; set; }

    [MaxLength(500, ErrorMessage = "Other cost description cannot exceed 500 characters.")]
    public string? OtherCostDescription { get; set; }

    public CostAllocationMethod CostAllocationMethod { get; set; } = CostAllocationMethod.Equal;

    [MaxLength(200)]
    public string? SupplierReference { get; set; }

    [MaxLength(2000)]
    public string? Comments { get; set; }

    public Guid? DocumentId { get; set; }
}

