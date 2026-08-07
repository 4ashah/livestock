using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Sales;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Invoices;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.Sales;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewFinancialData")]
public class SalesController : Controller
{
    private readonly ISaleService _saleService;
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerService _customerService;
    private readonly IFarmService _farmService;
    private readonly ILivestockService _livestockService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SalesController(
        ISaleService saleService,
        IInvoiceService invoiceService,
        ICustomerService customerService,
        IFarmService farmService,
        ILivestockService livestockService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _saleService = saleService;
        _invoiceService = invoiceService;
        _customerService = customerService;
        _farmService = farmService;
        _livestockService = livestockService;
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private bool CanEdit => User.IsInRole(RoleNames.Accounts)
        || User.IsInRole(RoleNames.FarmManager)
        || User.IsInRole(RoleNames.CompanyAdministrator)
        || User.IsInRole(RoleNames.SystemAdministrator);

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? customerId, SaleStatus? status, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var sales = await _saleService.ListAsync(companyId, ct);

        if (from.HasValue)
        {
            var fromDto = new DateTimeOffset(from.Value.Date, TimeSpan.Zero);
            sales = sales.Where(s => s.Date >= fromDto).ToList();
        }
        if (to.HasValue)
        {
            var toDto = new DateTimeOffset(to.Value.Date.AddDays(1).AddTicks(-1), TimeSpan.Zero);
            sales = sales.Where(s => s.Date <= toDto).ToList();
        }
        if (customerId.HasValue)
            sales = sales.Where(s => s.CustomerId == customerId.Value).ToList();
        if (status.HasValue)
            sales = sales.Where(s => s.Status == status.Value).ToList();

        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["CustomerId"] = customerId;
        ViewData["Status"] = status;
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        return View(sales);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        SaleDetailDto sale;
        try
        {
            sale = await _saleService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        ViewData["CanEdit"] = CanEdit;

        var invoiceId = await _db.Invoices
            .Where(i => i.SaleId == id)
            .Select(i => (Guid?)i.Id)
            .FirstOrDefaultAsync(ct);
        ViewData["InvoiceId"] = invoiceId;
        return View(sale);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        ViewData["ActiveLivestock"] = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                l.FarmId,
                l.PurchaseAmount,
                Display = l.LivestockId + " (" + l.LivestockTypeId + ")",
                SuggestedPrice = l.PurchaseAmount * 1.3m
            })
            .ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> Create(SaleCreateDto dto, Guid[] livestockIds, decimal[] livestockPrices, string[] lineDesc, decimal[] lineQty, decimal[] linePrice, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        dto.Date = DateTimeOffset.UtcNow;
        dto.Items = new List<SaleItemDto>();

        if (livestockIds != null)
        {
            for (int i = 0; i < livestockIds.Length; i++)
            {
                var price = livestockPrices != null && i < livestockPrices.Length ? livestockPrices[i] : 0;
                dto.Items.Add(new SaleItemDto
                {
                    LivestockId = livestockIds[i],
                    Description = "Livestock #" + livestockIds[i].ToString().Substring(0, 8),
                    Quantity = 1,
                    UnitPrice = price,
                    DiscountPercent = 0,
                    TaxPercent = 0
                });
            }
        }

        if (lineDesc != null)
        {
            for (int i = 0; i < lineDesc.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lineDesc[i]))
                {
                    var qty = lineQty != null && i < lineQty.Length ? lineQty[i] : 1;
                    var prc = linePrice != null && i < linePrice.Length ? linePrice[i] : 0;
                    dto.Items.Add(new SaleItemDto
                    {
                        Description = lineDesc[i],
                        Quantity = qty,
                        UnitPrice = prc,
                        DiscountPercent = 0,
                        TaxPercent = 0
                    });
                }
            }
        }

        SaleDetailDto sale;
        try
        {
            sale = await _saleService.CreateDraftAsync(dto, companyId, ct);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
            ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
            ViewData["ActiveLivestock"] = await _db.Livestock
                .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
                .Select(l => new
                {
                    l.Id,
                    l.LivestockId,
                    l.LivestockTypeId,
                    l.FarmId,
                    l.PurchaseAmount,
                    Display = l.LivestockId + " (" + l.LivestockTypeId + ")",
                    SuggestedPrice = l.PurchaseAmount * 1.3m
                })
                .ToListAsync(ct);
            return View();
        }
        return RedirectToAction(nameof(Details), new { id = sale.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> Confirm(Guid id, SaleConfirmDto dto, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            var sale = await _saleService.ConfirmAsync(id, dto ?? new SaleConfirmDto(), companyId, ct);

            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(i => i.SaleId == id, ct);

            if (invoice != null)
            {
                var confirmDto = new Application.DTOs.Invoices.InvoiceConfirmDto { InvoiceId = invoice.Id };
                await _invoiceService.ConfirmAsync(confirmDto, companyId, ct);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> Cancel(Guid id, string reason, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            await _saleService.CancelAsync(id, reason ?? "User cancelled", companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
