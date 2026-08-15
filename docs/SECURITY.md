# SECURITY

## Current Role Model
The active application role model contains exactly six business roles:
- `DataEntry` -> Employee
- `FarmManager` -> Farm Manager
- `Accounts` -> Accounting
- `OperationsManager` -> Manager
- `CompanyAdministrator` -> Admin
- `SystemAdministrator` -> System Admin

`Viewer` is retired. Any remaining mention of `Viewer` in audit material is historical only.

## Authorization Rules
- Authorization is policy-based through `PolicyNames` and role constants in `RoleNames`.
- Navigation is a usability layer only; every controller action must enforce authorization independently.
- `CompanyAdministrator` may assign only company-safe roles.
- `SystemAdministrator` is the only role with global system-management authority.

## Final Policy Summary
| Policy | Allowed roles |
|---|---|
| `CanViewFarms` | DataEntry, FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageFarms` | OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewLivestock` | DataEntry, FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRegisterLivestock` | DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageLivestock` | FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordWeight` | DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanDischargeLivestock` | FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordStockPurchase` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordNewborn` | DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageCustomers` / `CanManageSuppliers` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewDocuments` / `CanUploadDocuments` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanCreateSales` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanReverseSales` | CompanyAdministrator, SystemAdministrator |
| `CanManagePurchaseInvoices` / `CanViewInvoices` / `CanManageInvoices` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordPayments` / `CanReversePayments` / `CanGenerateReceipts` | Accounts, CompanyAdministrator, SystemAdministrator |
| `CanManageExpenses` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordLosses` | FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewOperationalReports` | DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewFinancialReports` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageCompany` / `CanViewAuditLogs` / `CanManageUsers` | CompanyAdministrator, SystemAdministrator |
| `CanManageSystem` | SystemAdministrator |

## Scope Model
- Employee/DataEntry is operational and scoped to the assigned company/farm context.
- FarmManager remains operational and farm-scoped unless broader scope is explicitly granted.
- Accounts remains company-scoped and cannot manage farms, companies, or global users.
- OperationsManager is company-wide, not cross-company.
- CompanyAdministrator is company-scoped only.
- SystemAdministrator is cross-company/global and every sensitive action must be audited.
