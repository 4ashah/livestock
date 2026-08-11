using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.Reports;

public class ProfitLossReportDto
{
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }

    public decimal TotalSales { get; set; }
    public int SalesCount { get; set; }
    public int SoldHead { get; set; }

    public decimal TotalPurchases { get; set; }
    public int PurchaseCount { get; set; }

    public decimal TotalLosses { get; set; }
    public int LossCount { get; set; }
    public int LostHead { get; set; }

    public decimal TotalExpenses { get; set; }
    public int ExpenseCount { get; set; }

    public decimal GrossProfit { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal NetProfit { get; set; }
}
