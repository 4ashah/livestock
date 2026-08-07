using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockRegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Farm is required.")]
    [Display(Name = "Farm")]
    public Guid FarmId { get; set; }

    [Required(ErrorMessage = "Livestock type is required.")]
    [Display(Name = "Livestock Type")]
    public LivestockType LivestockTypeId { get; set; } = LivestockType.Ah;

    [Required(ErrorMessage = "Acquisition date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Acquisition Date")]
    public DateTimeOffset AcquisitionDate { get; set; } = new DateTimeOffset(DateTime.Today);

    [Required(ErrorMessage = "Initial weight is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Initial weight must be greater than 0.")]
    [Display(Name = "Initial Weight")]
    public decimal InitialWeight { get; set; }

    [Required(ErrorMessage = "Weight unit is required.")]
    [Display(Name = "Weight Unit")]
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [Required(ErrorMessage = "Purchase amount is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Purchase amount cannot be negative.")]
    [Display(Name = "Purchase Amount")]
    public decimal PurchaseAmount { get; set; }

    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters.")]
    [DataType(DataType.MultilineText)]
    public string? Comments { get; set; }

    public SelectList? FarmOptions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcquisitionDate > new DateTimeOffset(DateTime.Today).AddDays(1).AddTicks(-1))
        {
            yield return new ValidationResult(
                "Acquisition date cannot be in the future.",
                new[] { nameof(AcquisitionDate) });
        }

        if (LivestockTypeId == LivestockType.Ad || LivestockTypeId == LivestockType.Sd)
        {
            if (PurchaseAmount != 0)
            {
                yield return new ValidationResult(
                    "Purchase amount must be 0 for bred livestock (Ad/Sd).",
                    new[] { nameof(PurchaseAmount) });
            }
        }

        if (LivestockTypeId == LivestockType.Ah || LivestockTypeId == LivestockType.Su || LivestockTypeId == LivestockType.Sa)
        {
            if (PurchaseAmount <= 0)
            {
                yield return new ValidationResult(
                    "Purchase amount must be greater than 0 for purchased livestock (Ah/Su/Sa).",
                    new[] { nameof(PurchaseAmount) });
            }
        }
    }
}
