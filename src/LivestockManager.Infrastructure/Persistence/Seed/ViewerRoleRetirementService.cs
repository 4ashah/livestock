using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Infrastructure.Identity;

namespace LivestockManager.Infrastructure.Persistence.Seed;

public static class ViewerRoleRetirementService
{
    private const string RetiredViewerRoleName = "Viewer";
    private const string ViewerOnlyAuditMessage = "Viewer role retired; account migrated to Employee and disabled pending administrator review.";
    private const string NoCompanyAuditMessage = "Viewer role retired; account migrated to Employee and disabled pending SystemAdministrator review because no valid company assignment exists.";

    public static async Task RunAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(logger);

        await EnsureFinalRolesAsync(roleManager);

        var viewerRole = await roleManager.FindByNameAsync(RetiredViewerRoleName);
        if (viewerRole == null)
        {
            logger.LogInformation("[ROLE-MIGRATION] Viewer role not found. No retirement work required.");
            return;
        }

        var viewerUsers = await userManager.GetUsersInRoleAsync(RetiredViewerRoleName);
        var auditLogs = new List<AuditLog>();
        var migrationReport = new List<object>();

        logger.LogInformation("[ROLE-MIGRATION] Found {ViewerUserCount} users assigned to retired Viewer role.", viewerUsers.Count);

        foreach (var user in viewerUsers.OrderBy(u => u.UserName))
        {
            ct.ThrowIfCancellationRequested();

            var currentRoles = (await userManager.GetRolesAsync(user))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToList();

            var validRolesExcludingViewer = currentRoles
                .Where(r => !string.Equals(r, RetiredViewerRoleName, StringComparison.Ordinal)
                            && RoleNames.All.Contains(r, StringComparer.Ordinal))
                .ToList();

            var hadOnlyViewer = validRolesExcludingViewer.Count == 0;
            var missingCompany = !user.CompanyId.HasValue || user.CompanyId == Guid.Empty;
            var accountWasEnabled = user.IsEnabled;
            string migrationAction;
            string? auditMessage = null;

            if (hadOnlyViewer)
            {
                if (!await userManager.IsInRoleAsync(user, RoleNames.DataEntry))
                {
                    var addResult = await userManager.AddToRoleAsync(user, RoleNames.DataEntry);
                    ThrowIfFailed(addResult, $"assign {RoleNames.DataEntry} to {user.UserName}");
                    validRolesExcludingViewer.Add(RoleNames.DataEntry);
                }

                user.IsEnabled = false;
                user.ModifiedAt = DateTimeOffset.UtcNow;
                var updateResult = await userManager.UpdateAsync(user);
                ThrowIfFailed(updateResult, $"disable migrated user {user.UserName}");

                migrationAction = missingCompany
                    ? "ViewerOnlyNoCompany -> DataEntryDisabled"
                    : "ViewerOnly -> DataEntryDisabled";
                auditMessage = missingCompany ? NoCompanyAuditMessage : ViewerOnlyAuditMessage;
            }
            else
            {
                migrationAction = "ViewerPlusValidRole -> ViewerRemoved";
            }

            if (await userManager.IsInRoleAsync(user, RetiredViewerRoleName))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, RetiredViewerRoleName);
                ThrowIfFailed(removeResult, $"remove Viewer from {user.UserName}");
            }

            if (!string.IsNullOrWhiteSpace(auditMessage))
            {
                auditLogs.Add(new AuditLog("Identity.ViewerRoleRetired", DateTimeOffset.UtcNow)
                {
                    CompanyId = user.CompanyId,
                    UserId = user.Id,
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id.ToString(),
                    NewValuesJson = JsonSerializer.Serialize(new
                    {
                        message = auditMessage,
                        currentRoles,
                        resultingRoles = validRolesExcludingViewer.OrderBy(r => r, StringComparer.Ordinal).ToArray(),
                        previousEnabledState = accountWasEnabled,
                        resultingEnabledState = user.IsEnabled
                    })
                });
            }

            migrationReport.Add(new
            {
                userId = user.Id,
                companyId = user.CompanyId,
                userName = user.UserName,
                currentRoleState = currentRoles,
                migrationAction,
                resultingEnabledState = user.IsEnabled
            });
        }

        if (auditLogs.Count > 0)
        {
            await db.AuditLogs.AddRangeAsync(auditLogs, ct);
            await db.SaveChangesAsync(ct);
        }

        var remainingViewerAssignments = await db.UserRoles
            .Join(db.Roles.Where(r => r.Name == RetiredViewerRoleName),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => ur)
            .CountAsync(ct);

        if (remainingViewerAssignments == 0)
        {
            var deleteRoleResult = await roleManager.DeleteAsync(viewerRole);
            ThrowIfFailed(deleteRoleResult, "delete retired Viewer role");

            db.AuditLogs.Add(new AuditLog("Identity.ViewerRoleDeleted", DateTimeOffset.UtcNow)
            {
                EntityType = nameof(ApplicationRole),
                EntityId = viewerRole.Id.ToString(),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    message = "Viewer role deleted after migration completed.",
                    report = migrationReport
                })
            });
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "[ROLE-MIGRATION] Viewer retirement finished. Viewer users processed={Processed}, remaining assignments={RemainingAssignments}.",
            migrationReport.Count,
            remainingViewerAssignments);
    }

    private static async Task EnsureFinalRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            new { Name = RoleNames.DataEntry, Description = "Employee operational access" },
            new { Name = RoleNames.FarmManager, Description = "Farm and operational management" },
            new { Name = RoleNames.Accounts, Description = "Finance and accounting access" },
            new { Name = RoleNames.OperationsManager, Description = "Company-wide manager access" },
            new { Name = RoleNames.CompanyAdministrator, Description = "Full company-level administrative access" },
            new { Name = RoleNames.SystemAdministrator, Description = "Cross-company system-level access" }
        };

        foreach (var roleDef in roles)
        {
            var existing = await roleManager.FindByNameAsync(roleDef.Name);
            if (existing == null)
            {
                var createResult = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleDef.Name,
                    Description = roleDef.Description
                });

                if (createResult.Succeeded)
                {
                    continue;
                }

                if (!createResult.Errors.Any(e => string.Equals(e.Code, nameof(IdentityErrorDescriber.DuplicateRoleName), StringComparison.Ordinal)))
                {
                    ThrowIfFailed(createResult, $"create role {roleDef.Name}");
                }

                existing = await roleManager.FindByNameAsync(roleDef.Name);
                if (existing == null)
                {
                    ThrowIfFailed(createResult, $"create role {roleDef.Name}");
                    throw new InvalidOperationException($"Role {roleDef.Name} was not found after duplicate role handling.");
                }
            }

            if (string.Equals(existing.Description, roleDef.Description, StringComparison.Ordinal))
            {
                continue;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                existing.Description = roleDef.Description;
                var updateResult = await roleManager.UpdateAsync(existing);
                if (updateResult.Succeeded)
                {
                    break;
                }

                if (!updateResult.Errors.Any(e => string.Equals(e.Code, nameof(IdentityErrorDescriber.ConcurrencyFailure), StringComparison.Ordinal)))
                {
                    ThrowIfFailed(updateResult, $"update role description for {roleDef.Name}");
                }

                existing = await roleManager.FindByNameAsync(roleDef.Name);
                if (existing == null || string.Equals(existing.Description, roleDef.Description, StringComparison.Ordinal))
                {
                    break;
                }

                if (attempt == 2)
                {
                    ThrowIfFailed(updateResult, $"update role description for {roleDef.Name}");
                }
            }
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var message = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
        throw new InvalidOperationException($"Identity operation failed while attempting to {action}: {message}");
    }
}
