using LivestockManager.Application.DTOs.Dashboard;
using LivestockManager.Application.DTOs.Reports;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Application.Services.Reports;

public interface IReportService
{
    Task<DashboardKpisDto> GetDashboardKpisAsync(Guid companyId, CancellationToken ct);
    Task<IList<LivestockProfitabilityReportRowDto>> CompleteLivestockProfitabilityAsync(Guid companyId, Guid? farmId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<IList<FarmProfitabilityReportRowDto>> CompleteFarmProfitabilityAsync(Guid companyId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
    Task<IList<OutstandingInvoiceRowDto>> ReceivableAgingAsync(Guid companyId, CancellationToken ct);
    Task<byte[]> ExportPurchasesCsvAsync(Guid companyId, Guid? supplierId, PurchaseStatus? status, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task<byte[]> ExportExpensesCsvAsync(Guid companyId, Guid? farmId, Guid? supplierId, Guid? livestockId, ExpenseCategory? category, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityAsync(Guid companyId, Guid? farmId, CancellationToken ct);
    Task<IList<FarmProfitabilityReportRowDto>> FarmProfitabilityAsync(Guid companyId, CancellationToken ct);
    Task<IList<OutstandingInvoiceRowDto>> OutstandingInvoicesAsync(Guid companyId, CancellationToken ct);
    Task<IList<(LivestockType Type, int Count)>> ActiveLivestockByTypeAsync(Guid companyId, Guid? farmId, CancellationToken ct);
    Task<IList<(DateTimeOffset WeekStart, decimal Weight)>> WeeklyWeightsAsync(Guid livestockId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IList<WeightChangeRowDto>> WeightChangesAsync(Guid companyId, Guid? farmId, CancellationToken ct);
    Task<IList<(Guid? FarmId, string? FarmName, int Count)>> MortalityAsync(Guid companyId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IList<(Guid? FarmId, string? FarmName, int Count, decimal TotalValue)>> DischargesAsync(Guid companyId, DateTimeOffset from, DateTimeOffset to, DischargeCondition? condition, CancellationToken ct);
    Task<IList<(Guid CustomerId, string CustomerName, decimal Balance)>> OutstandingBalancesAsync(Guid companyId, CancellationToken ct);
    Task<byte[]> ExportCsv<T>(IEnumerable<T> rows);
}
