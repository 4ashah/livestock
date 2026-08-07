using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.DTOs.Dashboard;

public class DashboardKpisDto
{
    public DateTimeOffset TodayUtc { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Currency CompanyCurrency { get; set; }

    public int ActiveLivestockCount { get; set; }
    public int NewRegistrationsThisMonth { get; set; }
    public int DischargesToday { get; set; }
    public decimal? AverageDailyWeightGainKgCurrentMonth { get; set; }
    public decimal? LivestockMortalityRate30dPct { get; set; }
    public int PendingWeighingsCount { get; set; }
    public int FarmsActive { get; set; }

    public decimal RevenueMonthToDate { get; set; }
    public decimal PaidReceiptsMonthToDate { get; set; }
    public decimal OutstandingInvoicesAmountTotal { get; set; }
    public decimal OverdueInvoicesAmountTotal { get; set; }
    public decimal PurchasesMonthToDateAmount { get; set; }
    public decimal ExpensesMonthToDateAmount { get; set; }
    public decimal NetOperatingResultMonthToDate { get; set; }
}
