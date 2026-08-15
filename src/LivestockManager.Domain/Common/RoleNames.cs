namespace LivestockManager.Domain.Common;

public static class RoleNames
{
    public const string DataEntry = "DataEntry";
    public const string FarmManager = "FarmManager";
    public const string Accounts = "Accounts";
    public const string OperationsManager = "OperationsManager";
    public const string CompanyAdministrator = "CompanyAdministrator";
    public const string SystemAdministrator = "SystemAdministrator";

    public const string Administrator = CompanyAdministrator;

    public static readonly string[] All =
    [
        DataEntry,
        FarmManager,
        Accounts,
        OperationsManager,
        CompanyAdministrator,
        SystemAdministrator
    ];

    public static readonly string[] CompanySafeAssignable =
    [
        DataEntry,
        FarmManager,
        Accounts,
        OperationsManager,
        CompanyAdministrator
    ];

    public static string GetDisplayName(string? roleName)
        => roleName switch
        {
            DataEntry => "Employee",
            FarmManager => "Farm Manager",
            Accounts => "Accounting",
            OperationsManager => "Manager",
            CompanyAdministrator => "Admin",
            SystemAdministrator => "System Admin",
            null or "" => string.Empty,
            _ => roleName
        };
}
