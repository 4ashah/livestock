# ROLE RIGHTS IMPLEMENTATION REPORT

## Summary
This report records the implementation of the final six-role rights model and the retirement of the historical `Viewer` role.

## Original Seven Roles
- Viewer
- DataEntry
- FarmManager
- Accounts
- OperationsManager
- CompanyAdministrator
- SystemAdministrator

## Viewer Removal Decision
`Viewer` is retired from the active application model. Ordinary read/basic-entry access now belongs to `DataEntry` (display name: Employee). Historical audit evidence may still mention `Viewer`, but current runtime source, selectors, navigation, and seed data do not expose it as an available role.

## Existing Viewer User Count
- Initial development-database Viewer assignment count: `1`
- Final development-database Viewer assignment count: `0`

## User Migration Actions
Development database evidence:

| User ID | Company ID | Account | Previous role state | Migration action | Result |
|---|---|---|---|---|---|
| `4F9F8091-600B-46A0-464D-08DEF4774455` | `CE88B724-FF2E-4DBA-B462-C3817B38871A` | `viewer@livestock.dev` | `Viewer` only | Migrated to `DataEntry`, disabled, pending administrator review | `DataEntry`, `IsEnabled = 0` |

Audit text used by the retirement flow:
- `Viewer role retired; account migrated to Employee and disabled pending administrator review.`
- `Viewer role retired; account migrated to Employee and disabled pending SystemAdministrator review because no valid company assignment exists.`

## Final Six Roles

| Internal role | Display name | Scope |
|---|---|---|
| `DataEntry` | Employee | Company/farm scoped operational read/basic-entry access |
| `FarmManager` | Farm Manager | Assigned farms unless broader company scope is explicitly granted |
| `Accounts` | Accounting | Company-scoped finance and document workflow access |
| `OperationsManager` | Manager | Company-wide operational access plus approved financial visibility |
| `CompanyAdministrator` | Admin | Full company-scoped operational, financial, user, settings, and audit access |
| `SystemAdministrator` | System Admin | Cross-company/global administrative access |

## Policy Matrix

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
| `CanManageCustomers` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageSuppliers` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewDocuments` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanUploadDocuments` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanCreateSales` | FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanReverseSales` | CompanyAdministrator, SystemAdministrator |
| `CanManagePurchaseInvoices` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewInvoices` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageInvoices` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordPayments` | Accounts, CompanyAdministrator, SystemAdministrator |
| `CanReversePayments` | Accounts, CompanyAdministrator, SystemAdministrator |
| `CanGenerateReceipts` | Accounts, CompanyAdministrator, SystemAdministrator |
| `CanManageExpenses` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanRecordLosses` | FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewOperationalReports` | DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanViewFinancialReports` | Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator |
| `CanManageCompany` | CompanyAdministrator, SystemAdministrator |
| `CanViewAuditLogs` | CompanyAdministrator, SystemAdministrator |
| `CanManageUsers` | CompanyAdministrator, SystemAdministrator |
| `CanManageSystem` | SystemAdministrator |

## Farm / Company Scope
- `DataEntry` remains company/farm scoped and does not gain management access to farms, finance, users, settings, or audit logs.
- `FarmManager` stays operational and farm-scoped unless the company explicitly grants broader scope.
- `Accounts` stays company-scoped and does not gain farm management or company/user administration.
- `OperationsManager` is company-wide but not cross-company.
- `CompanyAdministrator` is company-scoped only.
- `SystemAdministrator` is the only cross-company role and still requires explicit policy handling.

## Desktop / Mobile Navigation Changes
- Viewer removed from desktop and mobile navigation conditions.
- Employee/`DataEntry` retains operational pages that match the final rights model.
- Financial menu items now depend on the final policy split rather than older broad role assumptions.
- User role selectors now expose exactly the six approved roles, with `System Admin` visible only to `SystemAdministrator`.

## Test Evidence
- Release build: `PASS` (`0 warnings / 0 errors`)
- Unit tests: `324 / 324 PASS`
- Integration tests: `15 / 15 PASS`
- Architecture tests: `60 / 60 PASS`
- Playwright / E2E: `39 / 39 PASS`
- Targeted login regression slice: `8 / 8 PASS`

## Final Commit
- Latest valid base commit before this feature finalization: `bb4f81f`
- Final implementation commit for this feature: `PENDING`

## Final Status
**SIX-ROLE RIGHTS MODEL READY FOR USER REVIEW**

This report does **not** mark the full application as production-approved.
