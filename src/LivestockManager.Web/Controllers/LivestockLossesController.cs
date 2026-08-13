using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.LivestockLosses;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.LivestockLosses;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Helpers;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewOperationalData")]
public class LivestockLossesController : Controller
{
    private readonly ILivestockLossService _lossService;
    private readonly IFarmService _farmService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public LivestockLossesController(
        ILivestockLossService lossService,
        IFarmService farmService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _lossService = lossService;
        _farmService = farmService;
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private bool CanEdit =>
        User.IsInRole(RoleNames.FarmManager) ||
        User.IsInRole(RoleNames.Accounts) ||
        User.IsInRole(RoleNames.CompanyAdministrator) ||
        User.IsInRole(RoleNames.SystemAdministrator);

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> Index(
        DateTime? from,
        DateTime? to,
        Guid? farmId,
        Guid? livestockId,
        DischargeCondition? lossType,
        bool includeReversed = false,
        CancellationToken ct = default)
    {
        var companyId = await GetCompanyIdAsync();
        var fromDto = from.AsUtcDayStartOrDefault();
        var toDto = to.AsUtcDayEndOrDefault();

        ViewData["From"] = from.HasValue ? from.Value.ToString("yyyy-MM-dd") : string.Empty;
        ViewData["To"] = to.HasValue ? to.Value.ToString("yyyy-MM-dd") : string.Empty;
        ViewData["FarmId"] = farmId;
        ViewData["LivestockId"] = livestockId;
        ViewData["LossType"] = lossType;
        ViewData["IncludeReversed"] = includeReversed;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;

        var list = await _lossService.ListAsync(companyId, farmId, livestockId, lossType, fromDto, toDto, includeReversed, ct);
        return View(list);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        try
        {
            var loss = await _lossService.GetByIdAsync(id, companyId, ct);
            ViewData["CanEdit"] = CanEdit;
            return View(loss);
        }
        catch (DomainException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [Authorize(Policy = "CanManageLosses")]
    public async Task<IActionResult> Create(Guid? livestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        var activeLivestockRaw = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .OrderBy(l => l.LivestockId)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                FarmName = l.FarmId.HasValue ? l.Farm!.Name : null,
                l.FarmId,
                l.PurchaseAmount
            })
            .ToListAsync(ct);

        var activeLivestock = activeLivestockRaw
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.FarmName,
                l.PurchaseAmount
            })
            .ToList();

        ViewData["ActiveLivestock"] = activeLivestock;

        var dto = new LivestockLossCreateDto
        {
            CompanyId = companyId,
            LossDate = DateTimeOffset.Now,
            LossType = DischargeCondition.Deceased,
            Currency = Currency.USD
        };

        if (livestockId.HasValue)
        {
            var existing = activeLivestock.FirstOrDefault(l => l.Id == livestockId.Value);
            if (existing != null)
            {
                dto.LivestockId = existing.Id;
                dto.FarmId = existing.FarmId;
                dto.BookValue = existing.PurchaseAmount;
            }
        }

        return View(dto);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> MobileIndex(DateTime? from, DateTime? to, Guid? farmId, DischargeCondition? lossType, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var list = await _lossService.ListAsync(
            companyId, farmId, null, lossType,
            from.AsUtcDayStartOrDefault(), to.AsUtcDayEndOrDefault(), false, ct);
        ViewData["From"] = from.HasValue ? from.Value.ToString("yyyy-MM-dd") : string.Empty;
        ViewData["To"] = to.HasValue ? to.Value.ToString("yyyy-MM-dd") : string.Empty;
        ViewData["FarmId"] = farmId;
        ViewData["LossType"] = lossType;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        return View("MobileIndex", list);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageLosses")]
    public async Task<IActionResult> MobileCreate(Guid? livestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);

        var activeLivestockRaw = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .OrderBy(l => l.LivestockId)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                FarmName = l.FarmId.HasValue ? l.Farm!.Name : null,
                l.FarmId,
                l.PurchaseAmount
            })
            .ToListAsync(ct);

        var activeLivestock = activeLivestockRaw
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.FarmName,
                l.PurchaseAmount
            })
            .ToList();

        ViewData["ActiveLivestock"] = activeLivestock;

        var dto = new LivestockLossCreateDto
        {
            CompanyId = companyId,
            LossDate = DateTimeOffset.Now,
            LossType = DischargeCondition.Deceased,
            Currency = Currency.USD
        };

        if (livestockId.HasValue)
        {
            var existing = activeLivestock.FirstOrDefault(l => l.Id == livestockId.Value);
            if (existing != null)
            {
                dto.LivestockId = existing.Id;
                dto.FarmId = existing.FarmId;
                dto.BookValue = existing.PurchaseAmount;
            }
        }

        return View("MobileCreate", dto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageLosses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MobileCreate(LivestockLossCreateDto dto, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        try
        {
            await _lossService.CreateAsync(dto, companyId, ct);
            return RedirectToAction(nameof(MobileIndex));
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        var activeLivestockRaw = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .OrderBy(l => l.LivestockId)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                l.FarmId,
                FarmName = l.FarmId.HasValue ? l.Farm!.Name : null,
                PurchaseAmount = (decimal?)l.PurchaseAmount
            })
            .ToListAsync(ct);
        ViewData["ActiveLivestock"] = activeLivestockRaw
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.FarmName,
                l.PurchaseAmount
            })
            .ToList();
        return View("MobileCreate", dto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageLosses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LivestockLossCreateDto dto, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        try
        {
            dto.CompanyId = companyId;
            var result = await _lossService.CreateAsync(dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        var activeLivestockRaw = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .OrderBy(l => l.LivestockId)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                FarmName = l.FarmId.HasValue ? l.Farm!.Name : null,
                l.FarmId,
                l.PurchaseAmount
            })
            .ToListAsync(ct);
        ViewData["ActiveLivestock"] = activeLivestockRaw
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                TypeLabel = LivestockTypeDisplay.GetDisplayName(l.LivestockTypeId),
                l.FarmId,
                l.FarmName,
                l.PurchaseAmount
            })
            .ToList();
        ViewData["CanEdit"] = CanEdit;
        return View(dto);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageLosses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reverse(Guid id, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            ModelState.AddModelError(nameof(reason), "Reversal reason is required.");

        var companyId = await GetCompanyIdAsync();
        try
        {
            await _lossService.ReverseAsync(id, reason ?? "User reversed loss entry", companyId, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
