using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.StockAdditionViewModels;

public class StockAdditionPurchaseViewModel : IValidatableObject
{
    [Required]
    public Guid IdempotencyKey { get; set; }

    [Required(ErrorMessage = "Supplier is required.")]
    [Display(Name = "Supplier")]
    public Guid SupplierId { get; set; }

    [Required(ErrorMessage = "Purchase date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Purchase Date")]
    public DateTimeOffset PurchaseDate { get; set; } = new DateTimeOffset(DateTime.Today);

    [Required(ErrorMessage = "Farm is required.")]
    [Display(Name = "Farm")]
    public Guid FarmId { get; set; }

    [Required(ErrorMessage = "Livestock type is required.")]
    [Display(Name = "Livestock Type")]
    public LivestockType LivestockTypeId { get; set; } = LivestockType.Ah;

    [Required(ErrorMessage = "Purchase weight is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Purchase weight must be greater than 0.")]
    [Display(Name = "Purchase Weight")]
    public decimal PurchaseWeight { get; set; }

    [Required(ErrorMessage = "Weight unit is required.")]
    [Display(Name = "Weight Unit")]
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [Required(ErrorMessage = "Purchase cost is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Purchase cost cannot be negative.")]
    [Display(Name = "Livestock Purchase Cost")]
    public decimal PurchaseCost { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Commission cannot be negative.")]
    [Display(Name = "Commission")]
    public decimal CommissionAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Taxes cannot be negative.")]
    [Display(Name = "Taxes")]
    public decimal TaxAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Transportation cannot be negative.")]
    [Display(Name = "Transportation")]
    public decimal TransportationAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Other costs cannot be negative.")]
    [Display(Name = "Other Costs")]
    public decimal OtherCostAmount { get; set; }

    [MaxLength(500, ErrorMessage = "Other cost description cannot exceed 500 characters.")]
    [Display(Name = "Other Cost Description")]
    public string? OtherCostDescription { get; set; }

    [Display(Name = "Cost Allocation Method")]
    public CostAllocationMethod CostAllocationMethod { get; set; } = CostAllocationMethod.Equal;

    [MaxLength(200, ErrorMessage = "Supplier reference cannot exceed 200 characters.")]
    [Display(Name = "Supplier Reference")]
    public string? SupplierReference { get; set; }

    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters.")]
    [DataType(DataType.MultilineText)]
    public string? Comments { get; set; }

    [Display(Name = "Additional Acquisition Costs")]
    public decimal PreviewAdditionalAcquisitionCost
    {
        get
        {
            static decimal Nz(decimal v) => v < 0 ? 0 : v;
            return Nz(CommissionAmount) + Nz(TaxAmount) + Nz(TransportationAmount) + Nz(OtherCostAmount);
        }
    }

    [Display(Name = "Total Acquisition Cost")]
    public decimal PreviewTotalAcquisitionCost
    {
        get
        {
            var pc = PurchaseCost < 0 ? 0 : PurchaseCost;
            return pc + PreviewAdditionalAcquisitionCost;
        }
    }

    public SelectList? SupplierOptions { get; set; }

    public SelectList? FarmOptions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PurchaseDate > new DateTimeOffset(DateTime.Today).AddDays(1).AddTicks(-1))
        {
            yield return new ValidationResult(
                "Purchase date cannot be in the future.",
                new[] { nameof(PurchaseDate) });
        }

        var purchasedTypes = new[] { LivestockType.Ah, LivestockType.Su, LivestockType.Sa };
        if (!purchasedTypes.Contains(LivestockTypeId))
        {
            yield return new ValidationResult(
                "Purchased livestock must be Ah, Su, or Sa type.",
                new[] { nameof(LivestockTypeId) });
        }

        if (PurchaseCost <= 0)
        {
            yield return new ValidationResult(
                "Purchase cost must be greater than 0 for purchased livestock.",
                new[] { nameof(PurchaseCost) });
        }

        if (CommissionAmount < 0)
        {
            yield return new ValidationResult(
                "Commission cannot be negative.",
                new[] { nameof(CommissionAmount) });
        }

        if (TaxAmount < 0)
        {
            yield return new ValidationResult(
                "Taxes cannot be negative.",
                new[] { nameof(TaxAmount) });
        }

        if (TransportationAmount < 0)
        {
            yield return new ValidationResult(
                "Transportation cannot be negative.",
                new[] { nameof(TransportationAmount) });
        }

        if (OtherCostAmount < 0)
        {
            yield return new ValidationResult(
                "Other costs cannot be negative.",
                new[] { nameof(OtherCostAmount) });
        }

        if (OtherCostAmount > 0 && string.IsNullOrWhiteSpace(OtherCostDescription))
        {
            yield return new ValidationResult(
                "Other cost description is required when other costs are greater than zero.",
                new[] { nameof(OtherCostDescription) });
        }
    }
}

