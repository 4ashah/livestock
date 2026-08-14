using System.ComponentModel.DataAnnotations;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.Sales;

public class SaleCreateDto
{
    public Guid CompanyId { get; set; }
    public Guid? FarmId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Notes { get; set; }
    public IList<SaleItemDto> Items { get; set; } = new List<SaleItemDto>();

    public decimal CommissionAmount { get; set; }
    public decimal SellerTaxAmount { get; set; }
    public decimal TransportationAmount { get; set; }
    public decimal OtherCostAmount { get; set; }

    [MaxLength(500)]
    public string? OtherCostDescription { get; set; }

    public CostAllocationMethod CostAllocationMethod { get; set; } = CostAllocationMethod.Equal;
}
