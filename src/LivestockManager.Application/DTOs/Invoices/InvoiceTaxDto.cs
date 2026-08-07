namespace LivestockManager.Application.DTOs.Invoices;

public class InvoiceTaxDto
{
    public string Name { get; set; } = string.Empty;
    public decimal TaxPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}
