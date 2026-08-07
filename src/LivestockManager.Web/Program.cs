using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LivestockManager.Application;
using LivestockManager.Domain.Common;
using LivestockManager.Infrastructure;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("UP"), tags: new[] { "live", "ready" })
    .AddDbContextCheck<AppDbContext>("db", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });

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
        await DemoDataSeeder.SeedAsync(db, userManager, roleManager, config);
    }
    catch
    {
    }
}

app.Run();
