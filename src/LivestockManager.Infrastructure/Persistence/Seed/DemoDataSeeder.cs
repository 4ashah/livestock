using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;

namespace LivestockManager.Infrastructure.Persistence.Seed;

public static class DemoDataSeeder
{
    private const string AdminPassword = "Admin@123456";

    public static async Task SeedAsync(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (env == "Production")
            return;

        var seedDemoData = configuration["SeedDemoData"] == "1";
        var enableDevSeed = configuration["EnableDevSeed"] == "true";
        var seedEnv = (env == "Development" || env == "Staging");
        var shouldSeed = (seedDemoData || seedEnv) && enableDevSeed;
        if (!shouldSeed)
            return;

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
            new { Name = "Administrator", Desc = "Full system access" },
            new { Name = "Manager", Desc = "Farm and operational management" },
            new { Name = "DataEntry", Desc = "Can create and edit records" },
            new { Name = "Viewer", Desc = "Read-only access" }
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
        if (existing != null) return;

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

        var result = await userManager.CreateAsync(user, AdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Administrator");
        }
    }

    private static async Task SeedStandardUsersAsync(UserManager<ApplicationUser> userManager, Guid companyId)
    {
        var otherUsers = new[]
        {
            new { Email = "manager@livestock.dev", Name = "Farm Manager", Role = "Manager" },
            new { Email = "dataentry@livestock.dev", Name = "Data Entry Clerk", Role = "DataEntry" },
            new { Email = "viewer@livestock.dev", Name = "View-only Viewer", Role = "Viewer" }
        };

        const string standardPwd = "Dev@123456";

        foreach (var spec in otherUsers)
        {
            var existing = await userManager.FindByEmailAsync(spec.Email);
            if (existing != null) continue;

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
