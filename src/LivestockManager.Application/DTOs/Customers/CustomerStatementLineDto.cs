namespace LivestockManager.Application.DTOs.Customers;

public enum CustomerStatementLineType
{
    OpeningBalance = 1,
    Invoice = 2,
    Payment = 3,
    Receipt = 4,
    Adjustment = 5
}

public class CustomerStatementLineDto
{
    public DateTimeOffset Date { get; set; }
    public string? ReferenceNo { get; set; }
    public CustomerStatementLineType Type { get; set; }
    public string? Details { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
