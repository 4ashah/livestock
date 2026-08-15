using System.Reflection;
using LivestockManager.Domain.Common;
using Xunit;

namespace LivestockManager.ArchitectureTests;

public class DomainCommonTests
{
    [Fact]
    public void RoleNames_AllArray_HasExactlySixDistinctRoles()
    {
        var allRoles = RoleNames.All;
        Assert.NotNull(allRoles);
        Assert.Equal(6, allRoles.Length);

        var distinct = allRoles.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(6, distinct.Count);

        var expected = new[]
        {
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        };

        foreach (var role in expected)
        {
            Assert.Contains(role, allRoles);
        }
    }

    [Fact]
    public void PermissionNames_AllArray_HasExactlyFiftySevenEntries()
    {
        var allPermissions = PermissionNames.All;
        Assert.NotNull(allPermissions);
        Assert.Equal(57, allPermissions.Length);

        var distinct = allPermissions.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(57, distinct.Count);
    }

    [Fact]
    public void RoleNames_SixRoleFields_AreConsistentWithAllArray()
    {
        var fieldRoles = new List<string>();

        var fields = typeof(RoleNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral && !f.IsInitOnly)
            .ToList();

        foreach (var f in fields)
        {
            if (f.Name.Equals("Administrator", StringComparison.Ordinal))
                continue;

            var val = f.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(val))
            {
                fieldRoles.Add(val);
            }
        }

        var distinctFieldRoles = fieldRoles.Distinct(StringComparer.Ordinal).ToList();

        Assert.Equal(6, distinctFieldRoles.Count);
        Assert.Equal(RoleNames.All.OrderBy(r => r, StringComparer.Ordinal),
            distinctFieldRoles.OrderBy(r => r, StringComparer.Ordinal));
    }
}
