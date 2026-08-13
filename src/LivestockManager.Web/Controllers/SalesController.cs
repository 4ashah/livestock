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
using LivestockManager.Domain.Helpers;

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
            var fromDto = from.Value.AsUtcDayStart();
            sales = sales.Where(s => s.Date >= fromDto).ToList();
        }
        if (to.HasValue)
        {
            var toDto = to.Value.AsUtcDayEnd();
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
                Display = l.LivestockId + " (" + LivestockManager.Domain.Helpers.LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")",
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
                    Display = l.LivestockId + " (" + LivestockManager.Domain.Helpers.LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")",
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

    [HttpGet]
    [Authorize(Policy = "CanManageSales")]
    public async Task<JsonResult> SearchLivestock(string? keyword, Guid? farmId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var q = _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active);
        if (farmId.HasValue)
            q = q.Where(l => l.FarmId == farmId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(l => l.LivestockId.Contains(kw)
                || (l.Comments != null && l.Comments.Contains(kw)));
        }
        var results = await q
            .OrderBy(l => l.LivestockId)
            .Take(25)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockManager.Domain.Helpers.LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                InitialWeight = (decimal?)l.InitialWeight,
                PurchaseAmount = (decimal?)l.PurchaseAmount,
                SuggestedPrice = Math.Round(((decimal?)l.PurchaseAmount ?? 0) * 1.3m, 2)
            })
            .ToListAsync(ct);
        return Json(results);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSales")]
    public async Task<JsonResult> ResolveLivestock(string id, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        if (string.IsNullOrWhiteSpace(id)) return Json(new { ok = false, msg = "Id required" });
        var clean = id.Trim();
        var l = await _db.Livestock
            .Where(x => x.CompanyId == companyId && x.Status == LivestockStatus.Active
                && x.LivestockId == clean)
            .Select(x => new
            {
                x.Id,
                x.LivestockId,
                x.LivestockTypeId,
                TypeLabel = LivestockManager.Domain.Helpers.LivestockTypeDisplay.GetDisplayName(x.LivestockTypeId),
                x.FarmId,
                InitialWeight = (decimal?)x.InitialWeight,
                PurchaseAmount = (decimal?)x.PurchaseAmount,
                SuggestedPrice = Math.Round(((decimal?)x.PurchaseAmount ?? 0) * 1.3m, 2)
            })
            .FirstOrDefaultAsync(ct);
        if (l == null) return Json(new { ok = false, msg = "No active livestock matches '" + clean + "'" });
        return Json(new { ok = true, item = l });
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSales")]
    public async Task<JsonResult> SearchCustomers(string? keyword, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var q = _db.Customers.Where(c => c.CompanyId == companyId && c.IsActive);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(c => (c.Name != null && c.Name.Contains(kw))
                || (c.CustomerCode != null && c.CustomerCode.Contains(kw))
                || (c.Email != null && c.Email.Contains(kw)));
        }
        var results = await q
            .OrderBy(c => c.Name)
            .Take(25)
            .Select(c => new { c.Id, c.Name, c.CustomerCode, c.Email, c.Phone })
            .ToListAsync(ct);
        return Json(results);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> MobileIndex(DateTime? from, DateTime? to, Guid? customerId, SaleStatus? status, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var sales = await _saleService.ListAsync(companyId, ct);
        if (from.HasValue)
        {
            var fromDto = from.Value.AsUtcDayStart();
            sales = sales.Where(s => s.Date >= fromDto).ToList();
        }
        if (to.HasValue)
        {
            var toDto = to.Value.AsUtcDayEnd();
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
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> MobileCreate(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageSales")]
    public async Task<IActionResult> MobileCreate(SaleCreateDto dto, Guid[] livestockIds, decimal[] livestockPrices, string[] lineDesc, decimal[] lineQty, decimal[] linePrice, CancellationToken ct)
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
        try
        {
            var sale = await _saleService.CreateDraftAsync(dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id = sale.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
            ViewData["Customers"] = await _customerService.ListAsync(companyId, ct);
            ViewData["CanEdit"] = CanEdit;
            return View();
        }
    }
}
