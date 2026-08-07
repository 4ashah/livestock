namespace LivestockManager.Application.DTOs.Expenses;

public class ExpenseDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? LivestockId { get; set; }
    public ExpenseCategory Category { get; set; }
    public DateTimeOffset ExpenseDate { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public Guid? DocumentId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
