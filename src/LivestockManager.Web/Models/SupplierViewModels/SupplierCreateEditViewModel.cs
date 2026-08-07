using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.SupplierViewModels;

public class SupplierCreateEditViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? LegalName { get; set; }

    [StringLength(50)]
    public string? TaxNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(200)]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? BankAccount { get; set; }

    public int? PaymentTermsDays { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    public Currency Currency { get; set; } = Currency.USD;

    public bool IsActive { get; set; } = true;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
