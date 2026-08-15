using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Dashboard;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.Services.Reports;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
public class HomeController : Controller
{
    private readonly IAppDbContext _db;
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;

    public HomeController(
        ILogger<HomeController> logger,
        IAppDbContext db,
        IReportService reportService,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        _ = logger;
        _db = db;
        _reportService = reportService;
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private async Task<bool> UserCanViewFinancialsAsync() =>
        (await _authorizationService.AuthorizeAsync(User, PermissionNames.Reports.ProfitAndLoss)).Succeeded;

    private static void SanitizeFinancialKpis(DashboardKpisDto kpis)
    {
        kpis.RevenueMonthToDate = 0m;
        kpis.PaidReceiptsMonthToDate = 0m;
        kpis.OutstandingInvoicesAmountTotal = 0m;
        kpis.OverdueInvoicesAmountTotal = 0m;
        kpis.PurchasesMonthToDateAmount = 0m;
        kpis.ExpensesMonthToDateAmount = 0m;
        kpis.NetOperatingResultMonthToDate = 0m;
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();

        var kpis = await _reportService.GetDashboardKpisAsync(companyId, ct);

        var canViewFinancials = await UserCanViewFinancialsAsync();
        if (!canViewFinancials)
            SanitizeFinancialKpis(kpis);
        ViewData["CanViewFinancials"] = canViewFinancials;

        var recentLivestock = await _db.Livestock
            .Where(l => l.CompanyId == companyId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new LivestockSummaryDto
            {
                Id = l.Id,
                CompanyId = l.CompanyId,
                FarmId = l.FarmId,
                LivestockId = l.LivestockId,
                LivestockTypeId = l.LivestockTypeId,
                AcquisitionDate = l.AcquisitionDate,
                InitialWeight = l.InitialWeight,
                WeightUnit = l.WeightUnit,
                PurchaseAmount = l.PurchaseAmount,
                CurrentWeight = l.CurrentWeight,
                CurrentWeightDate = l.CurrentWeightDate,
                Status = l.Status
            })
            .ToListAsync(ct);

        var recentInvoices = await _db.Invoices
            .Where(i => i.CompanyId == companyId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new InvoiceSummaryDto
            {
                Id = i.Id,
                CompanyId = i.CompanyId,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                GrandTotal = i.GrandTotal,
                PaidAmount = i.PaidAmount,
                OutstandingAmount = i.OutstandingAmount,
                Status = i.Status
            })
            .ToListAsync(ct);

        ViewData["RecentLivestock"] = recentLivestock;
        ViewData["RecentInvoices"] = recentInvoices;

        return View(kpis);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Reports.ActiveLivestock)]
    public async Task<IActionResult> MobileDashboard(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var kpis = await _reportService.GetDashboardKpisAsync(companyId, ct);

        var canViewFinancials = await UserCanViewFinancialsAsync();
        if (!canViewFinancials)
            SanitizeFinancialKpis(kpis);
        ViewData["CanViewFinancials"] = canViewFinancials;

        var recentLivestock = await _db.Livestock
            .Where(l => l.CompanyId == companyId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(4)
            .Select(l => new LivestockSummaryDto
            {
                Id = l.Id,
                CompanyId = l.CompanyId,
                FarmId = l.FarmId,
                LivestockId = l.LivestockId,
                LivestockTypeId = l.LivestockTypeId,
                AcquisitionDate = l.AcquisitionDate,
                InitialWeight = l.InitialWeight,
                WeightUnit = l.WeightUnit,
                PurchaseAmount = l.PurchaseAmount,
                CurrentWeight = l.CurrentWeight,
                CurrentWeightDate = l.CurrentWeightDate,
                Status = l.Status
            })
            .ToListAsync(ct);

        var recentInvoices = await _db.Invoices
            .Where(i => i.CompanyId == companyId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(4)
            .Select(i => new InvoiceSummaryDto
            {
                Id = i.Id,
                CompanyId = i.CompanyId,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                GrandTotal = i.GrandTotal,
                PaidAmount = i.PaidAmount,
                OutstandingAmount = i.OutstandingAmount,
                Status = i.Status
            })
            .ToListAsync(ct);

        ViewData["RecentLivestock"] = recentLivestock;
        ViewData["RecentInvoices"] = recentInvoices;
        return View(kpis);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
