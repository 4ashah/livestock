using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Livestock;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Domain.Enums;
using LivestockManager.Web.Models.LivestockViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize]
public class LivestockController : Controller
{
    private readonly ILivestockService _livestockService;
    private readonly IFarmService _farmService;
    private readonly IAppDbContext _db;

    public LivestockController(
        ILivestockService livestockService,
        IFarmService farmService,
        IAppDbContext db)
    {
        _livestockService = livestockService;
        _farmService = farmService;
        _db = db;
    }

    private async Task<Guid> GetCompanyIdAsync(CancellationToken ct)
    {
        var companyClaim = User.FindFirstValue("CompanyId");
        if (!string.IsNullOrWhiteSpace(companyClaim) && Guid.TryParse(companyClaim, out var cid))
            return cid;

        var defaultCompany = await _db.Companies.FirstOrDefaultAsync(ct);
        return defaultCompany?.Id ?? Guid.Empty;
    }

    private async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetFarmSelectListAsync(Guid? selectedFarmId, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        var farms = await _farmService.ListByCompanyAsync(companyId, null, ct);
        return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(farms, "Id", "Name", selectedFarmId);
    }

    private static string GetLivestockTypeDescription(LivestockType type)
    {
        return type switch
        {
            LivestockType.Ah => "Ah - Purchased castrated ram",
            LivestockType.Su => "Su - Uncastrated ram",
            LivestockType.Sa => "Sa - Purchased ewe",
            LivestockType.Ad => "Ad - Bred castrated ram",
            LivestockType.Sd => "Sd - Bred ewe",
            _ => type.ToString()
        };
    }

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

    public async Task<IActionResult> Index(
        Guid? farmId,
        LivestockType? livestockTypeId,
        LivestockStatus? status,
        string? searchString,
        CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);

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
            .Select(l => new LivestockListItemDto
            {
                Id = l.Id,
                LivestockId = l.LivestockId,
                LivestockType = l.LivestockTypeId,
                FarmName = l.Farm != null ? l.Farm.Name : null,
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
            .ToListAsync(ct);

        var vm = new LivestockIndexViewModel
        {
            FarmId = farmId,
            LivestockTypeId = livestockTypeId,
            Status = status,
            SearchString = searchString,
            Items = items,
            FarmOptions = await GetFarmSelectListAsync(farmId, ct)
        };

        ViewData["FarmId"] = farmId;
        ViewData["LivestockTypeId"] = livestockTypeId;
        ViewData["Status"] = status;
        ViewData["SearchString"] = searchString;
        ViewData["GetStatusBadgeClass"] = (Func<LivestockStatus, string>)GetStatusBadgeClass;
        ViewData["GetStatusDescription"] = (Func<LivestockStatus, string>)GetStatusDescription;
        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;

        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var detail = await _livestockService.GetByIdAsync(id, ct);

        var vm = new LivestockDetailsViewModel
        {
            Id = detail.Id,
            LivestockId = detail.LivestockId,
            Type = detail.LivestockTypeId,
            FarmName = detail.FarmName,
            FarmId = detail.FarmId,
            AcquisitionDate = detail.AcquisitionDate,
            InitialWeight = detail.InitialWeight,
            InitialWeightUnit = detail.WeightUnit,
            CurrentWeight = detail.CurrentWeight,
            CurrentWeightDate = detail.CurrentWeightDate,
            Status = detail.Status,
            PurchaseAmount = detail.PurchaseAmount,
            SoldAmount = detail.SoldAmount,
            BasicProfitLoss = detail.BasicProfitLoss,
            Comments = detail.Comments,
            DischargeDate = detail.DischargeDate,
            DischargeCondition = detail.DischargeCondition,
            DischargeDetails = detail.DischargeDetails,
            Weights = detail.WeightHistory
                .OrderByDescending(w => w.WeighedAt)
                .Take(10)
                .Select(w => new LivestockWeightHistoryItem
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

    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    public async Task<IActionResult> Register(CancellationToken ct)
    {
        var vm = new LivestockRegisterViewModel
        {
            FarmOptions = await GetFarmSelectListAsync(null, ct)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LivestockRegisterViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            return View(vm);
        }

        var companyId = await GetCompanyIdAsync(ct);

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
            var result = await _livestockService.RegisterAsync(dto, ct);
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            return View(vm);
        }
    }

    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var detail = await _livestockService.GetByIdAsync(id, ct);

        var vm = new LivestockEditViewModel
        {
            Id = detail.Id,
            LivestockId = detail.LivestockId,
            LivestockTypeId = detail.LivestockTypeId,
            FarmId = detail.FarmId,
            AcquisitionDate = detail.AcquisitionDate,
            InitialWeight = detail.InitialWeight,
            Comments = detail.Comments,
            FarmOptions = await GetFarmSelectListAsync(detail.FarmId, ct)
        };

        ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, LivestockEditViewModel vm, CancellationToken ct)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            var detail = await _livestockService.GetByIdAsync(id, ct);
            vm.LivestockId = detail.LivestockId;
            vm.LivestockTypeId = detail.LivestockTypeId;
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
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
            await _livestockService.UpdateAsync(id, dto, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var detail = await _livestockService.GetByIdAsync(id, ct);
            vm.LivestockId = detail.LivestockId;
            vm.LivestockTypeId = detail.LivestockTypeId;
            vm.FarmOptions = await GetFarmSelectListAsync(vm.FarmId, ct);
            ViewData["GetLivestockTypeDescription"] = (Func<LivestockType, string>)GetLivestockTypeDescription;
            return View(vm);
        }
    }

    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    public async Task<IActionResult> AddWeight(Guid id, CancellationToken ct)
    {
        var detail = await _livestockService.GetByIdAsync(id, ct);

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
    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWeight(Guid id, AddWeightViewModel vm, CancellationToken ct)
    {
        if (id != vm.LivestockId)
            return NotFound();

        var detail = await _livestockService.GetByIdAsync(id, ct);
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
            await _livestockService.AddWeightAsync(id, dto, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    public async Task<IActionResult> WeightHistory(Guid id, CancellationToken ct)
    {
        var detail = await _livestockService.GetByIdAsync(id, ct);
        var weights = await _livestockService.GetWeightHistoryAsync(id, ct);

        var vm = new List<LivestockWeightHistoryItem>();
        foreach (var w in weights.OrderByDescending(w => w.WeighedAt))
        {
            vm.Add(new LivestockWeightHistoryItem
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

    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Discharge(Guid id, CancellationToken ct)
    {
        var detail = await _livestockService.GetByIdAsync(id, ct);

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
    [Authorize(Roles = "Administrator,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Discharge(Guid id, DischargeViewModel vm, CancellationToken ct)
    {
        if (id != vm.LivestockId)
            return NotFound();

        var detail = await _livestockService.GetByIdAsync(id, ct);
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
            await _livestockService.DischargeAsync(id, dto, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    public async Task<IActionResult> AddComment(Guid id, string newComment, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(newComment))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var activityDto = new LivestockActivityDto
        {
            ActivityType = LivestockActivityType.Note,
            Description = newComment.Trim(),
            PerformedAt = DateTimeOffset.Now
        };

        try
        {
            await _livestockService.AddActivityAsync(id, activityDto, ct);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Administrator,Manager,DataEntry")]
    public async Task<FileContentResult> ExportCsv(
        Guid? farmId,
        LivestockType? livestockTypeId,
        LivestockStatus? status,
        string? searchString,
        CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);

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
            .ToListAsync(ct);

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
