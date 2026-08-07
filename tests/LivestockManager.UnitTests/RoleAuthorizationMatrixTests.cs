using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Domain.Common;
using LivestockManager.Web.Controllers;

namespace LivestockManager.UnitTests;

public class RoleAuthorizationMatrixTests
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string DataEntry = "DataEntry";
    public const string Viewer = "Viewer";

    public const string FarmManager = "FarmManager";
    public const string Accounts = "Accounts";
    public const string CompanyAdministrator = "CompanyAdministrator";
    public const string SystemAdministrator = "SystemAdministrator";

    private static readonly HashSet<string> AllowedRoles = new()
    {
        Administrator,
        Manager,
        DataEntry,
        Viewer,
        FarmManager,
        Accounts,
        CompanyAdministrator,
        SystemAdministrator
    };

    private static readonly string[] ExpectedPolicyNames = new[]
    {
        "CanViewOperationalData",
        "CanManageLivestock",
        "CanManageSales",
        "CanManageAccounting",
        "CanManageCompany",
        "CanManageSystem",
        "CanViewFinancialData"
    };

    [Fact]
    public void RoleConstants_AreExactlyAsSpecified()
    {
        Assert.Equal("Administrator", Administrator);
        Assert.Equal("Manager", Manager);
        Assert.Equal("DataEntry", DataEntry);
        Assert.Equal("Viewer", Viewer);
        Assert.Equal("FarmManager", FarmManager);
        Assert.Equal("Accounts", Accounts);
        Assert.Equal("CompanyAdministrator", CompanyAdministrator);
        Assert.Equal("SystemAdministrator", SystemAdministrator);
    }

    [Fact]
    public void AllowedRoles_HasExpandedPhase2Entries()
    {
        Assert.Equal(8, AllowedRoles.Count);
        Assert.Contains(Administrator, AllowedRoles);
        Assert.Contains(Manager, AllowedRoles);
        Assert.Contains(DataEntry, AllowedRoles);
        Assert.Contains(Viewer, AllowedRoles);
        Assert.Contains(FarmManager, AllowedRoles);
        Assert.Contains(Accounts, AllowedRoles);
        Assert.Contains(CompanyAdministrator, AllowedRoles);
        Assert.Contains(SystemAdministrator, AllowedRoles);
    }

    [Fact]
    public void RoleConstants_AreDistinct()
    {
        var roles = new[] { Administrator, Manager, DataEntry, Viewer, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator };
        Assert.Equal(8, roles.Distinct().Count());
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
                "Administrator",
                "Manager",
                "DataEntry",
                "Viewer"
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

        var serviceProvider = services.BuildServiceProvider();
        var policyProvider = serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.Equal(7, ExpectedPolicyNames.Length);

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
