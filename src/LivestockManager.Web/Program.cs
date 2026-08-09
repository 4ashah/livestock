using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using LivestockManager.Application;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Persistence.Seed;
using LivestockManager.Infrastructure.Security;
using LivestockManager.Infrastructure.Services.Storage;
using LivestockManager.Web;

var earlyConfigBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

var earlyConfig = earlyConfigBuilder.Build();

var aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var earlyCanonical = earlyConfig[ConnectionStringStartupValidator.CanonicalKey];
var earlyLegacy = earlyConfig[ConnectionStringStartupValidator.LegacyKey];

var earlyResult = ConnectionStringStartupValidator.ValidateAndResolve(
    aspNetEnv,
    earlyCanonical,
    earlyLegacy,
    flag => earlyConfig[flag]);

if (!earlyResult.Success)
{
    Console.Error.WriteLine("[STARTUP-FATAL] Connection string validation failed:");
    foreach (var e in earlyResult.Errors)
    {
        Console.Error.WriteLine($"  [ERROR] {e}");
    }
    Environment.Exit(1);
}

foreach (var w in earlyResult.Warnings)
{
    Console.WriteLine($"[STARTUP-WARN] {w}");
}

var configExtValue = earlyConfig["FileStorage:AllowedExtensions"];
string[]? configExts = string.IsNullOrWhiteSpace(configExtValue)
    ? null
    : configExtValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
var (extMismatch, extWarning) = ProtectedFileUploadValidator.CheckConfigAgainstAllowList(configExts);
if (extMismatch && !string.IsNullOrWhiteSpace(extWarning))
{
    Console.WriteLine($"[STARTUP-WARN] {extWarning}");
}

var configSizeStr = earlyConfig["FileStorage:MaxUploadBytes"];
if (!string.IsNullOrWhiteSpace(configSizeStr) && long.TryParse(configSizeStr, out var configSize)
    && configSize != ProtectedFileUploadValidator.MaxFileSizeBytes)
{
    Console.WriteLine($"[STARTUP-WARN] Config FileStorage:MaxUploadBytes={configSize} differs from server code-constant MaxFileSizeBytes={ProtectedFileUploadValidator.MaxFileSizeBytes}; server always uses code-constant (config is documentation only).");
}

Console.WriteLine($"[STARTUP-INFO] Connection validation: {earlyResult.SanitizedSummary}");

try
{
    bool isEarlyProduction = string.Equals(aspNetEnv, "Production", StringComparison.OrdinalIgnoreCase);
    if (isEarlyProduction)
    {
        string[] earlySeedFlags = { "SeedDemoData", "EnableDevSeed", "EnableE2ESeed", "ENV_ENABLE_DEV_SEED" };
        foreach (var flag in earlySeedFlags)
        {
            var flagVal = earlyConfig[flag];
            if (!string.IsNullOrWhiteSpace(flagVal))
            {
                var v = flagVal.Trim().ToLowerInvariant();
                if (v == "true" || v == "1" || v == "yes")
                {
                    throw new InvalidOperationException($"UNSAFE_SEED_FLAG_{flag}");
                }
            }
        }
    }
}
catch (InvalidOperationException sex) when (sex.Message.StartsWith("UNSAFE_SEED_FLAG_", StringComparison.Ordinal))
{
    Console.Error.WriteLine("[STARTUP-FATAL] Unsafe seed flag detected in Production, refusing to start.");
    Environment.Exit(1);
}

if (!string.IsNullOrWhiteSpace(earlyResult.ResolvedConnectionString) &&
    earlyResult.UsedLegacyFallback)
{
    Environment.SetEnvironmentVariable(
        ConnectionStringStartupValidator.CanonicalEnvVar,
        earlyResult.ResolvedConnectionString,
        EnvironmentVariableTarget.Process);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    [ConnectionStringStartupValidator.CanonicalKey] = earlyResult.ResolvedConnectionString
});

builder.Services.AddControllersWithViews();

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("UP"), tags: new[] { "live", "ready" })
    .AddDbContextCheck<AppDbContext>("db", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" })
    .AddCheck("clock", () => !string.IsNullOrWhiteSpace(TimeZoneInfo.Local.Id) && DateTime.UtcNow >= new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) && DateTime.UtcNow <= new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc) ? HealthCheckResult.Healthy("Clock and timezone valid.") : HealthCheckResult.Degraded("System clock or timezone appears invalid"), tags: new[] { "live", "ready" });

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddRazorPages();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewOperationalData", policy =>
        policy.RequireRole(RoleNames.Viewer, RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));

    options.AddPolicy("CanManageLivestock", policy =>
        policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));

    options.AddPolicy("CanManageSales", policy =>
        policy.RequireRole(RoleNames.FarmManager, RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));

    options.AddPolicy("CanManageAccounting", policy =>
        policy.RequireRole(RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));

    options.AddPolicy("CanManageCompany", policy =>
        policy.RequireRole(RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));

    options.AddPolicy("CanManageSystem", policy =>
        policy.RequireRole(RoleNames.SystemAdministrator));

    options.AddPolicy("CanViewFinancialData", policy =>
        policy.RequireRole(RoleNames.Accounts, RoleNames.FarmManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
});

var app = builder.Build();

try
{
    ProductionSeedGuard.CheckAndThrowIfUnsafe(app.Configuration, app.Environment);
}
catch (InvalidOperationException sex) when (sex.Message.StartsWith("UNSAFE_SEED_FLAG_", StringComparison.Ordinal))
{
    Console.Error.WriteLine("[STARTUP-FATAL] Unsafe seed flag detected in Production, refusing to start.");
    Environment.Exit(1);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

static async Task WriteHealthJson(HttpContext ctx, HealthReport r)
{
    ctx.Response.ContentType = "application/json";
    var json = new
    {
        status = r.Status.ToString(),
        totalDuration = Math.Round(r.TotalDuration.TotalSeconds, 3),
        checks = r.Entries.Select(kv => new
        {
            name = kv.Key,
            status = kv.Value.Status.ToString(),
            duration = Math.Round(kv.Value.Duration.TotalSeconds, 3)
        })
    };
    await ctx.Response.WriteAsJsonAsync(json);
}

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("live"),
    ResponseWriter = (ctx, r) => WriteHealthJson(ctx, r)
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = (ctx, r) => WriteHealthJson(ctx, r)
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = (ctx, r) => WriteHealthJson(ctx, r)
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (IServiceScope scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch
    {
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    try
    {
        await DemoDataSeeder.SeedAsync(db, userManager, roleManager, app.Environment, config);
    }
    catch
    {
    }
}

var utcStart = DateTime.UtcNow;
var localTz = TimeZoneInfo.Local;
var localOffset = localTz.GetUtcOffset(utcStart);
Console.WriteLine($"[STARTUP-INFO] UTC start time: {utcStart:o}");
Console.WriteLine($"[STARTUP-INFO] Local timezone id: {localTz.Id}");
Console.WriteLine($"[STARTUP-INFO] Local offset: {(localOffset < TimeSpan.Zero ? "-" : "+")}{localOffset:hh\\:mm}");
Console.WriteLine("[STARTUP-INFO] Clock verification: machine clock MUST be accurate (see ClockVer.md notes).");

var cmdArgs = args ?? Array.Empty<string>();
var hasFirstAdminSwitch = cmdArgs.Any(a => string.Equals(a, "--first-admin", StringComparison.OrdinalIgnoreCase));
if (hasFirstAdminSwitch)
{
    Console.WriteLine("[FIRST-ADMIN] --first-admin CLI switch detected. Running one-time first-company + first-admin creation.");
    static string? GetCliSwitch(string[] argsArr, string key)
    {
        for (int i = 0; i < argsArr.Length - 1; i++)
        {
            if (string.Equals(argsArr[i], key, StringComparison.OrdinalIgnoreCase))
                return argsArr[i + 1];
        }
        return null;
    }

    var cliCompany = GetCliSwitch(cmdArgs, "--first-admin-company");
    var cliFullName = GetCliSwitch(cmdArgs, "--first-admin-fullname");
    var cliEmail = GetCliSwitch(cmdArgs, "--first-admin-email");
    var envPassword = Environment.GetEnvironmentVariable("FIRST_ADMIN_PASSWORD");
    try
    {
        Environment.SetEnvironmentVariable("FIRST_ADMIN_PASSWORD", null, EnvironmentVariableTarget.Process);
    }
    catch { }

    if (string.IsNullOrWhiteSpace(cliCompany) || string.IsNullOrWhiteSpace(cliFullName)
        || string.IsNullOrWhiteSpace(cliEmail) || string.IsNullOrWhiteSpace(envPassword))
    {
        Console.Error.WriteLine("[FIRST-ADMIN][FATAL] Missing required parameters. Use:");
        Console.Error.WriteLine("  --first-admin-company \"Company Name\"");
        Console.Error.WriteLine("  --first-admin-fullname \"Admin Full Name\"");
        Console.Error.WriteLine("  --first-admin-email \"admin@company.com\"");
        Console.Error.WriteLine("  And environment variable FIRST_ADMIN_PASSWORD (set by New-FirstProductionAdmin.ps1).");
        Environment.Exit(6);
    }

    using (var adminScope = app.Services.CreateScope())
    {
        var adb = adminScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var aum = adminScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var arm = adminScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        int userCount = await aum.Users.CountAsync();
        Console.WriteLine($"[FIRST-ADMIN] AspNetUsers row count = {userCount} (safety guard: proceed only when == 0).");
        if (userCount != 0)
        {
            Console.Error.WriteLine("[FIRST-ADMIN][FATAL] AspNetUsers already contains users (count != 0). Refusing first-admin creation for safety. Create additional users via the application UI or standard UserManager APIs.");
            Environment.Exit(7);
        }

        var roleNames = new[]
        {
            new { Name = RoleNames.SystemAdministrator, Desc = "Cross-company system-level access" },
            new { Name = RoleNames.CompanyAdministrator, Desc = "Full company-level access" },
            new { Name = RoleNames.Accounts, Desc = "Finance and accounts management" },
            new { Name = RoleNames.FarmManager, Desc = "Farm and operational management" },
            new { Name = RoleNames.DataEntry, Desc = "Can create and edit records" },
            new { Name = RoleNames.Viewer, Desc = "Read-only access" }
        };
        foreach (var r in roleNames)
        {
            var ex = await arm.FindByNameAsync(r.Name);
            if (ex == null)
            {
                var cr = await arm.CreateAsync(new ApplicationRole { Name = r.Name, Description = r.Desc });
                if (!cr.Succeeded)
                    Console.WriteLine($"[FIRST-ADMIN][WARN] Create role {r.Name} failed: {string.Join(", ", cr.Errors.Select(e => e.Description))}");
                else
                    Console.WriteLine($"[FIRST-ADMIN] Created role: {r.Name}");
            }
        }

        Company company;
        var existingCompany = await adb.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Name == cliCompany);
        if (existingCompany != null)
        {
            company = existingCompany;
            Console.WriteLine($"[FIRST-ADMIN] Company already exists: {company.Name} (Id={company.Id:N})");
        }
        else
        {
            company = new Company(cliCompany)
            {
                Currency = Currency.USD,
                WeightUnit = WeightUnit.Kg,
                TaxRate = 0.15m,
                InvoicePrefix = "INV",
                IsActive = true
            };
            adb.Companies.Add(company);
            await adb.SaveChangesAsync();
            Console.WriteLine($"[FIRST-ADMIN] Created company: {company.Name} (Id={company.Id:N})");
        }

        var adminUser = new ApplicationUser
        {
            UserName = cliEmail,
            Email = cliEmail,
            FullName = cliFullName,
            EmailConfirmed = true,
            CompanyId = company.Id,
            IsEnabled = true,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var createAdmin = await aum.CreateAsync(adminUser, envPassword);
        if (!createAdmin.Succeeded)
        {
            Console.Error.WriteLine("[FIRST-ADMIN][FATAL] UserManager<ApplicationUser>.CreateAsync failed for first admin:");
            foreach (var e in createAdmin.Errors)
                Console.Error.WriteLine($"  IDENT_ERR code={e.Code} desc={e.Description}");
            Environment.Exit(8);
        }
        Console.WriteLine($"[FIRST-ADMIN] Created first admin user: {adminUser.Email} (Id={adminUser.Id:N})");

        var rCompanyAdmin = await aum.AddToRoleAsync(adminUser, RoleNames.CompanyAdministrator);
        var rSystemAdmin = await aum.AddToRoleAsync(adminUser, RoleNames.SystemAdministrator);
        if (rCompanyAdmin.Succeeded && rSystemAdmin.Succeeded)
            Console.WriteLine("[FIRST-ADMIN] Assigned CompanyAdministrator + SystemAdministrator roles to first admin user.");
        else
            Console.WriteLine("[FIRST-ADMIN][WARN] Role assignment may have failed; verify via UI.");

        Console.WriteLine("[FIRST-ADMIN] SUCCESS: First company + admin + required roles created.");
        Console.WriteLine($"[FIRST-ADMIN]   Company: {company.Name}");
        Console.WriteLine($"[FIRST-ADMIN]   Admin  : {adminUser.FullName} <{adminUser.Email}>");
        Console.WriteLine("[FIRST-ADMIN] Exiting with code 0 (one-time first-admin run complete; web server will NOT be started).");
        Environment.Exit(0);
    }
}

app.Run();
