using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Application.DTOs.Expenses;
using LivestockManager.Application.Services.Expenses;
using LivestockManager.Application.Services.Farms;
using LivestockManager.Application.Services.Livestock;
using LivestockManager.Application.Services.Suppliers;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewFinancialData")]
public class ExpensesController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly IFarmService _farmService;
    private readonly ISupplierService _supplierService;
    private readonly ILivestockService _livestockService;
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExpensesController(
        IExpenseService expenseService,
        IFarmService farmService,
        ISupplierService supplierService,
        ILivestockService livestockService,
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _farmService = farmService;
        _supplierService = supplierService;
        _livestockService = livestockService;
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
    public async Task<IActionResult> Index(
        DateTime? from,
        DateTime? to,
        Guid? farmId,
        Guid? supplierId,
        Guid? livestockId,
        ExpenseCategory? category,
        string? format,
        CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        DateTimeOffset? fromDto = from.AsUtcDayStartOrDefault();
        DateTimeOffset? toDto = to.AsUtcDayEndOrDefault();

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _expenseService.ExportCsvAsync(companyId, farmId, supplierId, livestockId, category, fromDto, toDto, ct);
            var fileName = $"Expenses_{(from ?? DateTime.Today.AddMonths(-1)).ToString("yyyyMMdd")}_{(to ?? DateTime.Today).ToString("yyyyMMdd")}.csv";
            return File(bytes, "text/csv", fileName);
        }

        var list = await _expenseService.ListAsync(companyId, farmId, supplierId, livestockId, category, fromDto, toDto, ct);
        var categorySummary = await _expenseService.CategorySummaryAsync(companyId, fromDto, toDto, ct);

        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["SupplierId"] = supplierId;
        ViewData["LivestockId"] = livestockId;
        ViewData["Category"] = category;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        ViewData["CategorySummary"] = categorySummary;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> MobileIndex(
        DateTime? from,
        DateTime? to,
        Guid? farmId,
        Guid? supplierId,
        Guid? livestockId,
        ExpenseCategory? category,
        CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        var list = await _expenseService.ListAsync(
            companyId, farmId, supplierId, livestockId, category,
            from.AsUtcDayStartOrDefault(), to.AsUtcDayEndOrDefault(), ct);
        var categorySummary = await _expenseService.CategorySummaryAsync(
            companyId, from.AsUtcDayStartOrDefault(), to.AsUtcDayEndOrDefault(), ct);
        ViewData["From"] = from?.ToString("yyyy-MM-dd");
        ViewData["To"] = to?.ToString("yyyy-MM-dd");
        ViewData["FarmId"] = farmId;
        ViewData["Category"] = category;
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["CanEdit"] = CanEdit;
        ViewData["CategorySummary"] = categorySummary;
        return View("MobileIndex", list);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> MobileCreate(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        await PopulateSelectListsAsync(companyId, ct);
        var dto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            ExpenseDate = DateTimeOffset.Now,
            Category = ExpenseCategory.Feed,
            Currency = Currency.USD,
            PaymentMethod = PaymentMethod.Cash,
            TaxRate = 0
        };
        return View("MobileCreate", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> MobileCreate(ExpenseCreateDto dto, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        try
        {
            var created = await _expenseService.CreateAsync(dto, companyId, ct);
            return RedirectToAction(nameof(MobileIndex));
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        await PopulateSelectListsAsync(companyId, ct);
        return View("MobileCreate", dto);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> MobileEdit(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        ExpenseDetailDto expense;
        try { expense = await _expenseService.GetByIdAsync(id, companyId, ct); }
        catch (DomainException) { return NotFound(); }
        await PopulateSelectListsAsync(companyId, ct);
        var dto = new ExpenseUpdateDto
        {
            FarmId = expense.FarmId,
            SupplierId = expense.SupplierId,
            LivestockId = expense.LivestockId,
            Category = expense.Category,
            ExpenseDate = expense.ExpenseDate,
            Currency = expense.Currency,
            Amount = expense.Amount,
            TaxRate = expense.TaxRate,
            PaymentMethod = expense.PaymentMethod,
            Description = expense.Description,
            Reference = expense.Reference,
            DocumentId = expense.DocumentId,
            Notes = expense.Notes
        };
        return View("MobileEdit", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> MobileEdit(Guid id, ExpenseUpdateDto dto, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            await _expenseService.UpdateAsync(id, dto, companyId, ct);
            return RedirectToAction(nameof(MobileIndex));
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        await PopulateSelectListsAsync(companyId, ct);
        return View("MobileEdit", dto);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        ExpenseDetailDto expense;
        try
        {
            expense = await _expenseService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        if (expense.FarmId.HasValue)
        {
            var f = await _farmService.GetByIdAsync(expense.FarmId.Value, companyId, ct);
            ViewData["FarmName"] = f?.Name;
        }
        if (expense.SupplierId.HasValue)
        {
            var s = await _supplierService.GetByIdAsync(expense.SupplierId.Value, companyId, ct);
            ViewData["SupplierName"] = s?.Name;
        }
        if (expense.LivestockId.HasValue)
        {
            var l = await _livestockService.GetByIdAsync(expense.LivestockId.Value, companyId, ct);
            ViewData["LivestockCode"] = l?.LivestockId;
        }
        ViewData["CanEdit"] = CanEdit;
        return View(expense);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        await PopulateSelectListsAsync(companyId, ct);
        var dto = new ExpenseCreateDto
        {
            CompanyId = companyId,
            ExpenseDate = DateTimeOffset.Now,
            Category = ExpenseCategory.Feed,
            Currency = Currency.USD,
            PaymentMethod = PaymentMethod.Cash,
            TaxRate = 0
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> Create(ExpenseCreateDto dto, CancellationToken ct)
    {
        var companyId = await GetCompanyIdAsync();
        dto.CompanyId = companyId;
        try
        {
            var created = await _expenseService.CreateAsync(dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        await PopulateSelectListsAsync(companyId, ct);
        return View(dto);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        ExpenseDetailDto expense;
        try
        {
            expense = await _expenseService.GetByIdAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        await PopulateSelectListsAsync(companyId, ct);
        var dto = new ExpenseUpdateDto
        {
            FarmId = expense.FarmId,
            SupplierId = expense.SupplierId,
            LivestockId = expense.LivestockId,
            Category = expense.Category,
            ExpenseDate = expense.ExpenseDate,
            Currency = expense.Currency,
            Amount = expense.Amount,
            TaxRate = expense.TaxRate,
            PaymentMethod = expense.PaymentMethod,
            Description = expense.Description,
            Reference = expense.Reference,
            DocumentId = expense.DocumentId,
            Notes = expense.Notes
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> Edit(Guid id, ExpenseUpdateDto dto, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            var updated = await _expenseService.UpdateAsync(id, dto, companyId, ct);
            return RedirectToAction(nameof(Details), new { id = updated.Id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        await PopulateSelectListsAsync(companyId, ct);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanManageExpenses")]
    public async Task<IActionResult> Delete(Guid id, string reason, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        if (string.IsNullOrWhiteSpace(reason))
            ModelState.AddModelError(nameof(reason), "A deletion reason is required.");

        var companyId = await GetCompanyIdAsync();
        try
        {
            await _expenseService.DeleteAsync(id, reason ?? "User deleted expense", companyId, ct);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task PopulateSelectListsAsync(Guid companyId, CancellationToken ct)
    {
        ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
        ViewData["Suppliers"] = await _supplierService.ListAsync(companyId, ct);
        ViewData["ActiveLivestock"] = await _db.Livestock
            .Where(l => l.CompanyId == companyId && l.Status == LivestockStatus.Active)
            .OrderBy(l => l.LivestockId)
            .Select(l => new
            {
                l.Id,
                l.LivestockId,
                l.LivestockTypeId,
                Display = l.LivestockId + " (" + l.LivestockTypeId + ")"
            })
            .ToListAsync(ct);
    }
}
