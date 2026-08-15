using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Exceptions;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.AuditViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PolicyNames.CanViewAuditLogs)]
public class AuditController : Controller
{
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditController(
        IAppDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.CompanyId ?? Guid.Empty;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewAuditLogs)]
    public async Task<IActionResult> Index(
        string? entityType,
        string? entityId,
        string? actionType,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 500) pageSize = 500;

        var companyId = await GetCompanyIdAsync();
        var isSystemAdmin = User.IsInRole(RoleNames.SystemAdministrator);

        var query = _db.AuditLogs.AsNoTracking();

        if (!isSystemAdmin)
        {
            query = query.Where(a => a.CompanyId == companyId);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType != null && a.EntityType.Contains(entityType));
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(a => a.EntityId != null && a.EntityId.Contains(entityId));
        }

        if (!string.IsNullOrWhiteSpace(actionType) && actionType != "All")
        {
            query = query.Where(a => a.Action == actionType);
        }

        if (from.HasValue)
        {
            var fromOffset = from.Value.AsUtcDayStart();
            query = query.Where(a => a.CreatedAt >= fromOffset);
        }

        if (to.HasValue)
        {
            var toOffset = to.Value.AsUtcDayEnd();
            query = query.Where(a => a.CreatedAt <= toOffset);
        }

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditListItemViewModel
            {
                Id = a.Id,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                CreatedAt = a.CreatedAt,
                UserId = a.UserId
            })
            .ToListAsync(ct);

        var userIds = items.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email, ct);

            foreach (var item in items)
            {
                if (item.UserId.HasValue && users.TryGetValue(item.UserId.Value, out var email))
                {
                    item.UserEmail = email;
                }
            }
        }

        var vm = new AuditListViewModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            EntityTypeFilter = entityType,
            EntityIdFilter = entityId,
            ActionFilter = actionType,
            From = from,
            To = to
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanViewAuditLogs)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return NotFound();

        var companyId = await GetCompanyIdAsync();
        var isSystemAdmin = User.IsInRole(RoleNames.SystemAdministrator);

        AuditLog? audit;
        try
        {
            audit = await _db.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        if (audit == null) return NotFound();

        if (!isSystemAdmin && audit.CompanyId != companyId)
        {
            return NotFound();
        }

        var vm = new AuditDetailsViewModel
        {
            Id = audit.Id,
            EntityType = audit.EntityType,
            EntityId = audit.EntityId,
            Action = audit.Action,
            CreatedAt = audit.CreatedAt,
            OldValuesJson = audit.OldValuesJson,
            NewValuesJson = audit.NewValuesJson
        };

        if (audit.UserId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(audit.UserId.Value.ToString());
            if (user != null)
            {
                vm.UserName = user.FullName;
                vm.UserEmail = user.Email;
            }
        }

        return View(vm);
    }
}
