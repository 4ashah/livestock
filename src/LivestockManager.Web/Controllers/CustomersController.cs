using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.DTOs.Customers;
using LivestockManager.Application.Services.Customers;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private bool CanEdit => User.IsInRole("Administrator") || User.IsInRole("Manager");

    public async Task<IActionResult> Index(string searchString, CancellationToken ct)
    {
        ViewData["CurrentFilter"] = searchString;
        var companyId = await GetCompanyIdAsync();
        IList<CustomerSummaryDto> customers;

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            customers = await _customerService.SearchAsync(companyId, searchString, ct);
        }
        else
        {
            customers = await _customerService.ListAsync(companyId, ct);
        }

        ViewData["CanEdit"] = CanEdit;
        return View(customers);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var customer = await _customerService.GetByIdAsync(id, ct);
        ViewData["CanEdit"] = CanEdit;
        return View(customer);
    }

    [Authorize(Roles = "Administrator,Manager")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Create(CustomerCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        dto.CompanyId = await GetCompanyIdAsync();
        var result = await _customerService.CreateAsync(dto, ct);
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var c = await _customerService.GetByIdAsync(id, ct);
        var dto = new CustomerUpdateDto
        {
            Name = c.Name,
            CustomerCode = c.CustomerCode,
            IsBusiness = c.IsBusiness,
            TaxNumber = c.TaxNumber,
            BillingAddress = c.BillingAddress,
            DeliveryAddress = c.DeliveryAddress,
            Phone = c.Phone,
            Email = c.Email,
            CreditLimit = c.CreditLimit,
            PaymentTermsDays = c.PaymentTermsDays,
            Notes = c.Notes,
            IsActive = c.IsActive
        };
        ViewData["CustomerId"] = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Edit(Guid id, CustomerUpdateDto dto, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["CustomerId"] = id;
            return View(dto);
        }
        await _customerService.UpdateAsync(id, dto, ct);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var c = await _customerService.GetByIdAsync(id, ct);
        var dto = new CustomerUpdateDto
        {
            Name = c.Name,
            CustomerCode = c.CustomerCode,
            IsBusiness = c.IsBusiness,
            TaxNumber = c.TaxNumber,
            BillingAddress = c.BillingAddress,
            DeliveryAddress = c.DeliveryAddress,
            Phone = c.Phone,
            Email = c.Email,
            CreditLimit = c.CreditLimit,
            PaymentTermsDays = c.PaymentTermsDays,
            Notes = c.Notes,
            IsActive = !c.IsActive
        };
        await _customerService.UpdateAsync(id, dto, ct);
        return RedirectToAction(nameof(Details), new { id });
    }
}
