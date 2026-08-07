using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LivestockManager.Web.Models.LivestockViewModels;

public class LivestockEditViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Display(Name = "Livestock ID")]
    public string LivestockId { get; set; } = string.Empty;

    [Display(Name = "Livestock Type")]
    public LivestockType LivestockTypeId { get; set; }

    [Display(Name = "Farm")]
    public Guid? FarmId { get; set; }

    [Required(ErrorMessage = "Acquisition date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Acquisition Date")]
    public DateTimeOffset AcquisitionDate { get; set; }

    [Required(ErrorMessage = "Initial weight is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Initial weight must be greater than 0.")]
    [Display(Name = "Initial Weight")]
    public decimal InitialWeight { get; set; }

    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters.")]
    [DataType(DataType.MultilineText)]
    public string? Comments { get; set; }

    public SelectList? FarmOptions { get; set; }
}
