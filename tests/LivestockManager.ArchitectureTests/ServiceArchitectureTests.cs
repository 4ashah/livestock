using System.Reflection;
using Xunit;

namespace LivestockManager.ArchitectureTests;

public class ServiceArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(LivestockManager.Application.ServiceCollectionExtensions).Assembly;

    [Fact]
    public void CompanyOwnedServices_GetById_HasCompanyIdParameter()
    {
        var serviceInterfaceTypes = new[]
        {
            typeof(LivestockManager.Application.Services.Livestock.ILivestockService),
            typeof(LivestockManager.Application.Services.Invoices.IInvoiceService),
            typeof(LivestockManager.Application.Services.Sales.ISaleService),
            typeof(LivestockManager.Application.Services.Payments.IPaymentService),
            typeof(LivestockManager.Application.Services.Receipts.IReceiptService),
            typeof(LivestockManager.Application.Services.Purchases.IPurchaseService),
            typeof(LivestockManager.Application.Services.Expenses.IExpenseService),
            typeof(LivestockManager.Application.Services.Customers.ICustomerService),
            typeof(LivestockManager.Application.Services.Suppliers.ISupplierService),
            typeof(LivestockManager.Application.Services.Farms.IFarmService)
        };

        foreach (var svcType in serviceInterfaceTypes)
        {
            var getByIdMethod = svcType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name.Equals("GetByIdAsync", StringComparison.Ordinal) ||
                    m.Name.Equals("GetById", StringComparison.Ordinal));

            Assert.NotNull(getByIdMethod);

            var parameters = getByIdMethod!.GetParameters().ToList();
            Assert.NotEmpty(parameters);

            bool hasCompanyId = parameters.Any(p =>
                p.Name != null &&
                p.Name.Equals("companyId", StringComparison.OrdinalIgnoreCase) &&
                (p.ParameterType == typeof(Guid) || p.ParameterType == typeof(Guid?)));

            Assert.True(hasCompanyId,
                $"{svcType.FullName}.{getByIdMethod.Name} should include Guid companyId parameter. " +
                $"Parameters found: {string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
        }
    }

    [Fact]
    public void NoPlaceholder_Test1_MethodsInTestProjects()
    {
        var testAssemblies = new List<System.Reflection.Assembly>
        {
            typeof(LayerReferenceTests).Assembly,
            typeof(LivestockManager.IntegrationTests.IntegrationBootTests).Assembly,
            typeof(LivestockManager.EndToEndTests.E2eRemediationChecklist).Assembly
        };

        try
        {
            var unitTestsAsm = System.Reflection.Assembly.Load("LivestockManager.UnitTests");
            if (unitTestsAsm != null) testAssemblies.Add(unitTestsAsm);
        }
        catch
        {
        }

        int totalTest1 = 0;
        foreach (var asm in testAssemblies)
        {
            foreach (var t in asm.GetTypes())
            {
                var methods = t.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (var m in methods)
                {
                    if (m.Name.Equals("Test1", StringComparison.Ordinal))
                    {
                        totalTest1++;
                    }
                }
            }
        }

        Assert.Equal(0, totalTest1);
    }
}
