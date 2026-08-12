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
    [Display(Name = "Purchase Cost")]
    public decimal PurchaseCost { get; set; }

    [MaxLength(200, ErrorMessage = "Supplier reference cannot exceed 200 characters.")]
    [Display(Name = "Supplier Reference")]
    public string? SupplierReference { get; set; }

    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters.")]
    [DataType(DataType.MultilineText)]
    public string? Comments { get; set; }

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
    }
}
