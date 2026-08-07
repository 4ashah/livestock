namespace LivestockManager.Application.DTOs.Purchases;

public class PurchasePostDto
{
    public Guid Id { get; set; }
    public bool FinalizeSupplierSnapshot { get; set; } = true;
}
