namespace LivestockManager.Application.DTOs.Expenses;

public class ExpenseSummaryDto
{
    public Guid Id { get; set; }
    public DateTimeOffset ExpenseDate { get; set; }
    public ExpenseCategory Category { get; set; }
    public Guid? FarmId { get; set; }
    public string? FarmName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? LivestockId { get; set; }
    public string? LivestockCode { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Currency Currency { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
