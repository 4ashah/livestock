using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.DTOs.Farms;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Domain.ValueObjects;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.FarmViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewOperationalData")]
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
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync(ct);
        if (companyId == null)
            return Challenge();

        var dtos = await _farmService.ListByCompanyAsync(companyId.Value, search, ct);

        var managerIds = dtos
            .Where(d => d.ManagerUserId.HasValue)
            .Select(d => d.ManagerUserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> managerNames = new();
        if (managerIds.Count > 0)
        {
            foreach (var mid in managerIds)
            {
                var user = await _userManager.FindByIdAsync(mid.ToString());
                if (user != null)
                    managerNames[mid] = user.FullName;
            }
        }

        var vm = dtos.Select(d => new FarmListViewModel
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            City = d.City,
            Currency = d.Currency,
            IsActive = d.IsActive,
            ManagerInfo = d.ManagerUserId.HasValue && managerNames.ContainsKey(d.ManagerUserId.Value)
                ? managerNames[d.ManagerUserId.Value]
                : null
        }).ToList();

        ViewData["Search"] = search;
        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
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
    [Authorize(Policy = "CanManageCompany")]
    public IActionResult Create()
    {
        var vm = new FarmCreateEditViewModel();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageCompany")]
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
    [Authorize(Policy = "CanManageCompany")]
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
    [Authorize(Policy = "CanManageCompany")]
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
    [Authorize(Policy = "CanManageCompany")]
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

    private async Task<Guid?> GetCompanyIdAsync(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId;
    }
}
