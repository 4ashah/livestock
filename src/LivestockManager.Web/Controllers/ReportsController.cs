using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Farms;
using LivestockManager.Application.DTOs.Reports;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Reports;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Helpers;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IFarmService _farmService;
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        IReportService reportService,
        IFarmService farmService,
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _farmService = farmService;
        _customerService = customerService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public async Task<IActionResult> MobileIndex(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public async Task<IActionResult> ActiveLivestock(Guid? farmId, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var rows = await _reportService.ActiveLivestockByTypeReportAsync(companyId, farmId, ct);
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csvRows = rows.Select(r => new
            {
                LivestockType = r.TypeLabel,
                ActiveCount = r.Count,
                PercentageOfTotal = r.PercentageOfTotal
            }).ToList();
            var bytes = CsvExporter.Write(csvRows, new[] { "Livestock Type", "Active Count", "% of Total" });
            return File(bytes, "text/csv", "ActiveLivestockByType.csv");
        }

        return View(rows);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.SalesByPeriod)]
    public async Task<IActionResult> SalesByPeriod(DateTime? from, DateTime? to, Guid? farmId, Guid? customerId, string? status, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDate = from.HasValue
            ? from.Value.AsUtcDayStart()
            : DateTime.Today.AddDays(-30).AsUtcDayStart();
        var toDate = to.HasValue
            ? to.Value.AsUtcDayEnd()
            : DateTimeOffset.UtcNow;

        var report = await _reportService.SalesByPeriodReportAsync(companyId, fromDate, toDate, farmId, customerId, status, ct);
        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["CustomerId"] = customerId;
        ViewData["Status"] = status;
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csvRows = report.SaleDetails.Select(s => new
            {
                SaleDate = s.SaleDate.ToString("yyyy-MM-dd"),
                s.SaleNumber,
                FarmName = s.FarmName,
                s.CustomerName,
                NumberSold = s.NumberSold,
                GrossSaleAmount = s.GrossSaleAmount.ToString("F2"),
                Status = s.Status.ToString()
            }).ToList();
            var bytes = CsvExporter.Write(csvRows, new[] { "Sale Date", "Sale Number", "Farm", "Customer", "Number Sold", "Gross Sale Amount", "Status" });
            return File(bytes, "text/csv", "SalesByPeriod.csv");
        }

        return View(report);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.LivestockProfitability)]
    public async Task<IActionResult> LivestockProfitability(DateTime? from, DateTime? to, Guid? farmId, string? format, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDate = from.HasValue ? from.Value.AsUtcDayStart() : (DateTimeOffset?)null;
        var toDate = to.HasValue ? to.Value.AsUtcDayEnd() : (DateTimeOffset?)null;
        var rows = await _reportService.LivestockProfitabilityWithDatesAsync(companyId, farmId, fromDate, toDate, ct);
        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        if (format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true)
        {
            var csvRows = rows.Select(r => new
            {
                LivestockID = r.LivestockDisplayId,
                Type = r.Type.GetDisplayName(),
                SaleFarm = r.SaleFarmName ?? "",
                PurchaseBirthDate = r.AcquisitionDate.ToString("yyyy-MM-dd"),
                SaleDate = r.SaleDate.HasValue ? r.SaleDate.Value.ToString("yyyy-MM-dd") : "",
                GrossSaleAmount = r.SoldAmount?.ToString("F2") ?? "0.00",
                PurchasePrice = r.PurchaseAmount.ToString("F2"),
                AdditionalAcquisitionCosts = r.AdditionalAcquisitionCosts.ToString("F2"),
                TotalAcquisitionCost = r.TotalAcquisitionCosts.ToString("F2"),
                OperatingExpenses = r.DirectExpenses.ToString("F2"),
                AdditionalSaleCosts = r.AdditionalSaleCosts.ToString("F2"),
                NetSaleProceeds = r.NetSaleProceeds.ToString("F2"),
                BasicProfit = r.BasicProfitLoss?.ToString("F2") ?? "0.00",
                CompleteProfit = r.CompleteProfitLoss?.ToString("F2") ?? "0.00"
            }).ToList();
            var bytes = CsvExporter.Write(csvRows, new[]
            {
                "Livestock ID", "Type", "Sale Farm", "Purchase/Birth Date", "Sale Date",
                "Gross Sale Amount", "Purchase Price", "Additional Acquisition Costs",
                "Total Acquisition Cost", "Operating Expenses", "Additional Sale Costs",
                "Net Sale Proceeds", "Basic Profit", "Complete Profit"
            });
            return File(bytes, "text/csv", "LivestockProfitability.csv");
        }

        return View(rows);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ProfitAndLoss)]
    public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to, Guid? farmId, string? format, CancellationToken ct)
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

        var report = await _reportService.ProfitLossAsync(companyId, farmId, fromDate, toDate, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

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
    [Authorize(Policy = PermissionNames.Reports.ProfitAndLoss)]
    public async Task<IActionResult> MobileProfitLoss(DateTime? from, DateTime? to, Guid? farmId, CancellationToken ct)
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

        var report = await _reportService.ProfitLossAsync(companyId, farmId, fromDate, toDate, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        return View("MobileProfitLoss", report);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public async Task<IActionResult> MobileActiveLivestock(Guid? farmId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var rows = await _reportService.ActiveLivestockByTypeReportAsync(companyId, farmId, ct);
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        return View("MobileActiveLivestock", rows);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.SalesByPeriod)]
    public async Task<IActionResult> MobileSalesByPeriod(DateTime? from, DateTime? to, Guid? farmId, Guid? customerId, string? status, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDate = from.HasValue
            ? from.Value.AsUtcDayStart()
            : DateTime.Today.AddDays(-30).AsUtcDayStart();
        var toDate = to.HasValue
            ? to.Value.AsUtcDayEnd()
            : DateTimeOffset.UtcNow;

        var report = await _reportService.SalesByPeriodReportAsync(companyId, fromDate, toDate, farmId, customerId, status, ct);
        ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? fromDate.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? toDate.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["CustomerId"] = customerId;
        ViewData["Status"] = status;
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        return View("MobileSalesByPeriod", report);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.LivestockProfitability)]
    public async Task<IActionResult> MobileLivestockProfitability(DateTime? from, DateTime? to, Guid? farmId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDate = from.HasValue ? from.Value.AsUtcDayStart() : (DateTimeOffset?)null;
        var toDate = to.HasValue ? to.Value.AsUtcDayEnd() : (DateTimeOffset?)null;
        var rows = await _reportService.LivestockProfitabilityWithDatesAsync(companyId, farmId, fromDate, toDate, ct);
        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        return View("MobileLivestockProfitability", rows);
    }
}
