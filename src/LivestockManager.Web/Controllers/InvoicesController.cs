using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Invoices;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewFinancialData")]
public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerService _customerService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoicesController(
        IInvoiceService invoiceService,
        ICustomerService customerService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _invoiceService = invoiceService;
        _customerService = customerService;
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private bool CanEdit => User.IsInRole(RoleNames.Accounts)
        || User.IsInRole(RoleNames.CompanyAdministrator)
        || User.IsInRole(RoleNames.SystemAdministrator);

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? customerId, InvoiceStatus? status, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var invoices = await _invoiceService.ListAsync(companyId, ct);

        if (from.HasValue)
        {
            var fromDto = new DateTimeOffset(from.Value.Date, TimeSpan.Zero);
            invoices = invoices.Where(i => i.InvoiceDate >= fromDto).ToList();
        }
        if (to.HasValue)
        {
            var toDto = new DateTimeOffset(to.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);
            invoices = invoices.Where(i => i.InvoiceDate <= toDto).ToList();
        }
        if (customerId.HasValue)
            invoices = invoices.Where(i => i.CustomerId == customerId.Value).ToList();
        if (status.HasValue)
            invoices = invoices.Where(i => i.Status == status.Value).ToList();

        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["CustomerId"] = customerId;
        ViewData["Status"] = status;
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        return View(invoices);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        InvoiceDetailDto invoice;
        try
        {
            invoice = await _invoiceService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var payments = await _db.Payments
            .Where(p => p.InvoiceId == id && !p.IsReversed)
            .Select(p => new
            {
                p.Id,
                p.PaymentDate,
                p.Method,
                p.Amount,
                p.Reference,
                p.Notes
            })
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

        ViewData["Payments"] = payments;
        ViewData["CanEdit"] = CanEdit;
        return View(invoice);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageAccounting")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        var dto = new InvoiceConfirmDto { InvoiceId = id };
        try
        {
            await _invoiceService.ConfirmAsync(dto, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        byte[] bytes;
        try
        {
            bytes = await _invoiceService.GetPdfAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return File(bytes, "application/pdf", $"Invoice_{id:N}.pdf");
    }
}
