using System.Security.Claims;
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

[Authorize(Policy = PermissionNames.Administration.Users)]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppDbContext _db;
    private const string DefaultResetPassword = "Dev@123456";

    public UserManagementController(UserManager<ApplicationUser> um, IAppDbContext db)
    {
        _userManager = um;
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Users)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);
        var coId = current?.CompanyId;
        IQueryable<ApplicationUser> query = _userManager.Users;
        if (!isSys && coId.HasValue)
            query = query.Where(u => u.CompanyId == coId.Value);
        var users = await query.OrderBy(u => u.FullName).ToListAsync(ct);
        List<UserRowViewModel> rows = [];
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
                Roles = roles.OrderBy(x => x).Select(RoleNames.GetDisplayName).ToList(),
                IsSystemAdmin = roles.Contains(RoleNames.SystemAdministrator),
                CompanyId = u.CompanyId
            });
        }
        return View(new UserManagementListViewModel
        {
            Items = rows,
            CurrentUserCanResetPasswords = true,
            CurrentUserIsSystemAdmin = isSys,
            CurrentUserCompanyId = coId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Administration.Users)]
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
    [Authorize(Policy = PermissionNames.Administration.Users)]
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
    [Authorize(Policy = PermissionNames.Users.AssignRole)]
    public IActionResult AddUser()
    {
        var vm = new UserAddViewModel();
        PopulateRoleOptions(vm, User);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Users.AssignRole)]
    public async Task<IActionResult> AddUser(UserAddViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            PopulateRoleOptions(vm, User);
            return View(vm);
        }

        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);

        if (!isSys)
        {
            if (vm.SelectedRole == RoleNames.SystemAdministrator)
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Company Administrators cannot assign System Administrator role.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }

            if (!RoleNames.CompanySafeAssignable.Contains(vm.SelectedRole))
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Invalid role for company-level assignment.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }
        }
        else
        {
            if (!RoleNames.All.Contains(vm.SelectedRole))
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Invalid role selected.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }
        }

        if (current == null)
            return Forbid();

        var targetCompanyId = isSys ? vm.TargetCompanyId : current.CompanyId;

        if (!isSys && targetCompanyId != current.CompanyId)
        {
            ModelState.AddModelError(string.Empty, "Company Administrators cannot create users for another company.");
            PopulateRoleOptions(vm, User);
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
            CompanyId = targetCompanyId ?? throw new InvalidOperationException("No company context"),
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
                PopulateRoleOptions(vm, User);
                return View(vm);
            }

            var addRoleRes = await _userManager.AddToRoleAsync(user, vm.SelectedRole);
            if (!addRoleRes.Succeeded)
            {
                await tx.RollbackAsync(ct);
                foreach (var err in addRoleRes.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                PopulateRoleOptions(vm, User);
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

    [HttpGet]
    [Authorize(Policy = PermissionNames.Users.AssignRole)]
    public async Task<IActionResult> EditUser(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);

        if (!isSys && user.CompanyId != current?.CompanyId)
            return Forbid();

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault(r => r != RoleNames.SystemAdministrator) ?? roles.FirstOrDefault() ?? RoleNames.DataEntry;

        var vm = new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            FullName = user.FullName ?? string.Empty,
            CurrentRole = currentRole,
            SelectedRole = currentRole,
            IsEnabled = user.IsEnabled,
            IsSystemAdmin = roles.Contains(RoleNames.SystemAdministrator),
            CompanyId = user.CompanyId
        };

        PopulateRoleOptions(vm, User);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Users.AssignRole)]
    public async Task<IActionResult> EditUser(UserEditViewModel vm, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(vm.Id.ToString());
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            PopulateRoleOptions(vm, User);
            return View(vm);
        }

        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);

        if (!isSys && user.CompanyId != current?.CompanyId)
            return Forbid();

        if (!isSys)
        {
            if (vm.SelectedRole == RoleNames.SystemAdministrator)
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Company Administrators cannot assign System Administrator role.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }

            if (!RoleNames.CompanySafeAssignable.Contains(vm.SelectedRole))
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Invalid role for company-level assignment.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }
        }
        else
        {
            if (!RoleNames.All.Contains(vm.SelectedRole))
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "Invalid role selected.");
                PopulateRoleOptions(vm, User);
                return View(vm);
            }
        }

        var targetCompanyId = isSys ? vm.CompanyId : current?.CompanyId;

        if (!isSys && targetCompanyId != current?.CompanyId)
        {
            ModelState.AddModelError(string.Empty, "Company Administrators cannot move users to another company.");
            PopulateRoleOptions(vm, User);
            return View(vm);
        }

        user.FullName = vm.FullName;
        user.IsEnabled = vm.IsEnabled;
        if (isSys)
            user.CompanyId = targetCompanyId;

        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
            {
                foreach (var err in updateRes.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                PopulateRoleOptions(vm, User);
                return View(vm);
            }

            var existingRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = existingRoles.Where(r =>
                isSys ? r != vm.SelectedRole : (r != vm.SelectedRole && r != RoleNames.SystemAdministrator)).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeRes = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    foreach (var err in removeRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    PopulateRoleOptions(vm, User);
                    return View(vm);
                }
            }

            if (!await _userManager.IsInRoleAsync(user, vm.SelectedRole))
            {
                var addRes = await _userManager.AddToRoleAsync(user, vm.SelectedRole);
                if (!addRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    foreach (var err in addRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    PopulateRoleOptions(vm, User);
                    return View(vm);
                }
            }

            if (isSys && vm.IncludeSystemAdmin && !await _userManager.IsInRoleAsync(user, RoleNames.SystemAdministrator))
            {
                var addSysRes = await _userManager.AddToRoleAsync(user, RoleNames.SystemAdministrator);
                if (!addSysRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    foreach (var err in addSysRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    PopulateRoleOptions(vm, User);
                    return View(vm);
                }
            }

            if (isSys && !vm.IncludeSystemAdmin && await _userManager.IsInRoleAsync(user, RoleNames.SystemAdministrator) && vm.SelectedRole != RoleNames.SystemAdministrator)
            {
                var removeSysRes = await _userManager.RemoveFromRoleAsync(user, RoleNames.SystemAdministrator);
                if (!removeSysRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    foreach (var err in removeSysRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    PopulateRoleOptions(vm, User);
                    return View(vm);
                }
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var indexVm = await BuildVm(ct);
        indexVm.SuccessMessage = $"User {user.UserName} updated successfully.";
        return View("Index", indexVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionNames.Users.AssignRole)]
    public async Task<IActionResult> ChangeRole(Guid id, string newRole, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var current = await _userManager.GetUserAsync(User);
        var isSys = User.IsInRole(RoleNames.SystemAdministrator);

        if (!isSys && user.CompanyId != current?.CompanyId)
            return Forbid();

        if (!isSys)
        {
            if (newRole == RoleNames.SystemAdministrator)
                return Forbid();

            if (!RoleNames.CompanySafeAssignable.Contains(newRole))
                return Forbid();
        }
        else
        {
            if (!RoleNames.All.Contains(newRole))
                return BadRequest("Invalid role.");
        }

        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var existingRoles = (await _userManager.GetRolesAsync(user)).ToList();
            var rolesToKeep = new List<string>();
            if (isSys)
            {
                rolesToKeep.Add(newRole);
            }
            else
            {
                rolesToKeep.AddRange(existingRoles.Where(r => r == RoleNames.SystemAdministrator));
                if (!rolesToKeep.Contains(newRole))
                    rolesToKeep.Add(newRole);
            }
            var rolesToRemove = existingRoles.Except(rolesToKeep).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeRes = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    var vmFail = await BuildVm(ct);
                    vmFail.ErrorMessage = string.Join(" | ", removeRes.Errors.Select(e => e.Description));
                    return View("Index", vmFail);
                }
            }

            if (!await _userManager.IsInRoleAsync(user, newRole))
            {
                var addRes = await _userManager.AddToRoleAsync(user, newRole);
                if (!addRes.Succeeded)
                {
                    await tx.RollbackAsync(ct);
                    var vmFail = await BuildVm(ct);
                    vmFail.ErrorMessage = string.Join(" | ", addRes.Errors.Select(e => e.Description));
                    return View("Index", vmFail);
                }
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var vm = await BuildVm(ct);
        vm.SuccessMessage = $"Role updated for {user.UserName} to {RoleNames.GetDisplayName(newRole)}.";
        return View("Index", vm);
    }

    private static void PopulateRoleOptions(UserAddViewModel vm, ClaimsPrincipal user)
    {
        var isSys = user.IsInRole(RoleNames.SystemAdministrator);
        IEnumerable<string> allowedRoles;
        if (isSys)
            allowedRoles = RoleNames.All.AsEnumerable();
        else
            allowedRoles = RoleNames.CompanySafeAssignable.AsEnumerable();
        vm.RoleOptions = allowedRoles
            .Select(role => new SelectListItem
            {
                Text = RoleNames.GetDisplayName(role),
                Value = role
            })
            .ToList();
    }

    private static void PopulateRoleOptions(UserEditViewModel vm, ClaimsPrincipal user)
    {
        var isSys = user.IsInRole(RoleNames.SystemAdministrator);
        IEnumerable<string> allowedRoles;
        if (isSys)
            allowedRoles = RoleNames.All.AsEnumerable();
        else
            allowedRoles = RoleNames.CompanySafeAssignable.AsEnumerable();
        vm.RoleOptions = allowedRoles
            .Select(role => new SelectListItem
            {
                Text = RoleNames.GetDisplayName(role),
                Value = role,
                Selected = role == vm.SelectedRole
            })
            .ToList();
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
        List<UserRowViewModel> rows = [];
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
                Roles = roles.OrderBy(x => x).Select(RoleNames.GetDisplayName).ToList(),
                IsSystemAdmin = roles.Contains(RoleNames.SystemAdministrator),
                CompanyId = u.CompanyId
            });
        }
        return new UserManagementListViewModel
        {
            Items = rows,
            CurrentUserCanResetPasswords = true,
            CurrentUserIsSystemAdmin = isSys,
            CurrentUserCompanyId = coId
        };
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Users)]
    public async Task<IActionResult> MobileIndex(CancellationToken ct)
        => View("MobileIndex", await BuildVm(ct));
}
