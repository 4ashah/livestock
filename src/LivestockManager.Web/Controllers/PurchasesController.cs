using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Purchases;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Purchases;
using LivestockManager.Application.Services.Suppliers;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Helpers;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewFinancialData")]
public class PurchasesController : Controller
{
    private readonly IPurchaseService _purchaseService;
    private readonly ISupplierService _supplierService;
    private readonly IFarmService _farmService;
    private readonly ILivestockService _livestockService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PurchasesController(
        IPurchaseService purchaseService,
        ISupplierService supplierService,
        IFarmService farmService,
        ILivestockService livestockService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _purchaseService = purchaseService;
        _supplierService = supplierService;
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
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, Guid? supplierId, PurchaseStatus? status, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        DateTimeOffset? fromDto = from.AsUtcDayStartOrDefault();
        DateTimeOffset? toDto = to.AsUtcDayEndOrDefault();
        var purchases = await _purchaseService.ListAsync(companyId, supplierId, status, fromDto, toDto, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["SupplierId"] = supplierId;
        ViewData["Status"] = status;
        ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        return View(purchases);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        PurchaseDetailDto purchase;
        try
        {
            purchase = await _purchaseService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        ViewData["CanEdit"] = CanEdit;
        return View(purchase);
    }

    [HttpGet]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["ActiveLivestock"] = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.PurchaseAmount,
                Display = l.LivestockId + " (" + LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")"
            })
            .ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> Create(PurchaseCreateDto dto, string[] lineDesc, int[] lineQty, decimal[] lineUnitCost, Guid[] lineLivestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        dto.PurchaseDate = DateTimeOffset.UtcNow;
        dto.Items = new List<PurchaseItemCreateDto>();

        if (lineDesc != null)
        {
            for (int i = 0; i < lineDesc.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lineDesc[i]))
                {
                    var qty = lineQty != null && i < lineQty.Length && lineQty[i] > 0 ? lineQty[i] : 1;
                    var cost = lineUnitCost != null && i < lineUnitCost.Length ? lineUnitCost[i] : 0;
                    var item = new PurchaseItemCreateDto
                    {
                        LineNo = i + 1,
                        ItemType = PurchaseItemType.General,
                        Description = lineDesc[i],
                        Quantity = qty,
                        UnitCost = cost
                    };
                    if (lineLivestockId != null && i < lineLivestockId.Length && lineLivestockId[i] != Guid.Empty)
                    {
                        item.LivestockId = lineLivestockId[i];
                        item.ItemType = PurchaseItemType.Livestock;
                    }
                    dto.Items.Add(item);
                }
            }
        }

        PurchaseDetailDto purchase;
        try
        {
            purchase = await _purchaseService.CreateDraftAsync(dto, companyId, ct);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
            ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
            ViewData["ActiveLivestock"] = await _db.Livestock
                .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
                .Select(l => new
                {
                    l.Id,
                    l.LivestockId,
                    l.LivestockTypeId,
                    TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                    l.FarmId,
                    l.PurchaseAmount,
                    Display = l.LivestockId + " (" + LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")"
                })
                .ToListAsync(ct);
            return View();
        }
        return RedirectToAction(nameof(Details), new { id = purchase.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> PostPurchase(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            var postDto = new PurchasePostDto { Id = id, FinalizeSupplierSnapshot = true };
            var purchase = await _purchaseService.PostPurchaseAsync(postDto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> Void(Guid id, string reason, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            await _purchaseService.VoidPurchaseAsync(id, reason ?? "User voided", companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public async Task<IActionResult> MobileIndex(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var list = await _db.Purchases
            .Where(p => p.CompanyId == companyId)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.PurchaseDate)
            .Take(100)
            .Select(p => new PurchaseListItemDto
            {
                Id = p.Id,
                PurchaseNumber = p.PurchaseNumber,
                PurchaseDate = p.PurchaseDate,
                SupplierName = p.Supplier != null ? p.Supplier.Name : null,
                GrandTotal = p.GrandTotal,
                Status = p.Status,
                ItemCount = p.Items.Count
            })
            .AsNoTracking()
            .ToListAsync(ct);
        ViewData["CanEdit"] = CanEdit;
        return View("MobileIndex", list);
    }

    [HttpGet]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> MobileCreate(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["ActiveLivestock"] = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.PurchaseAmount,
                Display = l.LivestockId + " (" + LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")"
            })
            .ToListAsync(ct);
        var dto = new PurchaseCreateDto
        {
            CompanyId = companyId,
            Currency = Currency.USD
        };
        return View("MobileCreate", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManagePurchases")]
    public async Task<IActionResult> MobileCreate(PurchaseCreateDto dto, string[] lineDesc, int[] lineQty, decimal[] lineUnitCost, Guid[] lineLivestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        dto.PurchaseDate = DateTimeOffset.UtcNow;
        dto.Items = new List<PurchaseItemCreateDto>();

        if (lineDesc != null)
        {
            for (int i = 0; i < lineDesc.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lineDesc[i]))
                {
                    var qty = lineQty != null && i < lineQty.Length && lineQty[i] > 0 ? lineQty[i] : 1;
                    var cost = lineUnitCost != null && i < lineUnitCost.Length ? lineUnitCost[i] : 0;
                    var item = new PurchaseItemCreateDto
                    {
                        LineNo = i + 1,
                        ItemType = PurchaseItemType.General,
                        Description = lineDesc[i],
                        Quantity = qty,
                        UnitCost = cost
                    };
                    if (lineLivestockId != null && i < lineLivestockId.Length && lineLivestockId[i] != Guid.Empty)
                    {
                        item.LivestockId = lineLivestockId[i];
                        item.ItemType = PurchaseItemType.Livestock;
                    }
                    dto.Items.Add(item);
                }
            }
        }

        try
        {
            await _purchaseService.CreateDraftAsync(dto, companyId, ct);
            return RedirectToAction(nameof(MobileIndex));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
            ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
            ViewData["ActiveLivestock"] = await _db.Livestock
                .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
                .Select(l => new
                {
                    l.Id,
                    l.LivestockId,
                    l.LivestockTypeId,
                    TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                    l.FarmId,
                    l.PurchaseAmount,
                    Display = l.LivestockId + " (" + LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId) + ")"
                })
                .ToListAsync(ct);
            return View("MobileCreate", dto);
        }
    }
}
