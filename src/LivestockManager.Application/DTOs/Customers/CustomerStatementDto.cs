namespace LivestockManager.Application.DTOs.Customers;

public class CustomerStatementDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? TaxNumber { get; set; }
    public Currency Currency { get; set; }
    public DateTimeOffset StatementFrom { get; set; }
    public DateTimeOffset StatementTo { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public IList<CustomerStatementLineDto> Lines { get; set; } = new List<CustomerStatementLineDto>();
}
