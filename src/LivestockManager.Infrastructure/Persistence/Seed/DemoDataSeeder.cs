using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Persistence.Seed;

public static class DemoDataSeeder
{
    private const string DevPassword = "Dev@123456";

    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? configuration["ASPNETCORE_ENVIRONMENT"];
        var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        var seedDemoData = configuration["SeedDemoData"] == "1";
        var enableDevSeed = configuration["EnableDevSeed"] == "true";
        var enableE2ESeed = configuration["EnableE2ESeed"] == "1";
        Environment.SetEnvironmentVariable("LIVESEED", "0");
        var seedEnv = (env == "Development" || env == "Staging" || env == "Testing");
        var shouldSeed = seedEnv && (enableDevSeed || enableE2ESeed);
        if (isProduction && !seedDemoData && !enableE2ESeed)
        {
            return;
        }
        if (!shouldSeed && !seedDemoData && !enableE2ESeed)
        {
            return;
        }

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
            new { Name = RoleNames.Accounts, Desc = "Finance and accounts management" },
            new { Name = RoleNames.FarmManager, Desc = "Farm and operational management" },
            new { Name = RoleNames.DataEntry, Desc = "Can create and edit records" },
            new { Name = RoleNames.Viewer, Desc = "Read-only access" }
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
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing != null)
        {
            var now = DateTimeOffset.UtcNow;
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
                try { await userManager.AddToRoleAsync(existing, "Administrator"); } catch { }
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = adminEmail,
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
            try { await userManager.AddToRoleAsync(user, "Administrator"); } catch { }
        }
    }

    private static async Task SeedStandardUsersAsync(UserManager<ApplicationUser> userManager, Guid companyId)
    {
        var otherUsers = new[]
        {
            new { Email = "accounts@livestock.dev", Name = "Accounts User", Role = RoleNames.Accounts },
            new { Email = "farmmanager@livestock.dev", Name = "Farm Manager", Role = RoleNames.FarmManager },
            new { Email = "dataentry@livestock.dev", Name = "Data Entry Clerk", Role = RoleNames.DataEntry },
            new { Email = "viewer@livestock.dev", Name = "View-only Viewer", Role = RoleNames.Viewer },
            new { Email = "sysadmin@livestock.dev", Name = "System Administrator", Role = RoleNames.SystemAdministrator }
        };

        const string standardPwd = DevPassword;

        foreach (var spec in otherUsers)
        {
            var existing = await userManager.FindByEmailAsync(spec.Email);
            if (existing != null)
            {
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
                    try { await userManager.AddToRoleAsync(existing, spec.Role); } catch { }
                }
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = spec.Email,
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
