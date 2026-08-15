using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.DTOs.Suppliers;
using LivestockManager.Application.Services.Suppliers;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.SupplierViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageSuppliers)]
public class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SuppliersController(
        ISupplierService supplierService,
        UserManager<ApplicationUser> userManager)
    {
        _supplierService = supplierService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private bool CanEdit => User.IsInRole(RoleNames.Accounts)
        || User.IsInRole(RoleNames.FarmManager)
        || User.IsInRole(RoleNames.OperationsManager)
        || User.IsInRole(RoleNames.CompanyAdministrator)
        || User.IsInRole(RoleNames.SystemAdministrator);

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Index([FromQuery] string search, [FromQuery] bool? onlyActive, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        IList<SupplierSummaryDto> suppliers;

        if (!string.IsNullOrWhiteSpace(search) || onlyActive.HasValue)
        {
            suppliers = await _supplierService.SearchAsync(companyId, search ?? string.Empty, onlyActive, ct);
        }
        else
        {
            suppliers = await _supplierService.ListAsync(companyId, ct);
        }

        var viewModel = new SupplierListViewModel
        {
            Items = suppliers,
            SearchTerm = search,
            OnlyActive = onlyActive
        };

        ViewData["CanEdit"] = CanEdit;
        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        SupplierDetailDto supplier;
        try
        {
            supplier = await _supplierService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var vm = new SupplierDetailsViewModel
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            Code = supplier.Code,
            Name = supplier.Name,
            LegalName = supplier.LegalName,
            TaxNumber = supplier.TaxNumber,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            BankAccount = supplier.BankAccount,
            PaymentTermsDays = supplier.PaymentTermsDays,
            Currency = supplier.Currency,
            IsActive = supplier.IsActive,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            ModifiedAt = supplier.ModifiedAt,
            PurchaseHistory = new List<object>()
        };

        ViewData["CanEdit"] = CanEdit;
        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public IActionResult Create()
    {
        var vm = new SupplierCreateEditViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Create(SupplierCreateEditViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var companyId = await GetCompanyIdAsync();
        var dto = new SupplierCreateDto
        {
            CompanyId = companyId,
            Code = vm.Code,
            Name = vm.Name,
            LegalName = vm.LegalName,
            TaxNumber = vm.TaxNumber,
            Address = vm.Address,
            Phone = vm.Phone,
            Email = vm.Email,
            BankAccount = vm.BankAccount,
            PaymentTermsDays = vm.PaymentTermsDays,
            Currency = vm.Currency,
            IsActive = vm.IsActive,
            Notes = vm.Notes
        };

        try
        {
            var result = await _supplierService.CreateAsync(dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id = result.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        SupplierDetailDto s;
        try
        {
            s = await _supplierService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var vm = new SupplierCreateEditViewModel
        {
            Id = id,
            Code = s.Code,
            Name = s.Name,
            LegalName = s.LegalName,
            TaxNumber = s.TaxNumber,
            Address = s.Address,
            Phone = s.Phone,
            Email = s.Email,
            BankAccount = s.BankAccount,
            PaymentTermsDays = s.PaymentTermsDays,
            Currency = s.Currency,
            IsActive = s.IsActive,
            Notes = s.Notes
        };
        ViewData["SupplierId"] = id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Edit(Guid id, SupplierCreateEditViewModel vm, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["SupplierId"] = id;
            return View(vm);
        }

        var companyId = await GetCompanyIdAsync();
        try
        {
            _ = await _supplierService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        var dto = new SupplierUpdateDto
        {
            Id = id,
            CompanyId = companyId,
            Code = vm.Code,
            Name = vm.Name,
            LegalName = vm.LegalName,
            TaxNumber = vm.TaxNumber,
            Address = vm.Address,
            Phone = vm.Phone,
            Email = vm.Email,
            BankAccount = vm.BankAccount,
            PaymentTermsDays = vm.PaymentTermsDays,
            Currency = vm.Currency,
            IsActive = vm.IsActive,
            Notes = vm.Notes
        };

        try
        {
            await _supplierService.UpdateAsync(id, dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["SupplierId"] = id;
            return View(vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.CanManageSuppliers)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();

        var companyId = await GetCompanyIdAsync();
        try
        {
            _ = await _supplierService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        try
        {
            await _supplierService.ArchiveAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult MobileIndex()
    {
        ViewData["DockKey"] = "suppliers";
        return RedirectToAction(nameof(Index));
    }
}
