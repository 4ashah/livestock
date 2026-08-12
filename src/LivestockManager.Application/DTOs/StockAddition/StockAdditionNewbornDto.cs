using System.ComponentModel.DataAnnotations;

namespace LivestockManager.Application.DTOs.StockAddition;

public class StockAdditionNewbornDto
{
    [Required]
    public Guid IdempotencyKey { get; set; }

    [Required]
    public DateTimeOffset DateOfBirth { get; set; }

    [Required]
    public Guid FarmId { get; set; }

    [Required]
    public LivestockType LivestockTypeId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Birth weight must be greater than zero.")]
    public decimal BirthWeight { get; set; }

    [Required]
    public WeightUnit WeightUnit { get; set; }

    [Required]
    public Guid MotherLivestockId { get; set; }

    [Required]
    public Guid FatherLivestockId { get; set; }

    [MaxLength(2000)]
    public string? BirthComments { get; set; }
}
