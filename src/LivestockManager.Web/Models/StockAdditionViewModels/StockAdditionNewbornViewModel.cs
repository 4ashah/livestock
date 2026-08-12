using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.StockAdditionViewModels;

public class StockAdditionNewbornViewModel : IValidatableObject
{
    [Required]
    public Guid IdempotencyKey { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTimeOffset DateOfBirth { get; set; } = new DateTimeOffset(DateTime.Today);

    [Required(ErrorMessage = "Farm is required.")]
    [Display(Name = "Farm")]
    public Guid FarmId { get; set; }

    [Required(ErrorMessage = "Livestock type is required.")]
    [Display(Name = "Livestock Type")]
    public LivestockType LivestockTypeId { get; set; } = LivestockType.Ad;

    [Required(ErrorMessage = "Birth weight is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Birth weight must be greater than 0.")]
    [Display(Name = "Birth Weight")]
    public decimal BirthWeight { get; set; }

    [Required(ErrorMessage = "Weight unit is required.")]
    [Display(Name = "Weight Unit")]
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    [Required(ErrorMessage = "Mother livestock is required.")]
    [Display(Name = "Mother (Ewe)")]
    public Guid MotherLivestockId { get; set; }

    [Required(ErrorMessage = "Father livestock is required.")]
    [Display(Name = "Father (Ram)")]
    public Guid FatherLivestockId { get; set; }

    [MaxLength(2000, ErrorMessage = "Birth comments cannot exceed 2000 characters.")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Birth Comments")]
    public string? BirthComments { get; set; }

    public SelectList? FarmOptions { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var maxFuture = DateTimeOffset.Now.AddDays(1);
        var minPast = DateTimeOffset.Now.AddYears(-20);

        if (DateOfBirth > maxFuture)
        {
            yield return new ValidationResult(
                "Date of birth cannot be more than 1 day in the future.",
                new[] { nameof(DateOfBirth) });
        }

        if (DateOfBirth < minPast)
        {
            yield return new ValidationResult(
                "Date of birth cannot be more than 20 years in the past.",
                new[] { nameof(DateOfBirth) });
        }

        var allowedNewbornTypes = new[] { LivestockType.Ad, LivestockType.Sd };
        if (!allowedNewbornTypes.Contains(LivestockTypeId))
        {
            yield return new ValidationResult(
                "Newborn livestock must be Ad (bred castrated ram) or Sd (bred ewe).",
                new[] { nameof(LivestockTypeId) });
        }

        if (MotherLivestockId == FatherLivestockId && MotherLivestockId != Guid.Empty)
        {
            yield return new ValidationResult(
                "Mother and father livestock must be different.",
                new[] { nameof(MotherLivestockId), nameof(FatherLivestockId) });
        }
    }
}
