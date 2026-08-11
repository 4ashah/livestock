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
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewOperationalData")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAppDbContext _db;
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        IAppDbContext db,
        IReportService reportService,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _db = db;
        _reportService = reportService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();

        var kpis = await _reportService.GetDashboardKpisAsync(companyId, ct);

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
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> MobileDashboard(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var kpis = await _reportService.GetDashboardKpisAsync(companyId, ct);

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
    [Authorize(Policy = "CanViewOperationalData")]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
