namespace LivestockManager.Application.DTOs.Suppliers;

public class SupplierSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxNumber { get; set; }
    public int? PaymentTermsDays { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
