using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.DTOs.Farms;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.ValueObjects;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.FarmViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Farms.View)]
public class FarmsController : Controller
{
    private readonly IFarmService _farmService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FarmsController(
        IFarmService farmService,
        UserManager<ApplicationUser> userManager)
    {
        _farmService = farmService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        ViewData["Search"] = search;
        return View(await BuildListVmAsync(companyId.Value, search, ct));
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Farms.Details)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        FarmDetailDto dto;
        try
        {
            dto = await _farmService.GetByIdAsync(id, companyId.Value, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        string? managerInfo = null;
        if (dto.ManagerUserId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(dto.ManagerUserId.Value.ToString());
            if (user != null)
                managerInfo = user.FullName;
        }

        var vm = new FarmDetailsViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Code = dto.Code,
            Street1 = dto.Address?.Street1,
            Street2 = dto.Address?.Street2,
            City = dto.Address?.City,
            State = dto.Address?.State,
            PostalCode = dto.Address?.PostalCode,
            Country = dto.Address?.Country,
            Currency = dto.Currency,
            WeightUnit = dto.WeightUnit,
            IsActive = dto.IsActive,
            ManagerInfo = managerInfo,
            LivestockCount = dto.LivestockCount,
            ActiveLivestockCount = dto.ActiveLivestockCount
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Farms.Create)]
    public IActionResult Create()
    {
        var vm = new FarmCreateEditViewModel();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Farms.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FarmCreateEditViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        var dto = new FarmCreateDto
        {
            CompanyId = companyId.Value,
            Name = vm.Name.Trim(),
            Code = vm.Code.Trim(),
            Address = new Address(vm.Street1, vm.Street2, vm.City, vm.State, vm.PostalCode, vm.Country),
            Currency = vm.Currency,
            WeightUnit = vm.WeightUnit
        };

        var created = await _farmService.CreateAsync(dto, companyId.Value, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Farms.Edit)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        FarmDetailDto dto;
        try
        {
            dto = await _farmService.GetByIdAsync(id, companyId.Value, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var vm = new FarmCreateEditViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Code = dto.Code,
            Street1 = dto.Address?.Street1,
            Street2 = dto.Address?.Street2,
            City = dto.Address?.City,
            State = dto.Address?.State,
            PostalCode = dto.Address?.PostalCode,
            Country = dto.Address?.Country,
            Currency = dto.Currency,
            WeightUnit = dto.WeightUnit,
            IsActive = dto.IsActive
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Farms.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FarmCreateEditViewModel vm, CancellationToken ct)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(vm);

        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        var updateDto = new FarmUpdateDto
        {
            Name = vm.Name.Trim(),
            Code = vm.Code.Trim(),
            Address = new Address(vm.Street1, vm.Street2, vm.City, vm.State, vm.PostalCode, vm.Country),
            Currency = vm.Currency,
            WeightUnit = vm.WeightUnit,
            IsActive = vm.IsActive
        };

        try
        {
            await _farmService.UpdateAsync(id, updateDto, companyId.Value, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Farms.Archive)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        try
        {
            await _farmService.DeleteAsync(id, companyId.Value, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<Guid?> GetCompanyIdAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId;
    }

    private async Task<List<FarmListViewModel>> BuildListVmAsync(Guid companyId, string? search, CancellationToken ct)
    {
        var dtos = await _farmService.ListByCompanyAsync(companyId, search, ct);

        var managerIds = dtos
            .Where(d => d.ManagerUserId.HasValue)
            .Select(d => d.ManagerUserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> managerNames = [];
        if (managerIds.Count > 0)
        {
            foreach (var mid in managerIds)
            {
                var user = await _userManager.FindByIdAsync(mid.ToString());
                if (user != null)
                    managerNames[mid] = user.FullName;
            }
        }

        return dtos.Select(d => new FarmListViewModel
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            City = d.City,
            Currency = d.Currency,
            IsActive = d.IsActive,
            ManagerInfo = d.ManagerUserId.HasValue && managerNames.TryGetValue(d.ManagerUserId.Value, out var managerName)
                ? managerName
                : null
        }).ToList();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Farms.View)]
    public async Task<IActionResult> MobileIndex(string? search, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        ViewData["Search"] = search;
        return View("MobileIndex", await BuildListVmAsync(companyId.Value, search, ct));
    }
}
