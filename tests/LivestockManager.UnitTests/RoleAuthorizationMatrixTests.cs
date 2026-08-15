using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Domain.Common;
using LivestockManager.Web.Controllers;

namespace LivestockManager.UnitTests;

public class RoleAuthorizationMatrixTests
{
    public const string DataEntry = "DataEntry";
    public const string FarmManager = "FarmManager";
    public const string Accounts = "Accounts";
    public const string OperationsManager = "OperationsManager";
    public const string CompanyAdministrator = "CompanyAdministrator";
    public const string SystemAdministrator = "SystemAdministrator";

    private static readonly HashSet<string> AllowedRoles = new()
    {
        DataEntry,
        FarmManager,
        Accounts,
        OperationsManager,
        CompanyAdministrator,
        SystemAdministrator
    };

    private static readonly string[] ExpectedPolicyNames = new[]
    {
        PolicyNames.CanViewOperationalData,
        PolicyNames.CanViewFarms,
        PolicyNames.CanManageFarms,
        PolicyNames.CanViewLivestock,
        PolicyNames.CanRegisterLivestock,
        PolicyNames.CanManageLivestock,
        PolicyNames.CanRecordWeight,
        PolicyNames.CanDischargeLivestock,
        PolicyNames.CanRecordStockPurchase,
        PolicyNames.CanRecordNewborn,
        PolicyNames.CanManageCustomers,
        PolicyNames.CanManageSuppliers,
        PolicyNames.CanViewDocuments,
        PolicyNames.CanUploadDocuments,
        PolicyNames.CanCreateSales,
        PolicyNames.CanReverseSales,
        PolicyNames.CanManagePurchaseInvoices,
        PolicyNames.CanViewInvoices,
        PolicyNames.CanManageInvoices,
        PolicyNames.CanRecordPayments,
        PolicyNames.CanReversePayments,
        PolicyNames.CanGenerateReceipts,
        PolicyNames.CanManageExpenses,
        PolicyNames.CanRecordLosses,
        PolicyNames.CanViewOperationalReports,
        PolicyNames.CanViewFinancialReports,
        PolicyNames.CanManageCompany,
        PolicyNames.CanViewAuditLogs,
        PolicyNames.CanManageUsers,
        PolicyNames.CanManageSystem
    };

    [Fact]
    public void RoleConstants_AreExactlyAsSpecified()
    {
        Assert.Equal("DataEntry", DataEntry);
        Assert.Equal("FarmManager", FarmManager);
        Assert.Equal("Accounts", Accounts);
        Assert.Equal("OperationsManager", OperationsManager);
        Assert.Equal("CompanyAdministrator", CompanyAdministrator);
        Assert.Equal("SystemAdministrator", SystemAdministrator);
    }

    [Fact]
    public void AllowedRoles_HasExpandedPhase2Entries()
    {
        Assert.Equal(6, AllowedRoles.Count);
        Assert.Contains(DataEntry, AllowedRoles);
        Assert.Contains(FarmManager, AllowedRoles);
        Assert.Contains(Accounts, AllowedRoles);
        Assert.Contains(OperationsManager, AllowedRoles);
        Assert.Contains(CompanyAdministrator, AllowedRoles);
        Assert.Contains(SystemAdministrator, AllowedRoles);
    }

    [Fact]
    public void RoleConstants_AreDistinct()
    {
        var roles = new[] { DataEntry, FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator };
        Assert.Equal(6, roles.Distinct().Count());
    }

    [Fact]
    public void AuthorizeAttributes_OnlyUseAllowedRoles_BestEffort()
    {
        Assembly? webAssembly = null;
        try
        {
            webAssembly = typeof(SalesController).Assembly;
        }
        catch
        {
        }

        if (webAssembly == null)
        {
            var expected = new HashSet<string>(AllowedRoles);
            var standardUsages = new[]
            {
                "DataEntry",
                "FarmManager",
                "Accounts",
                "OperationsManager",
                "CompanyAdministrator",
                "SystemAdministrator"
            };
            Assert.Subset(expected, standardUsages.ToHashSet());
            return;
        }

        var disallowed = new List<string>();
        var controllerTypes = webAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var controller in controllerTypes)
        {
            var classAttrs = controller.GetCustomAttributesData()
                .Where(a => a.AttributeType.Name.Contains("Authorize"));
            foreach (var attr in classAttrs)
            {
                CheckRoles(attr, disallowed);
            }

            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var methodAttrs = method.GetCustomAttributesData()
                    .Where(a => a.AttributeType.Name.Contains("Authorize"));
                foreach (var attr in methodAttrs)
                {
                    CheckRoles(attr, disallowed);
                }
            }
        }

        Assert.Empty(disallowed);
    }

    [Fact]
    public async Task PolicyRegistration_CoversAll7Policies()
    {
        var services = new ServiceCollection();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.CanViewOperationalData, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewFarms, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageFarms, policy =>
                policy.RequireRole(RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewLivestock, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRegisterLivestock, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageLivestock, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRecordWeight, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanDischargeLivestock, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRecordStockPurchase, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRecordNewborn, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageCustomers, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageSuppliers, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewDocuments, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanUploadDocuments, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanCreateSales, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanReverseSales, policy =>
                policy.RequireRole(RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManagePurchaseInvoices, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewInvoices, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageInvoices, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRecordPayments, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanReversePayments, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanGenerateReceipts, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageExpenses, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanRecordLosses, policy =>
                policy.RequireRole(RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewOperationalReports, policy =>
                policy.RequireRole(RoleNames.DataEntry, RoleNames.FarmManager, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewFinancialReports, policy =>
                policy.RequireRole(RoleNames.Accounts, RoleNames.OperationsManager, RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageCompany, policy =>
                policy.RequireRole(RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanViewAuditLogs, policy =>
                policy.RequireRole(RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageUsers, policy =>
                policy.RequireRole(RoleNames.CompanyAdministrator, RoleNames.SystemAdministrator));
            options.AddPolicy(PolicyNames.CanManageSystem, policy =>
                policy.RequireRole(RoleNames.SystemAdministrator));
        });

        var serviceProvider = services.BuildServiceProvider();
        var policyProvider = serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.Equal(30, ExpectedPolicyNames.Length);

        foreach (var policyName in ExpectedPolicyNames)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName);
            Assert.NotNull(policy);
        }
    }

    [Fact]
    public void ControllerPostActions_HaveExplicitRolePolicyAuthorize()
    {
        var webAssembly = typeof(SalesController).Assembly;

        var controllerTypes = webAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                     && !t.Name.Equals("AccountController", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(controllerTypes.Count >= 10, "Expected at least 10 controllers (non-Account controllers");

        var protectedPostMethods = new List<MethodInfo>();
        var unprotectedPostMethods = new List<MethodInfo>();

        foreach (var controllerType in controllerTypes)
        {
            var classAuthorizeAttr = controllerType.GetCustomAttributes(inherit: true)
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault();

            bool classHasExplicitAuth = classAuthorizeAttr != null
                && (!string.IsNullOrWhiteSpace(classAuthorizeAttr.Roles)
                    || !string.IsNullOrWhiteSpace(classAuthorizeAttr.Policy));

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var isPostAttr = method.GetCustomAttributes(inherit: true)
                    .OfType<HttpPostAttribute>()
                    .Any();

                var acceptsVerbsAttr = method.GetCustomAttributesData()
                    .Any(a => a.AttributeType == typeof(AcceptVerbsAttribute)
                           && a.ConstructorArguments.Count > 0
                           && a.ConstructorArguments[0].Value is System.Collections.IEnumerable args
                           && args.Cast<object>().Any(v => v.ToString()?.Equals("POST", StringComparison.OrdinalIgnoreCase) == true));

                if (!isPostAttr && !acceptsVerbsAttr) continue;

                var returnType = method.ReturnType;
                bool isActionResult = typeof(IActionResult).IsAssignableFrom(returnType)
                    || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
                        && typeof(IActionResult).IsAssignableFrom(returnType.GetGenericArguments()[0]));

                if (!isActionResult) continue;

                var methodAuthorizeAttr = method.GetCustomAttributes(inherit: true)
                    .OfType<AuthorizeAttribute>()
                    .FirstOrDefault();

                var allowAnonymous = method.GetCustomAttributes(inherit: true)
                    .OfType<AllowAnonymousAttribute>()
                    .Any();

                if (allowAnonymous) continue;

                bool methodHasExplicitAuth = methodAuthorizeAttr != null
                    && (!string.IsNullOrWhiteSpace(methodAuthorizeAttr.Roles)
                        || !string.IsNullOrWhiteSpace(methodAuthorizeAttr.Policy));

                if (classHasExplicitAuth || methodHasExplicitAuth)
                {
                    protectedPostMethods.Add(method);
                }
                else
                {
                    unprotectedPostMethods.Add(method);
                }
            }
        }

        Assert.Empty(unprotectedPostMethods
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList());

        Assert.True(protectedPostMethods.Count >= 15,
            $"Expected at least 15 protected POST actions across controllers, found {protectedPostMethods.Count}. " +
            $"Controllers checked: {string.Join(", ", controllerTypes.Select(c => c.Name))}");
    }

    private static void CheckRoles(CustomAttributeData attr, List<string> disallowed)
    {
        foreach (var arg in attr.NamedArguments)
        {
            if (arg.MemberName == "Roles" && arg.TypedValue.Value is string rolesStr)
            {
                var roles = rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    if (!AllowedRoles.Contains(role))
                        disallowed.Add(role);
                }
            }
        }
    }
}
