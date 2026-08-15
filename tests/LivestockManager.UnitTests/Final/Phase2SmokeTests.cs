using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Application;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Domain.Common;
using LivestockManager.Domain.Entities;
using LivestockManager.Domain.Enums;
using LivestockManager.Infrastructure;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Services.Pdf;
using LivestockManager.Infrastructure.Services.Sequencing;
using LivestockManager.Infrastructure.Services.Storage;

namespace LivestockManager.UnitTests.Final;

public class Phase2SmokeTests
{
    [Fact]
    public void AllPhase2DbSetsExist_OnAppDbContext()
    {
        var phase2Props = new[] { "Suppliers", "Purchases", "PurchaseItems", "Expenses", "Receipts", "InvoiceAdditionalCharges", "Documents" };
        var appDbProps = typeof(AppDbContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.Name)
            .ToList();

        var count = phase2Props.Count(p => appDbProps.Contains(p));
        Assert.Equal(7, count);
    }

    [Fact]
    public void Phase2EntitiesMigration_Exists()
    {
        var infraAssembly = typeof(AppDbContext).Assembly;
        var migrationNames = infraAssembly.GetTypes()
            .Where(t => t.Namespace == "LivestockManager.Infrastructure.Persistence.Migrations")
            .Select(t => t.Name)
            .ToList();

        var hasPhase2 = migrationNames.Any(n => n.Contains("Phase2Entities") && n.EndsWith(".cs") == false);

        var migrationsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "src", "LivestockManager.Infrastructure", "Persistence", "Migrations");
        var resolved = Path.GetFullPath(migrationsDir);
        bool hasFile = false;
        if (Directory.Exists(resolved))
        {
            hasFile = Directory.GetFiles(resolved, "*Phase2Entities*.cs").Length > 0;
        }

        Assert.True(hasPhase2 || hasFile, "Phase2Entities migration file/type must exist.");
    }

    [Fact]
    public void NewRoles_6CountInRoleNames()
    {
        var expected = new[]
        {
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        };

        var constFields = typeof(RoleNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        var primaryOnly = constFields
            .Where(v => expected.Contains(v))
            .Distinct()
            .Count();

        Assert.Equal(6, primaryOnly);
    }

    [Fact]
    public void PaymentMethodEnum_Has6Values()
    {
        var values = Enum.GetValues<PaymentMethod>().ToList();
        Assert.Equal(6, values.Count);

        var names = values.Select(v => v.ToString()).ToHashSet();
        Assert.Contains("Cash", names);
        Assert.Contains("BankTransfer", names);
        Assert.Contains("Card", names);
        Assert.Contains("Other", names);
        Assert.Contains("Cheque", names);
        Assert.Contains("MobilePayment", names);
    }

    [Fact]
    public void InvoiceStatusEnum_Has8Values()
    {
        var values = Enum.GetValues<InvoiceStatus>().ToList();
        Assert.Equal(8, values.Count);

        var names = values.Select(v => v.ToString()).ToHashSet();
        Assert.Contains("Draft", names);
        Assert.Contains("Confirmed", names);
        Assert.Contains("Unpaid", names);
        Assert.Contains("PartiallyPaid", names);
        Assert.Contains("Paid", names);
        Assert.Contains("Cancelled", names);
        Assert.Contains("Voided", names);
        Assert.Contains("Overdue", names);
    }

    [Fact]
    public void ExpenseCategoryEnum_Has10Values()
    {
        var values = Enum.GetValues<ExpenseCategory>().ToList();
        Assert.Equal(10, values.Count);

        var names = values.Select(v => v.ToString()).ToHashSet();
        Assert.Contains("Feed", names);
        Assert.Contains("Veterinary", names);
        Assert.Contains("Medication", names);
        Assert.Contains("Transportation", names);
        Assert.Contains("Labor", names);
        Assert.Contains("Insurance", names);
        Assert.Contains("Maintenance", names);
        Assert.Contains("Utilities", names);
        Assert.Contains("Equipment", names);
        Assert.Contains("Other", names);
    }

    [Fact]
    public void FormattedPdfWriter_ImplementsIPdfGenerator()
    {
        Assert.True(typeof(IPdfGenerator).IsAssignableFrom(typeof(FormattedPdfWriter)));
        Assert.Contains(typeof(IPdfGenerator), typeof(FormattedPdfWriter).GetInterfaces());
    }

    [Fact]
    public void ProtectedDocStorage_ImplementsInterface()
    {
        Assert.True(typeof(IProtectedDocumentStorage).IsAssignableFrom(typeof(ProtectedDocumentStorage)));
        Assert.Contains(typeof(IProtectedDocumentStorage), typeof(ProtectedDocumentStorage).GetInterfaces());
    }

    [Fact]
    public void Services_RegisteredInServiceCollection()
    {
        var configData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=Phase2SmokeTest;Trusted_Connection=true;MultipleActiveResultSets=true" },
            { "ProtectedStorage:Root", "./App_Data/Documents" }
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddInfrastructure(config);
        services.AddApplicationServices();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ISequenceGenerator>());
        Assert.NotNull(provider.GetRequiredService<IPdfGenerator>());
        Assert.NotNull(provider.GetRequiredService<IProtectedDocumentStorage>());
        Assert.NotNull(provider.GetRequiredService<DocumentNumberGenerator>());
    }

    [Fact]
    public void AllControllers_HaveAuthorizeAttribute()
    {
        var webAssembly = typeof(LivestockManager.Web.Controllers.HomeController).Assembly;

        var controllerTypes = webAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .ToList();

        var skipped = new List<string>();

        foreach (var ct in controllerTypes)
        {
            if (ct.Name == "AccountController")
            {
                var anonActions = ct.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                                typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType))
                    .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null)
                    .ToList();
                if (anonActions.Count == 0)
                {
                    skipped.Add($"{ct.Name}: expected AllowAnonymous on login actions");
                }
                continue;
            }

            var classAttr = ct.GetCustomAttribute<AuthorizeAttribute>();
            if (classAttr != null)
                continue;

            var publicActions = ct.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType) ||
                            typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType))
                .ToList();

            foreach (var act in publicActions)
            {
                var aa = act.GetCustomAttribute<AllowAnonymousAttribute>();
                var at = act.GetCustomAttribute<AuthorizeAttribute>();
                if (aa == null && at == null)
                {
                    skipped.Add($"{ct.Name}.{act.Name} has no [Authorize] or [AllowAnonymous]");
                }
            }
        }

        Assert.Empty(skipped);
    }
}
