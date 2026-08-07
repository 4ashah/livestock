using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.SettingsViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Roles = RoleNames.CompanyAdministrator + "," + RoleNames.SystemAdministrator)]
public class SettingsController : Controller
{
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public SettingsController(
        IAppDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _configuration = configuration;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    public async Task<IActionResult> Index(Guid? companyId, CancellationToken ct = default)
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
            return NotFound();
        }

        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId, ct);

        if (company == null)
        {
            return NotFound();
        }

        if (!isSystemAdmin && company.Id != userCompanyId)
        {
            return Forbid();
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

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
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
        return RedirectToAction(nameof(Index), new { companyId = vm.Id });
    }
}
