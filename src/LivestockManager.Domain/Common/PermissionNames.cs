namespace LivestockManager.Domain.Common;

public static class PermissionNames
{
    public static class Farms
    {
        public const string View = "Farms.View";
        public const string Create = "Farms.Create";
        public const string Details = "Farms.Details";
        public const string Edit = "Farms.Edit";
        public const string Archive = "Farms.Archive";

        public static readonly string[] All =
        [
            View,
            Create,
            Details,
            Edit,
            Archive
        ];
    }

    public static class Livestock
    {
        public const string View = "Livestock.View";
        public const string Register = "Livestock.Register";
        public const string Export = "Livestock.Export";
        public const string Details = "Livestock.Details";
        public const string Edit = "Livestock.Edit";
        public const string AddWeight = "Livestock.AddWeight";
        public const string Discharge = "Livestock.Discharge";
        public const string ExportCsv = "Livestock.ExportCsv";

        public static readonly string[] All =
        [
            View,
            Register,
            Export,
            Details,
            Edit,
            AddWeight,
            Discharge,
            ExportCsv
        ];
    }

    public static class StockAddition
    {
        public const string RecordPurchase = "StockAddition.RecordPurchase";
        public const string RecordNewborn = "StockAddition.RecordNewborn";

        public static readonly string[] All =
        [
            RecordPurchase,
            RecordNewborn
        ];
    }

    public static class Customers
    {
        public const string Create = "Customers.Create";
        public const string Details = "Customers.Details";
        public const string Edit = "Customers.Edit";

        public static readonly string[] All =
        [
            Create,
            Details,
            Edit
        ];
    }

    public static class Suppliers
    {
        public const string Create = "Suppliers.Create";
        public const string Details = "Suppliers.Details";
        public const string Edit = "Suppliers.Edit";

        public static readonly string[] All =
        [
            Create,
            Details,
            Edit
        ];
    }

    public static class Documents
    {
        public const string View = "Documents.View";
        public const string Upload = "Documents.Upload";
        public const string Delete = "Documents.Delete";

        public static readonly string[] All =
        [
            View,
            Upload,
            Delete
        ];
    }

    public static class Sales
    {
        public const string View = "Sales.View";
        public const string Create = "Sales.Create";
        public const string Confirm = "Sales.Confirm";
        public const string Reverse = "Sales.Reverse";

        public static readonly string[] All =
        [
            View,
            Create,
            Confirm,
            Reverse
        ];
    }

    public static class PurchaseInvoices
    {
        public const string View = "PurchaseInvoices.View";
        public const string Create = "PurchaseInvoices.Create";

        public static readonly string[] All =
        [
            View,
            Create
        ];
    }

    public static class Invoices
    {
        public const string View = "Invoices.View";
        public const string DownloadPdf = "Invoices.DownloadPdf";
        public const string CreateFromInvoice = "Invoices.CreateFromInvoice";
        public const string Confirm = "Invoices.Confirm";
        public const string Void = "Invoices.Void";

        public static readonly string[] All =
        [
            View,
            DownloadPdf,
            CreateFromInvoice,
            Confirm,
            Void
        ];
    }

    public static class Payments
    {
        public const string View = "Payments.View";
        public const string Record = "Payments.Record";
        public const string Reverse = "Payments.Reverse";

        public static readonly string[] All =
        [
            View,
            Record,
            Reverse
        ];
    }

    public static class Receipts
    {
        public const string View = "Receipts.View";
        public const string Download = "Receipts.Download";
        public const string GenerateFromPayment = "Receipts.GenerateFromPayment";

        public static readonly string[] All =
        [
            View,
            Download,
            GenerateFromPayment
        ];
    }

    public static class Expenses
    {
        public const string View = "Expenses.View";
        public const string ExportCsv = "Expenses.ExportCsv";
        public const string Create = "Expenses.Create";

        public static readonly string[] All =
        [
            View,
            ExportCsv,
            Create
        ];
    }

    public static class Losses
    {
        public const string View = "Losses.View";
        public const string Record = "Losses.Record";

        public static readonly string[] All =
        [
            View,
            Record
        ];
    }

    public static class Reports
    {
        public const string ActiveLivestock = "Reports.ActiveLivestock";
        public const string SalesByPeriod = "Reports.SalesByPeriod";
        public const string LivestockProfitability = "Reports.LivestockProfitability";
        public const string ProfitAndLoss = "Reports.ProfitAndLoss";
        public const string ProfitAndLossStatements = "Reports.ProfitAndLossStatements";

        public static readonly string[] All =
        [
            ActiveLivestock,
            SalesByPeriod,
            LivestockProfitability,
            ProfitAndLoss,
            ProfitAndLossStatements
        ];
    }

    public static class Administration
    {
        public const string AuditLogs = "Administration.AuditLogs";
        public const string Users = "Administration.Users";
        public const string Companies = "Administration.Companies";
        public const string Settings = "Administration.Settings";

        public static readonly string[] All =
        [
            AuditLogs,
            Users,
            Companies,
            Settings
        ];
    }

    public static class Users
    {
        public const string AssignRole = "Users.AssignRole";

        public static readonly string[] All =
        [
            AssignRole
        ];
    }

    public static class Settings
    {
        public const string Edit = "Settings.Edit";

        public static readonly string[] All =
        [
            Edit
        ];
    }

    public static readonly string[] All =
    [
        .. Farms.All,
        .. Livestock.All,
        .. StockAddition.All,
        .. Customers.All,
        .. Suppliers.All,
        .. Documents.All,
        .. Sales.All,
        .. PurchaseInvoices.All,
        .. Invoices.All,
        .. Payments.All,
        .. Receipts.All,
        .. Expenses.All,
        .. Losses.All,
        .. Reports.All,
        .. Administration.All,
        .. Users.All,
        .. Settings.All
    ];
}
