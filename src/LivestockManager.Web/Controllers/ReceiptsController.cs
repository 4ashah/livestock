using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Application.Services.Receipts;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.ReceiptViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Receipts.View)]
public class ReceiptsController : Controller
{
    private readonly IReceiptService _receiptService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReceiptsController(
        IReceiptService receiptService,
        UserManager<ApplicationUser> userManager)
    {
        _receiptService = receiptService;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    private async Task<(Guid companyId, Guid? userId)> GetCurrentCompanyAndUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            throw new DomainException("Current user not found.");

        var userId = user.Id;
        var companyId = user.CompanyId;

        if (companyId == null || companyId == Guid.Empty)
            throw new DomainException("User is not associated with a company.");

        return (companyId.Value, userId);
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Receipts.View)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var (companyId, _) = await GetCurrentCompanyAndUser();
        var list = await _receiptService.ListAsync(companyId, ct);
        return View(new ReceiptListViewModel { Items = list });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Receipts.GenerateFromPayment)]
    public async Task<IActionResult> GenerateForPayment(Guid paymentId, string? notes, CancellationToken ct)
    {
        if (paymentId == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        try
        {
            var receipt = await _receiptService.GenerateForPaymentAsync(paymentId, notes, companyId, ct);
            return RedirectToAction(nameof(DownloadPdf), new { id = receipt.Id });
        }
        catch (DomainException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Receipts.Download)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) return NotFound();
        var companyId = await GetCompanyIdAsync();
        byte[] bytes;
        try
        {
            bytes = await _receiptService.GetPdfAsync(id, companyId, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }
        return File(bytes, "application/pdf", $"Receipt_{id:N}.pdf");
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Receipts.View)]
    public async Task<IActionResult> MobileIndex(CancellationToken ct)
    {
        var (companyId, _) = await GetCurrentCompanyAndUser();
        var list = await _receiptService.ListAsync(companyId, ct);
        return View("MobileIndex", new ReceiptListViewModel { Items = list });
    }
}
