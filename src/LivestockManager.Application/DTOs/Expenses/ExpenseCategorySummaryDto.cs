namespace LivestockManager.Application.DTOs.Expenses;

public class ExpenseCategorySummaryDto
{
    public ExpenseCategory Category { get; set; }
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalWithTax { get; set; }
}
