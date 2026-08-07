using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.FarmViewModels;

public class FarmCreateEditViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Code must be alphanumeric only.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Street1 { get; set; }

    [StringLength(256)]
    public string? Street2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(50)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    public Currency Currency { get; set; } = Currency.USD;

    [Required(ErrorMessage = "Weight unit is required.")]
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    public bool IsActive { get; set; } = true;
}
