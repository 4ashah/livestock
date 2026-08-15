namespace LivestockManager.Domain.Common;

public static class PermissionRolesMatrix
{
    public static readonly Dictionary<string, string[]> ByPermission = new()
    {
        [PermissionNames.Farms.View] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Farms.Create] =
        [
            RoleNames.CompanyAdministrator
        ],
        [PermissionNames.Farms.Details] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Farms.Edit] =
        [
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator
        ],
        [PermissionNames.Farms.Archive] =
        [
            RoleNames.CompanyAdministrator
        ],

        [PermissionNames.Livestock.View] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.Register] =
        [
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.Export] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.Details] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.Edit] =
        [
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.AddWeight] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.Discharge] =
        [
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Livestock.ExportCsv] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.StockAddition.RecordPurchase] =
        [
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.StockAddition.RecordNewborn] =
        [
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Customers.Create] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Customers.Details] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Customers.Edit] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Suppliers.Create] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Suppliers.Details] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Suppliers.Edit] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Documents.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Documents.Upload] =
        [
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Documents.Delete] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Sales.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Sales.Create] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Sales.Confirm] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Sales.Reverse] =
        [
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.PurchaseInvoices.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.PurchaseInvoices.Create] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Invoices.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Invoices.DownloadPdf] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Invoices.CreateFromInvoice] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Invoices.Confirm] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Invoices.Void] =
        [
            RoleNames.Accounts,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Payments.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Payments.Record] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Payments.Reverse] =
        [
            RoleNames.Accounts,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Receipts.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Receipts.Download] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Receipts.GenerateFromPayment] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Expenses.View] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Expenses.ExportCsv] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Expenses.Create] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Losses.View] =
        [
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Losses.Record] =
        [
            RoleNames.FarmManager,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Reports.ActiveLivestock] =
        [
            RoleNames.DataEntry,
            RoleNames.FarmManager,
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Reports.SalesByPeriod] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Reports.LivestockProfitability] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Reports.ProfitAndLoss] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Reports.ProfitAndLossStatements] =
        [
            RoleNames.Accounts,
            RoleNames.OperationsManager,
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Administration.AuditLogs] =
        [
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Administration.Users] =
        [
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Administration.Companies] =
        [
            RoleNames.SystemAdministrator
        ],
        [PermissionNames.Administration.Settings] =
        [
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Users.AssignRole] =
        [
            RoleNames.CompanyAdministrator,
            RoleNames.SystemAdministrator
        ],

        [PermissionNames.Settings.Edit] =
        [
            RoleNames.SystemAdministrator
        ]
    };
}
