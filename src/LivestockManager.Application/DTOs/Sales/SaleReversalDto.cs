using System.ComponentModel.DataAnnotations;

namespace LivestockManager.Application.DTOs.Sales;

public class SaleReversalDto
{
    public Guid Id { get; set; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(500)]
    public string ReversalReason { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ReversalNotes { get; set; }

    public DateTimeOffset ReversalDate { get; set; } = DateTimeOffset.UtcNow;
}
