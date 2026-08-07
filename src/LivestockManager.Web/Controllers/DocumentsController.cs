using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Web.Models.DocumentViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanViewOperationalData")]
public class DocumentsController : Controller
{
    private readonly IProtectedDocumentStorage _storage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public DocumentsController(
        IProtectedDocumentStorage storage,
        UserManager<ApplicationUser> userManager,
        AppDbContext db)
    {
        _storage = storage;
        _userManager = userManager;
        _db = db;
    }

    private async Task<(Guid companyId, Guid? userId)> GetCurrentCompanyAndUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            throw new DomainException("Current user not found.");

        var userId = user.Id;
        var companyId = user.CompanyId;

        if (companyId == null || companyId == Guid.Empty)
            throw new DomainException("User is not associated with a company.");

        return (companyId.Value, userId);
    }

    private static string FormatSizeBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    [HttpGet]
    [Authorize(Policy = "CanViewFinancialData")]
    public IActionResult Upload(string entityType, Guid? entityId)
    {
        var vm = new UploadDocumentViewModel
        {
            EntityTypeStr = entityType,
            EntityIdStr = entityId.HasValue ? entityId.Value.ToString() : null
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = "CanViewFinancialData")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadDocumentViewModel vm, CancellationToken ct)
    {
        if (vm.Files == null || vm.Files.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Please select at least one file to upload.");
            return View(vm);
        }

        var (companyId, userId) = await GetCurrentCompanyAndUser();

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".csv" };
        const long maxSizeBytes = 10 * 1024 * 1024;

        var successCount = 0;
        var errorMessages = new List<string>();

        Guid? parsedEntityId = null;
        if (!string.IsNullOrWhiteSpace(vm.EntityIdStr) && Guid.TryParse(vm.EntityIdStr, out var eid))
            parsedEntityId = eid;

        foreach (var file in vm.Files)
        {
            if (file == null || file.Length == 0)
                continue;

            try
            {
                using var stream = file.OpenReadStream();
                var (documentId, _, _) = await _storage.StoreAsync(
                    companyId,
                    file.FileName,
                    stream,
                    vm.DocumentType,
                    maxSizeBytes,
                    allowedExtensions,
                    userId,
                    ct);

                if (!string.IsNullOrWhiteSpace(vm.EntityTypeStr) || parsedEntityId.HasValue)
                {
                    var doc = await _db.Documents.FindAsync(new object[] { documentId }, ct);
                    if (doc != null)
                    {
                        doc.EntityType = vm.EntityTypeStr;
                        doc.EntityId = parsedEntityId;
                        await _db.SaveChangesAsync(ct);
                    }
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errorMessages.Add($"{file.FileName}: {ex.Message}");
            }
        }

        if (successCount > 0)
            TempData["Success"] = $"Successfully uploaded {successCount} file(s).";

        if (errorMessages.Count > 0)
            TempData["Error"] = string.Join(" | ", errorMessages);

        return RedirectToAction(nameof(List), new { entityType = vm.EntityTypeStr, entityId = parsedEntityId });
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> List(string entityType, Guid? entityId, CancellationToken ct)
    {
        var (companyId, _) = await GetCurrentCompanyAndUser();

        var query = _db.Documents
            .Where(d => d.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType) && entityId.HasValue)
        {
            query = query.Where(d => d.EntityType == entityType && d.EntityId == entityId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(d => d.EntityType == entityType);
        }

        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentListItemViewModel
            {
                Id = d.Id,
                DisplayName = d.DisplayName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                Extension = d.Extension,
                DocumentType = d.DocumentType,
                EntityType = d.EntityType,
                EntityId = d.EntityId,
                UploadedAt = d.UploadedAt,
                UploadedByUserId = d.UploadedByUserId
            })
            .ToListAsync(ct);

        foreach (var item in items)
            item.SizeFriendly = FormatSizeBytes(item.SizeBytes);

        var canUpload = User.IsInRole(RoleNames.CompanyAdministrator)
                        || User.IsInRole(RoleNames.Accounts)
                        || User.IsInRole(RoleNames.FarmManager)
                        || User.IsInRole(RoleNames.SystemAdministrator);

        var canDelete = User.IsInRole(RoleNames.CompanyAdministrator)
                        || User.IsInRole(RoleNames.SystemAdministrator);

        var vm = new DocumentListViewModel
        {
            Items = items,
            EntityType = entityType,
            EntityId = entityId,
            CanUpload = canUpload,
            CanDelete = canDelete
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = "CanViewOperationalData")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (companyId, _) = await GetCurrentCompanyAndUser();

        try
        {
            var (fileStream, metadata) = await _storage.DownloadAsync(id, companyId, ct);

            var sanitizedName = string.IsNullOrWhiteSpace(metadata.DisplayName)
                ? $"document_{id:N}"
                : metadata.DisplayName;

            return File(fileStream, metadata.ContentType ?? "application/octet-stream", fileDownloadName: sanitizedName);
        }
        catch (DomainException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [Authorize(Policy = "CanManageCompany")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl, CancellationToken ct)
    {
        var (companyId, _) = await GetCurrentCompanyAndUser();

        var doc = await _db.Documents.FirstOrDefaultAsync(
            d => d.Id == id && d.CompanyId == companyId, ct);

        if (doc == null)
            return NotFound();

        doc.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Document deleted (retained for audit).";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(List));
    }
}
