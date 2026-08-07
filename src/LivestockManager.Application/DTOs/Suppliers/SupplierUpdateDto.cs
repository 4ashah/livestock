using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.Suppliers;

public class SupplierUpdateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BankAccount { get; set; }
    public int? PaymentTermsDays { get; set; }
    public Currency? Currency { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
