using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LivestockManager.Application.Common;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Web.Models.UserViewModels;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanManageCompany")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAppDbContext _db;
    private const string DefaultResetPassword = "Dev@123456";

    public UserManagementController(UserManager<ApplicationUser> um, RoleManager<ApplicationRole> rm, IAppDbContext db)
    {
        _userManager = um;
        _roleManager = rm;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);
        var coId = current?.CompanyId;
        IQueryable<ApplicationUser> query = _userManager.Users;
        if (!isSys && coId.HasValue)
            query = query.Where(u => u.CompanyId == coId.Value);
        var users = await query.OrderBy(u => u.FullName).ToListAsync(ct);
        var rows = new List<UserRowViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            rows.Add(new UserRowViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email,
                FullName = u.FullName ?? string.Empty,
                IsEnabled = u.IsEnabled,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                Roles = roles.OrderBy(x => x).ToList()
            });
        }
        return View(new UserManagementListViewModel
        {
            Items = rows,
            CurrentUserCanResetPasswords = true,
            CurrentUserIsSystemAdmin = isSys
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();
        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);
        if (!isSys && user.CompanyId != current?.CompanyId)
            return Forbid();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var res = await _userManager.ResetPasswordAsync(user, token, DefaultResetPassword);
        var vm = await BuildVm(ct);
        if (!res.Succeeded)
            vm.ErrorMessage = string.Join(" | ", res.Errors.Select(e => e.Description));
        else
            vm.SuccessMessage = $"Password reset for {user.UserName} (new: {DefaultResetPassword}).";
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEnabled(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();
        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);
        if (!isSys && user.CompanyId != current?.CompanyId)
            return Forbid();
        if (user.Id == current?.Id)
        {
            var vm = await BuildVm(ct);
            vm.ErrorMessage = "You cannot enable/disable your own account.";
            return View("Index", vm);
        }
        user.IsEnabled = !user.IsEnabled;
        var res = await _userManager.UpdateAsync(user);
        var vm2 = await BuildVm(ct);
        if (res.Succeeded)
            vm2.SuccessMessage = user.IsEnabled ? $"{user.UserName} enabled." : $"{user.UserName} disabled.";
        else
            vm2.ErrorMessage = string.Join(" | ", res.Errors.Select(e => e.Description));
        return View("Index", vm2);
    }

    [HttpGet]
    public IActionResult AddUser()
    {
        var vm = new UserAddViewModel();
        PopulateRoleOptions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(UserAddViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            PopulateRoleOptions(vm);
            return View(vm);
        }

        var current = await _userManager.GetUserAsync(User);

        var validRoles = new[] { RoleNames.Viewer, RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator };
        if (!validRoles.Contains(vm.SelectedRole))
        {
            ModelState.AddModelError(string.Empty, "Invalid role selected.");
            PopulateRoleOptions(vm);
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = vm.UserName,
            Email = vm.Email,
            FullName = vm.FullName,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            IsEnabled = true,
            CompanyId = current?.CompanyId ?? throw new InvalidOperationException("No company context"),
            CreatedAt = DateTimeOffset.Now
        };

        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var createRes = await _userManager.CreateAsync(user, vm.TemporaryPassword);
            if (!createRes.Succeeded)
            {
                foreach (var err in createRes.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                PopulateRoleOptions(vm);
                return View(vm);
            }

            var addRoleRes = await _userManager.AddToRoleAsync(user, vm.SelectedRole);
            if (!addRoleRes.Succeeded)
            {
                await tx.RollbackAsync(ct);
                foreach (var err in addRoleRes.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                PopulateRoleOptions(vm);
                return View(vm);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var indexVm = await BuildVm(ct);
        indexVm.SuccessMessage = $"User {vm.UserName} created. Temporary password: {vm.TemporaryPassword}. The user must reset password after first login.";
        return View("Index", indexVm);
    }

    private static void PopulateRoleOptions(UserAddViewModel vm)
    {
        vm.RoleOptions = new List<SelectListItem>
        {
            new() { Text = RoleNames.Viewer, Value = RoleNames.Viewer },
            new() { Text = RoleNames.DataEntry, Value = RoleNames.DataEntry },
            new() { Text = RoleNames.FarmManager, Value = RoleNames.FarmManager },
            new() { Text = RoleNames.Accounts, Value = RoleNames.Accounts },
            new() { Text = RoleNames.CompanyAdministrator, Value = RoleNames.CompanyAdministrator },
            new() { Text = RoleNames.SystemAdministrator, Value = RoleNames.SystemAdministrator }
        };
    }

    private async Task<UserManagementListViewModel> BuildVm(CancellationToken ct)
    {
        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);
        var coId = current?.CompanyId;
        IQueryable<ApplicationUser> q = _userManager.Users;
        if (!isSys && coId.HasValue)
            q = q.Where(u => u.CompanyId == coId.Value);
        var users = await q.OrderBy(u => u.FullName).ToListAsync(ct);
        var rows = new List<UserRowViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            rows.Add(new UserRowViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email,
                FullName = u.FullName ?? string.Empty,
                IsEnabled = u.IsEnabled,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                Roles = roles.OrderBy(x => x).ToList()
            });
        }
        return new UserManagementListViewModel
        {
            Items = rows,
            CurrentUserCanResetPasswords = true,
            CurrentUserIsSystemAdmin = isSys
        };
    }
}
