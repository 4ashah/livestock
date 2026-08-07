using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models;

namespace LivestockManager.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var today = DateTimeOffset.UtcNow.Date;
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var activeLivestockCount = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active, ct);

        var dischargedTodayCount = await _db.Livestock
            .CountAsync(l => l.CompanyId == companyId
                && l.DischargeDate.HasValue
                && l.DischargeDate.Value.Date == today, ct);

        var revenueStatuses = new[]
        {
            InvoiceStatus.Confirmed,
            InvoiceStatus.PartiallyPaid,
            InvoiceStatus.Paid,
            InvoiceStatus.Overdue
        };

        var revenueMonthToDate = await _db.Invoices
            .Where(i => i.CompanyId == companyId
                && i.InvoiceDate >= monthStart
                && revenueStatuses.Contains(i.Status))
            .SumAsync(i => (decimal?)i.GrandTotal ?? 0, ct);

        var soldLivestockMonthToDate = await _db.Livestock
            .Where(l => l.CompanyId == companyId
                && l.Status == LivestockStatus.DischargedSold
                && l.DischargeDate.HasValue
                && l.DischargeDate.Value >= monthStart)
            .ToListAsync(ct);

        var profitMonthToDate = soldLivestockMonthToDate
            .Sum(l => l.BasicProfitLoss ?? 0);

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

        ViewData["ActiveLivestockCount"] = activeLivestockCount;
        ViewData["DischargedTodayCount"] = dischargedTodayCount;
        ViewData["RevenueMonthToDate"] = revenueMonthToDate;
        ViewData["ProfitMonthToDate"] = profitMonthToDate;
        ViewData["RecentLivestock"] = recentLivestock;
        ViewData["RecentInvoices"] = recentInvoices;

        return View();
    }

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
