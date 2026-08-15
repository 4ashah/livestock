using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.Helpers;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.LivestockViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Livestock.View)]
public class LivestockController : Controller
{
    private readonly ILivestockService _livestockService;
    private readonly IFarmService _farmService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public LivestockController(
        ILivestockService livestockService,
        IFarmService farmService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _livestockService = livestockService;
        _farmService = farmService;
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetFarmSelectListAsync(Guid? selectedFarmId, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        var farms = await _farmService.ListByCompanyAsync(companyId, null, cancellationToken);
        return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(farms, "Id", "Name", selectedFarmId);
    }

    private static string GetLivestockTypeDescription(LivestockType type)
        => LivestockTypeDisplay.GetDisplayName(type);

    private static string GetStatusBadgeClass(LivestockStatus status)
    {
        return status switch
        {
            LivestockStatus.Active => "bg-success",
            LivestockStatus.DischargedSold => "bg-primary",
            LivestockStatus.DischargedDeceased => "bg-secondary",
            LivestockStatus.DischargedLost => "bg-warning text-dark",
            LivestockStatus.DischargedStolen => "bg-danger",
            _ => "bg-light text-dark"
        };
    }

    private static string GetStatusDescription(LivestockStatus status)
    {
        return status switch
        {
            LivestockStatus.Active => "Active",
            LivestockStatus.DischargedSold => "Discharged: Sold",
            LivestockStatus.DischargedDeceased => "Discharged: Deceased",
            LivestockStatus.DischargedLost => "Discharged: Lost",
            LivestockStatus.DischargedStolen => "Discharged: Stolen",
            _ => "Discharged: Other"
        };
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.View)]
    public async Task<IActionResult> Index(
        Guid? farmId,
        LivestockType? livestockTypeId,
        LivestockStatus? status,
        StockSource? sourceFilter,
        string? searchString,
        CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);

        var query = _db.Livestock
            .Include(l => l.Farm)
            .Where(l => l.CompanyId == companyId)
            .AsQueryable();

        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        if (livestockTypeId.HasValue)
            query = query.Where(l => l.LivestockTypeId == livestockTypeId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (sourceFilter.HasValue)
            query = query.Where(l => l.StockSource == sourceFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();
            query = query.Where(l =>
                l.LivestockId.Contains(searchString) ||
                (l.Comments != null && l.Comments.Contains(searchString)));
        }

        var items = await query
            .OrderByDescending(l => l.AcquisitionDate)
            .Select(l => new LivestockListItemViewModel
            {
                Id = l.Id,
                LivestockId = l.LivestockId,
                LivestockType = l.LivestockTypeId,
                FarmName = l.Farm != null ? l.Farm.Name : null,
                StockSource = l.StockSource,
                DateOfBirth = l.DateOfBirth,
                AcquisitionDate = l.AcquisitionDate,
                InitialWeight = l.InitialWeight,
                CurrentWeight = l.CurrentWeight,
                Status = l.Status,
                DaysInHerd = l.Status == LivestockStatus.Active
                    ? (int)Math.Floor((DateTimeOffset.Now - l.AcquisitionDate).TotalDays)
                    : l.DischargeDate.HasValue
                        ? (int)Math.Floor((l.DischargeDate.Value - l.AcquisitionDate).TotalDays)
                        : (int)Math.Floor((DateTimeOffset.Now - l.AcquisitionDate).TotalDays),
                SoldAmount = l.SoldAmount,
                BasicProfitLoss = l.BasicProfitLoss
            })
            .ToListAsync(cancellationToken);

        var vm = new LivestockIndexViewModel
        {
            FarmId = farmId,
            LivestockTypeId = livestockTypeId,
            Status = status,
            SourceFilter = sourceFilter,
            SearchString = searchString,
            Items = items,
            FarmOptions = await GetFarmSelectListAsync(farmId, cancellationToken)
        };

        ViewData["FarmId"] = farmId;
        ViewData["LivestockTypeId"] = livestockTypeId;
        ViewData["Status"] = status;
        ViewData["SourceFilter"] = sourceFilter;
        ViewData["SearchString"] = searchString;
        ViewData["GetStatusBadgeClass"] = (Func<LivestockStatus, string>)GetStatusBadgeClass;
        ViewData["GetStatusDescription"] = (Func<LivestockStatus, string>)GetStatusDescription;
        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.Details)]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var entity = await _db.Livestock
            .AsNoTracking()
            .Where(l => l.Id == id && l.CompanyId == companyId)
            .Select(l => new { l.StockSource, l.DateOfBirth, l.MotherLivestockId, l.FatherLivestockId })
            .FirstOrDefaultAsync(cancellationToken);

        var vm = new LivestockDetailsViewModel
        {
            Id = detail.Id,
            LivestockId = detail.LivestockId,
            Type = detail.LivestockTypeId,
            FarmName = detail.FarmName,
            FarmId = detail.FarmId,
            StockSource = entity?.StockSource ?? StockSource.Purchased,
            DateOfBirth = entity?.DateOfBirth ?? detail.DateOfBirth,
            MotherLivestockId = entity?.MotherLivestockId,
            FatherLivestockId = entity?.FatherLivestockId,
            MotherLivestockIdCode = detail.MotherLivestockIdCode,
            FatherLivestockIdCode = detail.FatherLivestockIdCode,
            AcquisitionDate = detail.AcquisitionDate,
            InitialWeight = detail.InitialWeight,
            InitialWeightUnit = detail.WeightUnit,
            CurrentWeight = detail.CurrentWeight,
            CurrentWeightDate = detail.CurrentWeightDate,
            Status = detail.Status,
            PurchaseAmount = detail.PurchaseAmount,
            AllocatedCommission = detail.AllocatedCommission,
            AllocatedTax = detail.AllocatedTax,
            AllocatedTransportation = detail.AllocatedTransportation,
            AllocatedOtherCost = detail.AllocatedOtherCost,
            OtherCostDescription = detail.OtherCostDescription,
            TotalAcquisitionCost = detail.TotalAcquisitionCost,
            SoldAmount = detail.SoldAmount,
            BasicProfitLoss = detail.BasicProfitLoss,
            Comments = detail.Comments,
            DischargeDate = detail.DischargeDate,
            DischargeCondition = detail.DischargeCondition,
            DischargeDetails = detail.DischargeDetails,
            Weights = detail.WeightHistory
                .OrderByDescending(w => w.WeighedAt)
                .Take(10)
                .Select(w => new LivestockWeightHistoryItemViewModel
                {
                    LivestockWeightId = w.Id,
                    Weight = w.Weight,
                    Unit = w.Unit,
                    WeighedAt = w.WeighedAt,
                    Notes = w.Notes
                })
                .ToList(),
            Activities = detail.Activities.ToList()
        };

        ViewData["GetStatusBadgeClass"] = (Func<LivestockStatus, string>)GetStatusBadgeClass;
        ViewData["GetStatusDescription"] = (Func<LivestockStatus, string>)GetStatusDescription;
        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.Register)]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        var vm = new LivestockRegisterViewModel
        {
            FarmOptions = await GetFarmSelectListAsync(null, cancellationToken)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Livestock.Register)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LivestockRegisterViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            return View(vm);
        }

        var companyId = await GetCompanyIdAsync(cancellationToken);

        var dto = new LivestockRegisterDto
        {
            CompanyId = companyId,
            FarmId = vm.FarmId,
            LivestockType = vm.LivestockTypeId,
            AcquisitionDate = vm.AcquisitionDate,
            InitialWeight = vm.InitialWeight,
            WeightUnit = vm.WeightUnit,
            PurchaseAmount = vm.PurchaseAmount,
            Comments = vm.Comments
        };

        try
        {
            var result = await _livestockService.RegisterAsync(dto, companyId, cancellationToken);
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.Edit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var vm = new LivestockEditViewModel
        {
            Id = detail.Id,
            LivestockId = detail.LivestockId,
            LivestockTypeId = detail.LivestockTypeId,
            FarmId = detail.FarmId,
            AcquisitionDate = detail.AcquisitionDate,
            InitialWeight = detail.InitialWeight,
            Comments = detail.Comments,
            FarmOptions = await GetFarmSelectListAsync(detail.FarmId, cancellationToken)
        };

        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;

        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Livestock.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, LivestockEditViewModel vm, CancellationToken cancellationToken)
    {
        if (id != vm.Id)
            return NotFound();

        var companyId = await GetCompanyIdAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            LivestockDetailDto detail;
            try
            {
                detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
            }
            catch (DomainException)
            {
                return NotFound();
            }
            vm.LivestockId = detail.LivestockId;
            vm.LivestockTypeId = detail.LivestockTypeId;
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;
            return View(vm);
        }

        var dto = new LivestockEditDto
        {
            FarmId = vm.FarmId,
            AcquisitionDate = vm.AcquisitionDate,
            InitialWeight = vm.InitialWeight,
            WeightUnit = WeightUnit.Kg,
            Comments = vm.Comments
        };

        try
        {
            await _livestockService.UpdateAsync(id, dto, companyId, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            LivestockDetailDto detail;
            try
            {
                detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
            }
            catch (DomainException)
            {
                return NotFound();
            }
            vm.LivestockId = detail.LivestockId;
            vm.LivestockTypeId = detail.LivestockTypeId;
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;
            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.AddWeight)]
    public async Task<IActionResult> AddWeight(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        if (detail.Status != LivestockStatus.Active)
        {
            TempData["Error"] = "Cannot add weight to discharged livestock.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new AddWeightViewModel
        {
            LivestockId = detail.Id,
            LivestockDisplayId = detail.LivestockId,
            WeighedAt = new DateTimeOffset(DateTime.Today),
            Unit = detail.WeightUnit
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Livestock.AddWeight)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWeight(Guid id, AddWeightViewModel vm, CancellationToken cancellationToken)
    {
        if (id != vm.LivestockId)
            return NotFound();

        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        vm.LivestockDisplayId = detail.LivestockId;

        if (!ModelState.IsValid)
            return View(vm);

        var dto = new LivestockWeightAddDto
        {
            Weight = vm.Weight,
            Unit = vm.Unit,
            WeighedAt = vm.WeighedAt,
            Notes = vm.Notes
        };

        try
        {
            await _livestockService.AddWeightAsync(id, dto, companyId, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.View)]
    public async Task<IActionResult> WeightHistory(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        var weights = await _livestockService.GetWeightHistoryAsync(id, companyId, cancellationToken);

        var vm = new List<LivestockWeightHistoryItemViewModel>();
        foreach (var w in weights.OrderByDescending(w => w.WeighedAt))
        {
            vm.Add(new LivestockWeightHistoryItemViewModel
            {
                LivestockWeightId = w.Id,
                Weight = w.Weight,
                Unit = w.Unit,
                WeighedAt = w.WeighedAt,
                Notes = w.Notes
            });
        }

        ViewData["LivestockId"] = detail.LivestockId;
        ViewData["LivestockGuid"] = id;
        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.View)]
    public async Task<IActionResult> MobileIndex(
        Guid? farmId,
        LivestockType? livestockTypeId,
        LivestockStatus? status,
        StockSource? sourceFilter,
        string? searchString,
        CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        var query = _db.Livestock
            .Include(l => l.Farm)
            .Where(l => l.CompanyId == companyId)
            .AsQueryable();

        if (farmId.HasValue) query = query.Where(l => l.FarmId == farmId.Value);
        if (livestockTypeId.HasValue) query = query.Where(l => l.LivestockTypeId == livestockTypeId.Value);
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);
        if (sourceFilter.HasValue) query = query.Where(l => l.StockSource == sourceFilter.Value);
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();
            query = query.Where(l =>
                l.LivestockId.Contains(searchString) ||
                (l.Comments != null && l.Comments.Contains(searchString)));
        }

        var items = await query
            .OrderByDescending(l => l.AcquisitionDate)
            .Select(l => new LivestockListItemViewModel
            {
                Id = l.Id,
                LivestockId = l.LivestockId,
                LivestockType = l.LivestockTypeId,
                FarmName = l.Farm != null ? l.Farm.Name : null,
                StockSource = l.StockSource,
                DateOfBirth = l.DateOfBirth,
                AcquisitionDate = l.AcquisitionDate,
                InitialWeight = l.InitialWeight,
                CurrentWeight = l.CurrentWeight,
                Status = l.Status,
                DaysInHerd = l.Status == LivestockStatus.Active
                    ? (int)Math.Floor((DateTimeOffset.Now - l.AcquisitionDate).TotalDays)
                    : l.DischargeDate.HasValue
                        ? (int)Math.Floor((l.DischargeDate.Value - l.AcquisitionDate).TotalDays)
                        : (int)Math.Floor((DateTimeOffset.Now - l.AcquisitionDate).TotalDays),
                SoldAmount = l.SoldAmount,
                BasicProfitLoss = l.BasicProfitLoss
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        ViewData["FarmId"] = farmId;
        ViewData["LivestockTypeId"] = livestockTypeId;
        ViewData["Status"] = status;
        ViewData["SourceFilter"] = sourceFilter;
        ViewData["SearchString"] = searchString;
        ViewData["FarmOptions"] = await GetFarmSelectListAsync(farmId, cancellationToken);
        ViewData["GetStatusBadgeClass"] = (Func<LivestockStatus, string>)GetStatusBadgeClass;
        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;
        return View("MobileIndex", items);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.Register)]
    public async Task<IActionResult> MobileRegister(CancellationToken cancellationToken)
    {
        var vm = new LivestockRegisterViewModel { FarmOptions = await GetFarmSelectListAsync(null, cancellationToken) };
        return View("MobileRegister", vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Livestock.Register)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MobileRegister(LivestockRegisterViewModel vm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            return View("MobileRegister", vm);
        }
        var companyId = await GetCompanyIdAsync(cancellationToken);
        var dto = new LivestockRegisterDto
        {
            CompanyId = companyId,
            FarmId = vm.FarmId,
            LivestockType = vm.LivestockTypeId,
            AcquisitionDate = vm.AcquisitionDate,
            InitialWeight = vm.InitialWeight,
            WeightUnit = vm.WeightUnit,
            PurchaseAmount = vm.PurchaseAmount,
            Comments = vm.Comments
        };
        try
        {
            await _livestockService.RegisterAsync(dto, companyId, cancellationToken);
            return RedirectToAction(nameof(MobileIndex));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, cancellationToken);
            return View("MobileRegister", vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.Discharge)]
    public async Task<IActionResult> Discharge(Guid id, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        if (detail.Status != LivestockStatus.Active)
        {
            TempData["Error"] = "Livestock has already been discharged.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new DischargeViewModel
        {
            LivestockId = detail.Id,
            LivestockDisplayId = detail.LivestockId,
            DischargeDate = new DateTimeOffset(DateTime.Today)
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Livestock.Discharge)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Discharge(Guid id, DischargeViewModel vm, CancellationToken cancellationToken)
    {
        if (id != vm.LivestockId)
            return NotFound();

        var companyId = await GetCompanyIdAsync(cancellationToken);
        LivestockDetailDto detail;
        try
        {
            detail = await _livestockService.GetByIdAsync(id, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        vm.LivestockDisplayId = detail.LivestockId;

        if (!ModelState.IsValid)
            return View(vm);

        var dto = new LivestockDischargeDto
        {
            Condition = vm.DischargeCondition,
            Date = vm.DischargeDate,
            Details = vm.Details,
            SoldAmount = vm.DischargeCondition == DischargeCondition.Sold ? vm.SoldAmount : null
        };

        try
        {
            await _livestockService.DischargeAsync(id, dto, companyId, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Livestock.AddWeight)]
    public async Task<IActionResult> AddComment(Guid id, string newComment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newComment))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var companyId = await GetCompanyIdAsync(cancellationToken);

        var activityDto = new LivestockActivityDto
        {
            ActivityType = LivestockActivityType.Note,
            Description = newComment.Trim(),
            PerformedAt = DateTimeOffset.Now
        };

        try
        {
            await _livestockService.AddActivityAsync(id, activityDto, companyId, cancellationToken);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Livestock.ExportCsv)]
    public async Task<FileContentResult> ExportCsv(
        Guid? farmId,
        LivestockType? livestockTypeId,
        LivestockStatus? status,
        string? searchString,
        CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyIdAsync(cancellationToken);

        var query = _db.Livestock
            .Include(l => l.Farm)
            .Where(l => l.CompanyId == companyId)
            .AsQueryable();

        if (farmId.HasValue)
            query = query.Where(l => l.FarmId == farmId.Value);

        if (livestockTypeId.HasValue)
            query = query.Where(l => l.LivestockTypeId == livestockTypeId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();
            query = query.Where(l =>
                l.LivestockId.Contains(searchString) ||
                (l.Comments != null && l.Comments.Contains(searchString)));
        }

        var items = await query
            .OrderByDescending(l => l.AcquisitionDate)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("LivestockId,Type,Farm,Status,AcquisitionDate,InitialWeight,CurrentWeight,PurchaseAmount,SoldAmount,BasicProfitLoss,Comments");

        foreach (var l in items)
        {
            var farmName = l.Farm?.Name ?? "";
            var comments = l.Comments?.Replace(",", " ").Replace("\"", "\"\"") ?? "";
            sb.AppendLine(
                $"\"{l.LivestockId}\"," +
                $"\"{l.LivestockTypeId}\"," +
                $"\"{farmName}\"," +
                $"\"{l.Status}\"," +
                $"{l.AcquisitionDate:yyyy-MM-dd}," +
                $"{l.InitialWeight}," +
                $"{l.CurrentWeight}," +
                $"{l.PurchaseAmount}," +
                $"{l.SoldAmount}," +
                $"{l.BasicProfitLoss}," +
                $"\"{comments}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"Livestock_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}
