using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Web.Controllers;
using System.Data.Common;
using System.Security.Claims;

namespace LivestockManager.IntegrationTests;

public class TestAuthMiddleware
{
    private readonly RequestDelegate _next;

    public TestAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Test-Role", out var roleHeader)
            && context.Request.Headers.TryGetValue("X-Test-CompanyId", out var companyHeader))
        {
            var role = roleHeader.FirstOrDefault() ?? "Viewer";
            var company = companyHeader.FirstOrDefault() ?? LivestockManagerWebFactory.StagingCompanyIdA;
            var userId = context.Request.Headers["X-Test-UserId"].FirstOrDefault() ?? "test-user-id";

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, role),
                new("CompanyId", company)
            };
            var identity = new ClaimsIdentity(claims, "TestMiddlewareAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        return _next(context);
    }
}

public class TestAuthStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.UseMiddleware<TestAuthMiddleware>();
            next(builder);
        };
    }
}

public class LivestockManagerWebFactory : WebApplicationFactory<HomeController>
{
    private static bool _dbCreated;
    private static readonly object _lock = new();

    public const string StagingCompanyIdA = "11111111-1111-1111-1111-111111111111";
    public const string StagingCompanyIdB = "22222222-2222-2222-2222-222222222222";

    public Action<IServiceCollection>? ConfigureTestServicesHook { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Staging");
        builder.UseSetting("EnableDevSeed", "true");

        builder.ConfigureTestServices(services =>
        {
            services.AddTransient<IStartupFilter, TestAuthStartupFilter>();

            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null)
                services.Remove(dbConnectionDescriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    "Server=.;Database=Test_LivestockManager_Integration;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;");
            });

            ConfigureTestServicesHook?.Invoke(services);

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                lock (_lock)
                {
                    if (!_dbCreated)
                    {
                        try { db.Database.EnsureDeleted(); } catch { }
                        db.Database.EnsureCreated();
                        try
                        {
                            SeedTestData(db);
                        }
                        catch
                        {
                        }
                        _dbCreated = true;
                    }
                }
            }
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        var companyIdA = Guid.Parse(StagingCompanyIdA);
        var companyIdB = Guid.Parse(StagingCompanyIdB);

        int companyAExists = db.Database.ExecuteSqlRaw(
            "SELECT COUNT(*) FROM Companies WHERE Id = {0}",
            companyIdA);
        if (companyAExists == 0)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO Companies (Id, Name, Currency, WeightUnit, TaxRate, InvoicePrefix, ReceiptPrefix, IsActive, CreatedAt)
VALUES ({0}, N'Company A', 0, 0, 0.15, N'INV-A', N'RCP-A', 1, GETUTCDATE())",
                companyIdA);
        }

        int companyBExists = db.Database.ExecuteSqlRaw(
            "SELECT COUNT(*) FROM Companies WHERE Id = {0}",
            companyIdB);
        if (companyBExists == 0)
        {
            db.Database.ExecuteSqlRaw(@"
INSERT INTO Companies (Id, Name, Currency, WeightUnit, TaxRate, InvoicePrefix, ReceiptPrefix, IsActive, CreatedAt)
VALUES ({0}, N'Company B', 0, 0, 0.15, N'INV-B', N'RCP-B', 1, GETUTCDATE())",
                companyIdB);
        }

        try
        {
            var roleNames = new[] { "Viewer", "DataEntry", "FarmManager", "Accounts", "CompanyAdministrator", "SystemAdministrator" };
            foreach (var r in roleNames)
            {
                var existsParam = new object[] { r };
                int exists = db.Database.ExecuteSqlRaw(
                    "SELECT COUNT(*) FROM AspNetRoles WHERE Name = {0}",
                    existsParam);
                if (exists == 0)
                {
                    var roleId = Guid.NewGuid();
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ({0}, {1}, {2}, {3})",
                        roleId, r, r.ToUpperInvariant(), Guid.NewGuid().ToString());
                }
            }
        }
        catch
        {
        }
    }

    public HttpClient CreateAuthenticatedClient(string role, Guid companyId)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost/")
        });
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var companyIdStr = companyId.ToString();
        client.DefaultRequestHeaders.Add("X-Test-CompanyId", companyIdStr);
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateClientNoRedirect()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost/")
        });
    }
}
