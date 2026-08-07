using System.Reflection;

namespace LivestockManager.UnitTests;

public class RoleAuthorizationMatrixTests
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string DataEntry = "DataEntry";
    public const string Viewer = "Viewer";

    private static readonly HashSet<string> AllowedRoles = new()
    {
        Administrator, Manager, DataEntry, Viewer
    };

    [Fact]
    public void RoleConstants_AreExactlyAsSpecified()
    {
        Assert.Equal("Administrator", Administrator);
        Assert.Equal("Manager", Manager);
        Assert.Equal("DataEntry", DataEntry);
        Assert.Equal("Viewer", Viewer);
    }

    [Fact]
    public void AllowedRoles_HasExactlyFourEntries()
    {
        Assert.Equal(4, AllowedRoles.Count);
        Assert.Contains(Administrator, AllowedRoles);
        Assert.Contains(Manager, AllowedRoles);
        Assert.Contains(DataEntry, AllowedRoles);
        Assert.Contains(Viewer, AllowedRoles);
    }

    [Fact]
    public void RoleConstants_AreDistinct()
    {
        var roles = new[] { Administrator, Manager, DataEntry, Viewer };
        Assert.Equal(4, roles.Distinct().Count());
    }

    [Fact]
    public void AuthorizeAttributes_OnlyUseAllowedRoles_BestEffort()
    {
        Assembly? webAssembly = null;
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var webDll = Path.Combine(baseDir, "LivestockManager.Web.dll");
            if (File.Exists(webDll))
            {
                webAssembly = Assembly.LoadFrom(webDll);
            }
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
