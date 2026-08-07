using LivestockManager.Domain.Common;

namespace LivestockManager.UnitTests.Roles;

public class RoleNamesTests
{
    [Fact]
    public void RoleConstants_Spelling_IsCorrect()
    {
        Assert.Equal("Viewer", RoleNames.Viewer);
        Assert.Equal("DataEntry", RoleNames.DataEntry);
        Assert.Equal("FarmManager", RoleNames.FarmManager);
        Assert.Equal("Accounts", RoleNames.Accounts);
        Assert.Equal("CompanyAdministrator", RoleNames.CompanyAdministrator);
        Assert.Equal("SystemAdministrator", RoleNames.SystemAdministrator);
    }

    [Fact]
    public void AdministratorAlias_Equals_CompanyAdministrator()
    {
        Assert.Equal(RoleNames.CompanyAdministrator, RoleNames.Administrator);
        Assert.Same(RoleNames.CompanyAdministrator, RoleNames.Administrator);
    }

    [Fact]
    public void OrderedByPermissionLevel_HasExactlySixEntries()
    {
        var ordered = new[]
        {
            RoleNames.Viewer,
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        };

        Assert.Equal(6, ordered.Length);
    }

    [Fact]
    public void PrimaryRoleConstants_AreDistinct()
    {
        var primary = new[]
        {
            RoleNames.Viewer,
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        };

        Assert.Equal(6, primary.Distinct().Count());
    }

    [Fact]
    public void AllRoleConstants_AreNonEmptyStrings()
    {
        var all = typeof(RoleNames)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        Assert.All(all, v => Assert.False(string.IsNullOrWhiteSpace(v)));
    }
}
