using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.StockAddition;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.StockAddition;
using LivestockManager.Application.Services.Suppliers;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.StockAdditionViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewLivestock)]
public class StockAdditionController : Controller
{
    private readonly IStockAdditionService _stockAdditionService;
    private readonly ILivestockService _livestockService;
    private readonly IFarmService _farmService;
    private readonly ISupplierService? _supplierService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<StockAdditionController> _logger;

    public StockAdditionController(
        IStockAdditionService stockAdditionService,
        ILivestockService livestockService,
        IFarmService farmService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<StockAdditionController> logger,
        ISupplierService? supplierService = null)
    {
        _stockAdditionService = stockAdditionService;
        _livestockService = livestockService;
        _farmService = farmService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _supplierService = supplierService;
    }

    private async Task<Guid> GetCompanyIdAsync(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetFarmSelectListAsync(Guid? selectedFarmId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        var farms = await _farmService.ListByCompanyAsync(companyId, null, ct);
        return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(farms, "Id", "Name", selectedFarmId);
    }

    private async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetSupplierSelectListAsync(Guid? selectedSupplierId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);

        if (_supplierService != null)
        {
            var suppliers = await _supplierService.ListAsync(companyId, ct);
            var activeSuppliers = suppliers
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToList();
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(activeSuppliers, "Id", "Name", selectedSupplierId);
        }
        else
        {
            var suppliers = await _db.Suppliers
                .Where(s => s.CompanyId == companyId && s.IsActive && !s.IsDeleted)
                .OrderBy(s => s.Name)
                .ToListAsync(ct);
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(suppliers, "Id", "Name", selectedSupplierId);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewLivestock)]
    public IActionResult Index()
    {
        ViewData["Title"] = "Stock Addition";
        ViewData["DockKey"] = "stock";
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordStockPurchase)]
    public async Task<IActionResult> Purchase(CancellationToken ct)
    {
        var vm = new StockAdditionPurchaseViewModel
        {
            IdempotencyKey = Guid.NewGuid(),
            PurchaseDate = new DateTimeOffset(DateTime.Today),
            FarmOptions = await GetFarmSelectListAsync(null, ct),
            SupplierOptions = await GetSupplierSelectListAsync(null, ct)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanRecordStockPurchase)]
    public async Task<IActionResult> Purchase(StockAdditionPurchaseViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            vm.SupplierOptions = await GetSupplierSelectListAsync(vm.SupplierId, ct);
            return View(vm);
        }

        var companyId = await GetCompanyIdAsync(ct);
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? Guid.Empty;

        var dto = new StockAdditionPurchasedDto
        {
            IdempotencyKey = vm.IdempotencyKey,
            SupplierId = vm.SupplierId,
            PurchaseDate = vm.PurchaseDate,
            FarmId = vm.FarmId,
            LivestockTypeId = vm.LivestockTypeId,
            PurchaseWeight = vm.PurchaseWeight,
            WeightUnit = vm.WeightUnit,
            PurchaseCost = vm.PurchaseCost,
            CommissionAmount = vm.CommissionAmount,
            TaxAmount = vm.TaxAmount,
            TransportationAmount = vm.TransportationAmount,
            OtherCostAmount = vm.OtherCostAmount,
            OtherCostDescription = string.IsNullOrWhiteSpace(vm.OtherCostDescription) ? null : vm.OtherCostDescription.Trim(),
            CostAllocationMethod = vm.CostAllocationMethod,
            SupplierReference = vm.SupplierReference,
            Comments = vm.Comments
        };

        var result = await _stockAdditionService.AddPurchasedLivestockAsync(dto, companyId, userId, ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to add purchased livestock.");
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            vm.SupplierOptions = await GetSupplierSelectListAsync(vm.SupplierId, ct);
            return View(vm);
        }

        if (result.LivestockEntityId.HasValue)
        {
            return RedirectToAction(nameof(Success), new { livestockId = result.LivestockEntityId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<IActionResult> Newborn(CancellationToken ct)
    {
        var vm = new StockAdditionNewbornViewModel
        {
            IdempotencyKey = Guid.NewGuid(),
            DateOfBirth = new DateTimeOffset(DateTime.Today),
            FarmOptions = await GetFarmSelectListAsync(null, ct)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<IActionResult> Newborn(StockAdditionNewbornViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            return View(vm);
        }

        var companyId = await GetCompanyIdAsync(ct);
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? Guid.Empty;

        var dto = new StockAdditionNewbornDto
        {
            IdempotencyKey = vm.IdempotencyKey,
            DateOfBirth = vm.DateOfBirth,
            FarmId = vm.FarmId,
            LivestockTypeId = vm.LivestockTypeId,
            BirthWeight = vm.BirthWeight,
            WeightUnit = vm.WeightUnit,
            MotherLivestockId = vm.MotherLivestockId,
            FatherLivestockId = vm.FatherLivestockId,
            BirthComments = vm.BirthComments
        };

        var result = await _stockAdditionService.AddNewbornLivestockAsync(dto, companyId, userId, ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to add newborn livestock.");
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            return View(vm);
        }

        if (result.LivestockEntityId.HasValue)
        {
            return RedirectToAction(nameof(Success), new { livestockId = result.LivestockEntityId.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<JsonResult> SearchEligibleEwes(string keyword, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        var results = await _stockAdditionService.SearchEligibleEwesAsync(keyword ?? string.Empty, companyId, 25, ct);
        return Json(results);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<JsonResult> SearchEligibleRams(string keyword, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        var results = await _stockAdditionService.SearchEligibleRamsAsync(keyword ?? string.Empty, companyId, 25, ct);
        return Json(results);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewLivestock)]
    public async Task<IActionResult> Success(Guid livestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        try
        {
            var detail = await _livestockService.GetByIdAsync(livestockId, companyId, ct);
            ViewData["LivestockDetail"] = detail;
            return View(livestockId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load livestock details for success page (LivestockId={LivestockId}); redirecting to index", livestockId);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewLivestock)]
    public IActionResult MobileIndex()
    {
        ViewData["DockKey"] = "stock";
        return View("MobileIndex");
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordStockPurchase)]
    public async Task<IActionResult> MobilePurchase(CancellationToken ct)
    {
        var vm = new StockAdditionPurchaseViewModel
        {
            IdempotencyKey = Guid.NewGuid(),
            PurchaseDate = new DateTimeOffset(DateTime.Today),
            FarmOptions = await GetFarmSelectListAsync(null, ct),
            SupplierOptions = await GetSupplierSelectListAsync(null, ct)
        };
        return View("MobilePurchase", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanRecordStockPurchase)]
    public async Task<IActionResult> MobilePurchase(StockAdditionPurchaseViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            vm.SupplierOptions = await GetSupplierSelectListAsync(vm.SupplierId, ct);
            return View("MobilePurchase", vm);
        }

        var companyId = await GetCompanyIdAsync(ct);
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? Guid.Empty;

        var dto = new StockAdditionPurchasedDto
        {
            IdempotencyKey = vm.IdempotencyKey,
            SupplierId = vm.SupplierId,
            PurchaseDate = vm.PurchaseDate,
            FarmId = vm.FarmId,
            LivestockTypeId = vm.LivestockTypeId,
            PurchaseWeight = vm.PurchaseWeight,
            WeightUnit = vm.WeightUnit,
            PurchaseCost = vm.PurchaseCost,
            CommissionAmount = vm.CommissionAmount,
            TaxAmount = vm.TaxAmount,
            TransportationAmount = vm.TransportationAmount,
            OtherCostAmount = vm.OtherCostAmount,
            OtherCostDescription = string.IsNullOrWhiteSpace(vm.OtherCostDescription) ? null : vm.OtherCostDescription.Trim(),
            CostAllocationMethod = vm.CostAllocationMethod,
            SupplierReference = vm.SupplierReference,
            Comments = vm.Comments
        };

        var result = await _stockAdditionService.AddPurchasedLivestockAsync(dto, companyId, userId, ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to add purchased livestock.");
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            vm.SupplierOptions = await GetSupplierSelectListAsync(vm.SupplierId, ct);
            return View("MobilePurchase", vm);
        }

        if (result.LivestockEntityId.HasValue)
        {
            return RedirectToAction(nameof(MobileSuccess), new { livestockId = result.LivestockEntityId.Value });
        }

        return RedirectToAction(nameof(MobileIndex));
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<IActionResult> MobileNewborn(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        var vm = new StockAdditionNewbornViewModel
        {
            IdempotencyKey = Guid.NewGuid(),
            DateOfBirth = new DateTimeOffset(DateTime.Today),
            FarmOptions = await GetFarmSelectListAsync(null, ct)
        };

        var ewes = await _stockAdditionService.SearchEligibleEwesAsync(string.Empty, companyId, 25, ct);
        var rams = await _stockAdditionService.SearchEligibleRamsAsync(string.Empty, companyId, 25, ct);

        ViewData["EweOptions"] = ewes;
        ViewData["RamOptions"] = rams;

        return View("MobileNewborn", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanRecordNewborn)]
    public async Task<IActionResult> MobileNewborn(StockAdditionNewbornViewModel vm, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);

        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            var ewes = await _stockAdditionService.SearchEligibleEwesAsync(string.Empty, companyId, 25, ct);
            var rams = await _stockAdditionService.SearchEligibleRamsAsync(string.Empty, companyId, 25, ct);
            ViewData["EweOptions"] = ewes;
            ViewData["RamOptions"] = rams;
            return View("MobileNewborn", vm);
        }

        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? Guid.Empty;

        var dto = new StockAdditionNewbornDto
        {
            IdempotencyKey = vm.IdempotencyKey,
            DateOfBirth = vm.DateOfBirth,
            FarmId = vm.FarmId,
            LivestockTypeId = vm.LivestockTypeId,
            BirthWeight = vm.BirthWeight,
            WeightUnit = vm.WeightUnit,
            MotherLivestockId = vm.MotherLivestockId,
            FatherLivestockId = vm.FatherLivestockId,
            BirthComments = vm.BirthComments
        };

        var result = await _stockAdditionService.AddNewbornLivestockAsync(dto, companyId, userId, ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to add newborn livestock.");
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            var ewes = await _stockAdditionService.SearchEligibleEwesAsync(string.Empty, companyId, 25, ct);
            var rams = await _stockAdditionService.SearchEligibleRamsAsync(string.Empty, companyId, 25, ct);
            ViewData["EweOptions"] = ewes;
            ViewData["RamOptions"] = rams;
            return View("MobileNewborn", vm);
        }

        if (result.LivestockEntityId.HasValue)
        {
            return RedirectToAction(nameof(MobileSuccess), new { livestockId = result.LivestockEntityId.Value });
        }

        return RedirectToAction(nameof(MobileIndex));
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewLivestock)]
    public async Task<IActionResult> MobileSuccess(Guid livestockId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        try
        {
            var detail = await _livestockService.GetByIdAsync(livestockId, companyId, ct);
            ViewData["LivestockDetail"] = detail;
            return View("MobileSuccess", livestockId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load livestock details for mobile success page (LivestockId={LivestockId}); redirecting to index", livestockId);
            return RedirectToAction(nameof(MobileIndex));
        }
    }
}
