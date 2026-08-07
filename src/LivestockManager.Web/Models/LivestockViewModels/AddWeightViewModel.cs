using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class AddWeightViewModel
{
    public Guid LivestockId { get; set; }

    [Display(Name = "Livestock ID")]
    public string LivestockDisplayId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Weight is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Weight must be greater than 0.")]
    public decimal Weight { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    public WeightUnit Unit { get; set; } = WeightUnit.Kg;

    [Required(ErrorMessage = "Weighed date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Weighed At")]
    public DateTimeOffset WeighedAt { get; set; } = new DateTimeOffset(DateTime.Today);

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}
