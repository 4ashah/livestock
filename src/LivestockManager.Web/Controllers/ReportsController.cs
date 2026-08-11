using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.Common;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Reports;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewOperationalData")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IFarmService _farmService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        IReportService reportService,
        IFarmService farmService,
        UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _farmService = farmService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> ActiveLivestock(Guid? farmId, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var byType = await _reportService.ActiveLivestockByTypeAsync(companyId, farmId, ct);
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        var rows = byType.Select(t => new
        {
            Type = t.Type.ToString(),
            Count = t.Count
        }).ToList();

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var bytes = CsvExporter.Write(rows, new[] { "Livestock Type", "Active Count" });
            return File(bytes, "text/csv", "ActiveLivestockByType.csv");
        }

        return View(rows);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> SalesByPeriod(DateTime? from, DateTime? to, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDate = from.HasValue
            ? from.Value.AsUtcDayStart()
            : DateTime.Today.AddDays(-30).AsUtcDayStart();
        var toDate = to.HasValue
            ? to.Value.AsUtcDayEnd()
            : DateTimeOffset.UtcNow;

        var discharges = await _reportService.DischargesAsync(companyId, fromDate, toDate,
            Domain.Enums.DischargeCondition.Sold, ct);

        var rows = discharges.Select(d => new
        {
            FarmId = d.FarmId?.ToString() ?? string.Empty,
            FarmName = d.FarmName ?? "(All)",
            SoldCount = d.Count,
            TotalValue = d.TotalValue.ToString("F2")
        }).ToList();

        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var bytes = CsvExporter.Write(rows, new[] { "FarmId", "FarmName", "SoldCount", "TotalValue" });
            return File(bytes, "text/csv", "SalesByPeriod.csv");
        }

        return View(rows);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> LivestockProfitability(Guid? farmId, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var data = await _reportService.LivestockProfitabilityAsync(companyId, farmId, ct);
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        var rows = data.Select(d => new
        {
            d.LivestockDisplayId,
            Type = d.Type.ToString(),
            AcquisitionDate = d.AcquisitionDate.ToString("yyyy-MM-dd"),
            PurchaseAmount = d.PurchaseAmount.ToString("F2"),
            SoldAmount = d.SoldAmount?.ToString("F2") ?? "0.00",
            BasicProfitLoss = d.BasicProfitLoss?.ToString("F2") ?? "0.00",
            Status = d.Status.ToString()
        }).ToList();

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var bytes = CsvExporter.Write(rows, new[]
            {
                "Livestock ID", "Type", "Acquisition Date", "Purchase Amount",
                "Sold Amount", "Basic Profit/Loss", "Status"
            });
            return File(bytes, "text/csv", "LivestockProfitability.csv");
        }

        return View(rows);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var fromDate = from.HasValue
            ? from.Value.AsUtcDayStart()
            : monthStart.AsUtcDayStart();
        var toDate = to.HasValue
            ? to.Value.AsUtcDayEnd()
            : DateTimeOffset.UtcNow;

        var report = await _reportService.ProfitLossAsync(companyId, fromDate, toDate, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var rows = new[]
            {
                new { Metric = "Total Sales", Value = report.TotalSales.ToString("F2"), Count = report.SalesCount.ToString() },
                new { Metric = "Total Purchases", Value = report.TotalPurchases.ToString("F2"), Count = report.PurchaseCount.ToString() },
                new { Metric = "Total Livestock Losses", Value = report.TotalLosses.ToString("F2"), Count = report.LossCount.ToString() },
                new { Metric = "Total Expenses", Value = report.TotalExpenses.ToString("F2"), Count = report.ExpenseCount.ToString() },
                new { Metric = "Gross Profit (Sales - Purchases)", Value = report.GrossProfit.ToString("F2"), Count = "" },
                new { Metric = "Operating Profit (Gross - Expenses)", Value = report.OperatingProfit.ToString("F2"), Count = "" },
                new { Metric = "Net Profit (After Losses)", Value = report.NetProfit.ToString("F2"), Count = "" },
                new { Metric = "Sold Head", Value = report.SoldHead.ToString(), Count = "" },
                new { Metric = "Lost Head", Value = report.LostHead.ToString(), Count = "" }
            };
            var bytes = CsvExporter.Write(rows, new[] { "Metric", "Value", "Count" });
            return File(bytes, "text/csv", $"ProfitLoss_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
        }

        return View(report);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> MobileProfitLoss(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var fromDate = from.HasValue
            ? from.Value.AsUtcDayStart()
            : monthStart.AsUtcDayStart();
        var toDate = to.HasValue
            ? to.Value.AsUtcDayEnd()
            : DateTimeOffset.UtcNow;

        var report = await _reportService.ProfitLossAsync(companyId, fromDate, toDate, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");
        return View("MobileProfitLoss", report);
    }
}
