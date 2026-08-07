using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Invoices;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize]
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

    private bool CanEdit => User.IsInRole("Administrator") || User.IsInRole("Manager");

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

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var invoice = await _invoiceService.GetByIdAsync(id, ct);

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

    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var dto = new InvoiceConfirmDto { InvoiceId = id };
        await _invoiceService.ConfirmAsync(dto, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var bytes = await _invoiceService.GetPdfAsync(id, ct);
        return File(bytes, "application/pdf", $"Invoice_{id:N}.pdf");
    }
}
