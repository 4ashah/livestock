using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.SettingsViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Administration.Settings)]
public class SettingsController : Controller
{
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SettingsController(
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Settings)]
    public async Task<IActionResult> Index(Guid? companyId, CancellationToken ct = default)
    {
        var result = await LoadSettingsAsync(companyId, ct);
        return result.Failure ?? View(result.Vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Settings)]
    public async Task<IActionResult> MobileIndex(Guid? companyId, CancellationToken ct = default)
    {
        var result = await LoadSettingsAsync(companyId, ct);
        return result.Failure ?? View("MobileIndex", result.Vm);
    }

    private sealed record SettingsLoadResult(SettingsViewModel? Vm, IActionResult? Failure);

    private async Task<SettingsLoadResult> LoadSettingsAsync(Guid? companyId, CancellationToken ct)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var userCompanyId = currentUser?.CompanyId ?? Guid.Empty;
        var isSystemAdmin = User.IsInRole(RoleNames.SystemAdministrator);

        Guid targetCompanyId;
        if (isSystemAdmin && companyId.HasValue && companyId.Value != Guid.Empty)
        {
            targetCompanyId = companyId.Value;
        }
        else
        {
            targetCompanyId = userCompanyId;
        }

        if (targetCompanyId == Guid.Empty)
        {
            return new SettingsLoadResult(null, NotFound());
        }

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId, ct);

        if (company == null)
        {
            return new SettingsLoadResult(null, NotFound());
        }

        if (!isSystemAdmin && company.Id != userCompanyId)
        {
            return new SettingsLoadResult(null, Forbid());
        }

        var vm = new SettingsViewModel
        {
            Id = company.Id,
            CompanyName = company.Name,
            RegistrationNumber = company.RegistrationNumber,
            TaxNumber = company.TaxNumber,
            Phone = company.Phone,
            Email = company.Email,
            InvoicePrefix = company.InvoicePrefix,
            ReceiptPrefix = company.ReceiptPrefix,
            TaxRate = company.TaxRate,
            FinancialYearStartMonth = company.FinancialYearStartMonth,
            Currency = company.Currency,
            WeightUnit = company.WeightUnit,
            IsActive = company.IsActive
        };

        return new SettingsLoadResult(vm, null);
    }

    [HttpPost]
    [Authorize(Policy = PermissionNames.Administration.Settings)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel vm, CancellationToken ct = default)
    {
        // The mobile settings form posts here with a hidden "mobile" marker so that
        // validation failures and the post-save redirect stay on the mobile page.
        var isMobile = Request.Form.ContainsKey("mobile");

        if (!ModelState.IsValid)
        {
            return isMobile ? View("MobileIndex", vm) : View(vm);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var userCompanyId = currentUser?.CompanyId ?? Guid.Empty;
        var isSystemAdmin = User.IsInRole(RoleNames.SystemAdministrator);

        if (!isSystemAdmin && vm.Id != userCompanyId)
        {
            return Forbid();
        }

        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == vm.Id, ct);

        if (company == null)
        {
            return NotFound();
        }

        company.Name = vm.CompanyName;
        company.RegistrationNumber = vm.RegistrationNumber;
        company.TaxNumber = vm.TaxNumber;
        company.Phone = vm.Phone;
        company.Email = vm.Email;
        company.InvoicePrefix = vm.InvoicePrefix;
        company.ReceiptPrefix = vm.ReceiptPrefix;
        company.TaxRate = vm.TaxRate;
        company.FinancialYearStartMonth = vm.FinancialYearStartMonth;
        company.Currency = vm.Currency;
        company.WeightUnit = vm.WeightUnit;
        company.IsActive = vm.IsActive;

        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = "Settings saved successfully.";
        return RedirectToAction(isMobile ? nameof(MobileIndex) : nameof(Index), new { companyId = vm.Id });
    }
}
