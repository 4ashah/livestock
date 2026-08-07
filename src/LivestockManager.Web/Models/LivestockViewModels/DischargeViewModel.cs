using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class DischargeViewModel : IValidatableObject
{
    public Guid LivestockId { get; set; }

    [Display(Name = "Livestock ID")]
    public string LivestockDisplayId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Discharge condition is required.")]
    [Display(Name = "Discharge Condition")]
    public DischargeCondition DischargeCondition { get; set; }

    [Required(ErrorMessage = "Discharge date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Discharge Date")]
    public DateTimeOffset DischargeDate { get; set; } = new DateTimeOffset(DateTime.Today);

    [MaxLength(2000, ErrorMessage = "Details cannot exceed 2000 characters.")]
    public string? Details { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Sold amount must be greater than 0.")]
    [Display(Name = "Sold Amount")]
    public decimal? SoldAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DischargeDate > new DateTimeOffset(DateTime.Today).AddDays(1).AddTicks(-1))
        {
            yield return new ValidationResult(
                "Discharge date cannot be in the future.",
                new[] { nameof(DischargeDate) });
        }

        if (DischargeCondition == DischargeCondition.Sold)
        {
            if (!SoldAmount.HasValue || SoldAmount.Value <= 0)
            {
                yield return new ValidationResult(
                    "Sold amount is required and must be greater than 0 when condition is Sold.",
                    new[] { nameof(SoldAmount) });
            }
        }
    }
}
