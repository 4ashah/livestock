using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Persistence.Seed;

public static class DemoDataSeeder
{
    private const string DevPassword = "Dev@123456";

    internal static string? LastEnvironmentCheckedName;
    internal static bool LastProductionShortCircuited;
    internal static bool LastSeedConditionsMet;
    internal static int LastUsersEnumeratedCount;
    internal static bool LastUsedTestingE2EPath;
    internal static bool LastUsedDevelopmentPath;

    internal static readonly string[] KnownDevEmails = new[]
    {
        "admin@livestock.dev",
        "accounts@livestock.dev",
        "farmmanager@livestock.dev",
        "operationsmanager@livestock.dev",
        "dataentry@livestock.dev",
        "sysadmin@livestock.dev"
    };

    internal static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        return v == "true" || v == "1" || v == "yes";
    }

    internal static bool IsTestingEnvironment(string? envName)
    {
        if (string.IsNullOrWhiteSpace(envName)) return false;
        var n = envName.Trim();
        return string.Equals(n, "Testing", StringComparison.OrdinalIgnoreCase)
               || n.StartsWith("Testing", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsDevelopmentEnvironment(string? envName)
    {
        if (string.IsNullOrWhiteSpace(envName)) return false;
        var n = envName.Trim();
        return string.Equals(n, "Development", StringComparison.OrdinalIgnoreCase)
               || n.Contains("Development", StringComparison.OrdinalIgnoreCase);
    }

    internal static (bool TestingE2EActive, bool DevelopmentActive) EvaluateSeedConditions(
        IHostEnvironment env,
        IConfiguration config)
    {
        bool testingActive = false;
        bool devActive = false;

        string? envName = env?.EnvironmentName;
        string? enableE2E = config?["EnableE2ESeed"];
        string? enableDev = config?["EnableDevSeed"];

        if (IsTestingEnvironment(envName) && IsTruthy(enableE2E))
        {
            testingActive = true;
        }

        if (IsDevelopmentEnvironment(envName) && IsTruthy(enableDev))
        {
            devActive = true;
        }

        return (testingActive, devActive);
    }

    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IHostEnvironment env,
        IConfiguration config)
    {
        LastEnvironmentCheckedName = env?.EnvironmentName;
        LastProductionShortCircuited = false;
        LastSeedConditionsMet = false;
        LastUsersEnumeratedCount = 0;
        LastUsedTestingE2EPath = false;
        LastUsedDevelopmentPath = false;

        if (env == null) return;

        if (env.IsProduction())
        {
            LastProductionShortCircuited = true;
            return;
        }

        if (dbContext == null || userManager == null || roleManager == null || config == null) return;

        var (testingE2E, development) = EvaluateSeedConditions(env, config);

        if (!testingE2E && !development)
        {
            return;
        }

        LastSeedConditionsMet = true;
        if (testingE2E) LastUsedTestingE2EPath = true;
        if (development) LastUsedDevelopmentPath = true;

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var company = await SeedCompanyAsync(dbContext);
            await SeedFarmsAsync(dbContext, company.Id);
            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager, company.Id);
            await SeedStandardUsersAsync(userManager, company.Id);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<Company> SeedCompanyAsync(AppDbContext dbContext)
    {
        var existing = await dbContext.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Name == "Livestock Co");
        if (existing != null) return existing;

        var company = new Company("Livestock Co")
        {
            Currency = Currency.USD,
            WeightUnit = WeightUnit.Kg,
            TaxRate = 0.15m,
            InvoicePrefix = "INV",
            IsActive = true
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();
        return company;
    }

    private static async Task SeedFarmsAsync(AppDbContext dbContext, Guid companyId)
    {
        var farmSpecs = new[]
        {
            new { Code = "FARM01", Name = "Main Farm" },
            new { Code = "FARM02", Name = "South Pasture" }
        };

        foreach (var spec in farmSpecs)
        {
            var existing = await dbContext.Farms.IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.Code == spec.Code);
            if (existing != null)
                continue;

            var farm = new Farm(companyId, spec.Name, spec.Code)
            {
                Currency = Currency.USD,
                WeightUnit = WeightUnit.Kg,
                IsActive = true
            };

            dbContext.Farms.Add(farm);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            new { Name = RoleNames.SystemAdministrator, Desc = "Cross-company system-level access" },
            new { Name = RoleNames.CompanyAdministrator, Desc = "Full company-level access" },
            new { Name = RoleNames.OperationsManager, Desc = "Company-wide manager access" },
            new { Name = RoleNames.Accounts, Desc = "Finance and accounting access" },
            new { Name = RoleNames.FarmManager, Desc = "Farm and operational management" },
            new { Name = RoleNames.DataEntry, Desc = "Employee operational access" }
        };

        foreach (var r in roles)
        {
            var existing = await roleManager.FindByNameAsync(r.Name);
            if (existing != null)
                continue;

            var role = new ApplicationRole
            {
                Name = r.Name,
                Description = r.Desc
            };

            await roleManager.CreateAsync(role);
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, Guid companyId)
    {
        const string adminEmail = "admin@livestock.dev";
        const string adminUserName = "admin";
        LastUsersEnumeratedCount++;
        var existing = await userManager.FindByEmailAsync(adminEmail)
                     ?? await userManager.FindByNameAsync(adminUserName);
        if (existing != null)
        {
            var now = DateTimeOffset.UtcNow;
            existing.UserName = adminUserName;
            existing.NormalizedUserName = adminUserName.ToUpperInvariant();
            existing.LockoutEnd = null;
            existing.AccessFailedCount = 0;
            existing.LockoutEnabled = true;
            existing.IsEnabled = true;
            existing.LastLoginAt = null;
            await userManager.UpdateAsync(existing);
            if (existing.LastLoginAt is null || (now - existing.LastLoginAt) > TimeSpan.FromMinutes(2))
            {
                if ((await userManager.CheckPasswordAsync(existing, DevPassword)) == false)
                {
                    if ((await userManager.HasPasswordAsync(existing)) == false)
                    {
                        await userManager.AddPasswordAsync(existing, DevPassword);
                    }
                    else
                    {
                        var remove = await userManager.RemovePasswordAsync(existing);
                        if (remove.Succeeded)
                        {
                            await userManager.AddPasswordAsync(existing, DevPassword);
                        }
                    }
                }
            }
            var r1 = await userManager.IsInRoleAsync(existing, RoleNames.CompanyAdministrator);
            var r2 = await userManager.IsInRoleAsync(existing, RoleNames.SystemAdministrator);
            if (!r1) await userManager.AddToRoleAsync(existing, RoleNames.CompanyAdministrator);
            if (!r2) await userManager.AddToRoleAsync(existing, RoleNames.SystemAdministrator);
            if (!await userManager.IsInRoleAsync(existing, "Administrator"))
            {
                try { await userManager.AddToRoleAsync(existing, "Administrator"); } catch (Exception ex) { Console.WriteLine($"[SEED][WARN] Failed to assign legacy role 'Administrator' to existing admin user: {ex.Message}"); }
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Admin",
            CompanyId = companyId,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, DevPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, RoleNames.CompanyAdministrator);
            await userManager.AddToRoleAsync(user, RoleNames.SystemAdministrator);
            try { await userManager.AddToRoleAsync(user, "Administrator"); } catch (Exception ex) { Console.WriteLine($"[SEED][WARN] Failed to assign legacy role 'Administrator' to new admin user: {ex.Message}"); }
        }
    }

    private static async Task SeedStandardUsersAsync(UserManager<ApplicationUser> userManager, Guid companyId)
    {
        var otherUsers = new[]
        {
            new { UserName = "accounts",    Email = "accounts@livestock.dev",    Name = "Accounts User",         Role = RoleNames.Accounts },
            new { UserName = "farmmanager", Email = "farmmanager@livestock.dev", Name = "Farm Manager",          Role = RoleNames.FarmManager },
            new { UserName = "operationsmanager", Email = "operationsmanager@livestock.dev", Name = "Operations Manager", Role = RoleNames.OperationsManager },
            new { UserName = "dataentry",   Email = "dataentry@livestock.dev",         Name = "Employee",              Role = RoleNames.DataEntry },
            new { UserName = "sysadmin",    Email = "sysadmin@livestock.dev",    Name = "System Administrator",  Role = RoleNames.SystemAdministrator }
        };

        const string standardPwd = DevPassword;

        foreach (var spec in otherUsers)
        {
            LastUsersEnumeratedCount++;
            var existing = await userManager.FindByEmailAsync(spec.Email)
                         ?? await userManager.FindByNameAsync(spec.UserName);
            if (existing != null)
            {
                existing.UserName = spec.UserName;
                existing.NormalizedUserName = spec.UserName.ToUpperInvariant();
                existing.LockoutEnd = null;
                existing.AccessFailedCount = 0;
                existing.LockoutEnabled = true;
                existing.IsEnabled = true;
                existing.LastLoginAt = null;
                await userManager.UpdateAsync(existing);
                if ((await userManager.CheckPasswordAsync(existing, standardPwd)) == false)
                {
                    if ((await userManager.HasPasswordAsync(existing)) == false)
                    {
                        await userManager.AddPasswordAsync(existing, standardPwd);
                    }
                    else
                    {
                        var remove = await userManager.RemovePasswordAsync(existing);
                        if (remove.Succeeded)
                        {
                            await userManager.AddPasswordAsync(existing, standardPwd);
                        }
                    }
                }
                if (!await userManager.IsInRoleAsync(existing, spec.Role))
                {
                    try { await userManager.AddToRoleAsync(existing, spec.Role); } catch (Exception ex) { Console.WriteLine($"[SEED][WARN] Failed to assign role '{spec.Role}' to user: {ex.Message}"); }
                }
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = spec.UserName,
                Email = spec.Email,
                EmailConfirmed = true,
                FullName = spec.Name,
                CompanyId = companyId,
                IsEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var r = await userManager.CreateAsync(user, standardPwd);
            if (r.Succeeded)
            {
                await userManager.AddToRoleAsync(user, spec.Role);
            }
        }
    }
}
