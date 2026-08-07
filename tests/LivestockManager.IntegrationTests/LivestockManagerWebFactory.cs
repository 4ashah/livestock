using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
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
        try
        {
            EnsureCompany(db, Guid.Parse(StagingCompanyIdA), "Company A", "INV-A", "RCP-A");
            EnsureCompany(db, Guid.Parse(StagingCompanyIdB), "Company B", "INV-B", "RCP-B");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("WARN: Seed companies failed: " + ex.Message);
        }

        try
        {
            var roleNames = new[] { "Viewer", "DataEntry", "FarmManager", "Accounts", "CompanyAdministrator", "SystemAdministrator" };
            foreach (var r in roleNames)
            {
                int exists = db.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM AspNetRoles WHERE Name = {0}",
                    r).First();
                if (exists == 0)
                {
                    var roleId = Guid.NewGuid();
                    try
                    {
                        db.Database.ExecuteSqlRaw(
                            "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ({0}, {1}, {2}, {3})",
                            roleId, r, r.ToUpperInvariant(), Guid.NewGuid().ToString());
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("WARN: Seed roles failed: " + ex.Message);
        }
    }

    private static void EnsureCompany(AppDbContext db, Guid companyId, string name, string invPrefix, string rcpPrefix)
    {
        bool exists = db.Companies.IgnoreQueryFilters().Any(c => c.Id == companyId);
        if (exists) return;

        var company = new Company(name)
        {
            Currency = Currency.USD,
            WeightUnit = WeightUnit.Kg,
            TaxRate = 0.15m,
            InvoicePrefix = invPrefix,
            ReceiptPrefix = rcpPrefix,
            IsActive = true,
            FinancialYearStartMonth = 1
        };

        var idProp = typeof(BaseAuditableEntity).GetProperty("Id",
            BindingFlags.Public | BindingFlags.Instance);
        if (idProp != null && idProp.GetSetMethod(true) != null)
        {
            idProp.SetValue(company, companyId);
        }

        var createdAtProp = typeof(BaseAuditableEntity).GetProperty("CreatedAt",
            BindingFlags.Public | BindingFlags.Instance);
        if (createdAtProp != null)
        {
            createdAtProp.SetValue(company, DateTimeOffset.UtcNow);
        }

        db.Companies.Add(company);
        try
        {
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WARN: EnsureCompany({name}) SaveChanges failed: " + ex.Message);
            try
            {
                db.Entry(company).State = EntityState.Detached;
            }
            catch
            {
            }
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
