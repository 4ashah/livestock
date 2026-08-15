using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Payments;
using LivestockManager.Application.DTOs.Invoices;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Invoices;
using LivestockManager.Application.Services.Payments;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PolicyNames.CanRecordPayments)]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerService _customerService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentsController(
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        ICustomerService customerService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _paymentService = paymentService;
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

    private async Task<Guid> GetUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id ?? Guid.Empty;
    }

    private bool CanEdit => User.IsInRole(RoleNames.Accounts)
        || User.IsInRole(RoleNames.CompanyAdministrator)
        || User.IsInRole(RoleNames.SystemAdministrator);

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordPayments)]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? customerId, PaymentMethod? method, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var payments = await _paymentService.ListAsync(companyId, ct);

        if (from.HasValue)
        {
            var fromDto = from.Value.AsUtcDayStart();
            payments = payments.Where(p => p.PaymentDate >= fromDto).ToList();
        }
        if (to.HasValue)
        {
            var toDto = to.Value.AsUtcDayEnd();
            payments = payments.Where(p => p.PaymentDate <= toDto).ToList();
        }
        if (customerId.HasValue)
            payments = payments.Where(p => p.CustomerId == customerId.Value).ToList();
        if (method.HasValue)
            payments = payments.Where(p => p.Method == method.Value).ToList();

        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["CustomerId"] = customerId;
        ViewData["Method"] = method;
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        return View(payments);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordPayments)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        PaymentDetailDto payment;
        try
        {
            payment = await _paymentService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return View(payment);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordPayments)]
    public async Task<IActionResult> Create(Guid? invoiceId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        ViewData["Invoices"] = await _invoiceService.ListAsync(companyId, ct);

        var model = new PaymentCreateDto
        {
            CompanyId = companyId,
            PaymentDate = DateTimeOffset.UtcNow,
            Method = PaymentMethod.Cash
        };

        if (invoiceId.HasValue)
        {
            InvoiceDetailDto invoice;
            try
            {
                invoice = await _invoiceService.GetByIdAsync(invoiceId.Value, companyId, ct);
            }
            catch (DomainException)
            {
                return NotFound();
            }
            model.CustomerId = invoice.CustomerId;
            model.Amount = invoice.OutstandingAmount;
            model.Allocations = new List<PaymentAllocationDto>
            {
                new() { InvoiceId = invoiceId.Value, Amount = invoice.OutstandingAmount }
            };
            ViewData["SelectedInvoiceId"] = invoiceId.Value;
            ViewData["SelectedInvoiceNumber"] = invoice.InvoiceNumber;
            ViewData["OutstandingAmount"] = invoice.OutstandingAmount;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanRecordPayments)]
    public async Task<IActionResult> Create(PaymentCreateDto dto, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;

        if (dto.Allocations == null || dto.Allocations.Count == 0)
        {
            var invId = Request.Form["SelectedInvoiceId"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(invId) && Guid.TryParse(invId, out var parsedId))
            {
                dto.Allocations = new List<PaymentAllocationDto>
                {
                    new() { InvoiceId = parsedId, Amount = dto.Amount }
                };
            }
            else
            {
                ModelState.AddModelError("", "At least one invoice allocation is required.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
            ViewData["Invoices"] = await _invoiceService.ListAsync(companyId, ct);
            return View(dto);
        }

        PaymentDetailDto payment;
        try
        {
            payment = await _paymentService.PostAsync(dto, companyId, ct);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
            ViewData["Invoices"] = await _invoiceService.ListAsync(companyId, ct);
            return View(dto);
        }
        return RedirectToAction(nameof(Details), new { id = payment.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanReversePayments)]
    public async Task<IActionResult> Reverse(Guid id, string? reason, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();

        var companyId = await GetCompanyIdAsync();
        var actingUserId = await GetUserIdAsync();

        if (actingUserId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Could not identify current user.");
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _paymentService.ReverseAsync(id, reason ?? "User reversed payment", actingUserId, companyId, ct);
            TempData["Success"] = "Payment reversed successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public IActionResult MobileIndex()
    {
        ViewData["DockKey"] = "payments";
        return RedirectToAction(nameof(Index));
    }
}
