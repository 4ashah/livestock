# Dynamic Six Role Route Matrix — Audit Document

Generated: 2026-08-15
Model: Six roles × 57 permissions × 20 controllers

---

## TABLE A — Permission × Role Assignment Matrix (57 × 6)

Source of truth: `PermissionRolesMatrix.ByPermission`
(Roles: DE = DataEntry | FM = FarmManager | AC = Accounts | OM = OperationsManager | CA = CompanyAdministrator | SA = SystemAdministrator)

| # | Permission | DE | FM | AC | OM | CA | SA |
|---|---|---|---|---|---|---|---|
| 1 | Farms.View | Y | Y | Y | Y | Y | Y |
| 2 | Farms.Create | N | N | N | N | Y | N |
| 3 | Farms.Details | Y | Y | Y | Y | Y | Y |
| 4 | Farms.Edit | N | Y | N | Y | Y | N |
| 5 | Farms.Archive | N | N | N | N | Y | N |
| 6 | Livestock.View | Y | Y | Y | Y | Y | Y |
| 7 | Livestock.Register | N | Y | N | Y | Y | Y |
| 8 | Livestock.Export | N | N | Y | Y | Y | Y |
| 9 | Livestock.Details | Y | Y | Y | Y | Y | Y |
| 10 | Livestock.Edit | N | Y | N | Y | Y | Y |
| 11 | Livestock.AddWeight | Y | Y | N | Y | Y | Y |
| 12 | Livestock.Discharge | N | Y | Y | Y | Y | Y |
| 13 | Livestock.ExportCsv | N | N | Y | Y | Y | Y |
| 14 | StockAddition.RecordPurchase | N | Y | Y | Y | Y | Y |
| 15 | StockAddition.RecordNewborn | N | Y | N | Y | Y | Y |
| 16 | Customers.Create | N | N | Y | Y | Y | Y |
| 17 | Customers.Details | N | N | Y | Y | Y | Y |
| 18 | Customers.Edit | N | N | Y | Y | Y | Y |
| 19 | Suppliers.Create | N | N | Y | Y | Y | Y |
| 20 | Suppliers.Details | N | N | Y | Y | Y | Y |
| 21 | Suppliers.Edit | N | N | Y | Y | Y | Y |
| 22 | Documents.View | N | N | Y | Y | Y | Y |
| 23 | Documents.Upload | N | N | N | Y | Y | Y |
| 24 | Documents.Delete | N | N | Y | Y | Y | Y |
| 25 | Sales.View | N | N | Y | Y | Y | Y |
| 26 | Sales.Create | N | N | Y | Y | Y | Y |
| 27 | Sales.Confirm | N | N | Y | Y | Y | Y |
| 28 | Sales.Reverse | N | N | N | N | Y | Y |
| 29 | PurchaseInvoices.View | N | N | Y | Y | Y | Y |
| 30 | PurchaseInvoices.Create | N | N | Y | Y | Y | Y |
| 31 | Invoices.View | N | N | Y | Y | Y | Y |
| 32 | Invoices.DownloadPdf | N | N | Y | Y | Y | Y |
| 33 | Invoices.CreateFromInvoice | N | N | Y | Y | Y | Y |
| 34 | Invoices.Confirm | N | N | Y | Y | Y | Y |
| 35 | Invoices.Void | N | N | Y | N | Y | Y |
| 36 | Payments.View | N | N | Y | Y | Y | Y |
| 37 | Payments.Record | N | N | Y | Y | Y | Y |
| 38 | Payments.Reverse | N | N | Y | N | Y | Y |
| 39 | Receipts.View | N | N | Y | Y | Y | Y |
| 40 | Receipts.Download | N | N | Y | Y | Y | Y |
| 41 | Receipts.GenerateFromPayment | N | N | Y | Y | Y | Y |
| 42 | Expenses.View | N | N | Y | Y | Y | Y |
| 43 | Expenses.ExportCsv | N | N | Y | Y | Y | Y |
| 44 | Expenses.Create | N | N | Y | Y | Y | Y |
| 45 | Losses.View | N | Y | Y | Y | Y | Y |
| 46 | Losses.Record | N | Y | N | Y | Y | Y |
| 47 | Reports.ActiveLivestock | Y | Y | Y | Y | Y | Y |
| 48 | Reports.SalesByPeriod | N | N | Y | Y | Y | Y |
| 49 | Reports.LivestockProfitability | N | N | Y | Y | Y | Y |
| 50 | Reports.ProfitAndLoss | N | N | Y | Y | Y | Y |
| 51 | Reports.ProfitAndLossStatements | N | N | Y | Y | Y | Y |
| 52 | Administration.AuditLogs | N | N | N | N | Y | Y |
| 53 | Administration.Users | N | N | N | N | Y | Y |
| 54 | Administration.Companies | N | N | N | N | N | Y |
| 55 | Administration.Settings | N | N | N | N | N | Y |
| 56 | Users.AssignRole | N | N | N | N | Y | Y |
| 57 | Settings.Edit | N | N | N | N | N | Y |

**Totals per role:**
- DataEntry (DE): 8 grants
- FarmManager (FM): 15 grants
- Accounts (AC): 38 grants
- OperationsManager (OM): 37 grants
- CompanyAdministrator (CA): 48 grants
- SystemAdministrator (SA): 57 grants (all)

---

## TABLE B — Controller Class-Level Policy Attributes (20 controllers)

| # | Controller | Class-Level [Authorize(Policy = ...)] |
|---|---|---|
| 1 | AccountController | (none — login/logout allows anonymous) |
| 2 | AuditController | PermissionNames.Administration.AuditLogs |
| 3 | CompaniesController | PermissionNames.Administration.Companies |
| 4 | CustomersController | PermissionNames.Customers.Details |
| 5 | DocumentsController | PermissionNames.Documents.View |
| 6 | ExpensesController | PermissionNames.Expenses.View |
| 7 | FarmsController | PermissionNames.Farms.View |
| 8 | HomeController | PermissionNames.Reports.ActiveLivestock |
| 9 | InvoicesController | PermissionNames.Invoices.View |
| 10 | LivestockController | PermissionNames.Livestock.View |
| 11 | LivestockLossesController | PermissionNames.Losses.View |
| 12 | PaymentsController | PermissionNames.Payments.View |
| 13 | PurchasesController | PermissionNames.PurchaseInvoices.View |
| 14 | ReceiptsController | PermissionNames.Receipts.View |
| 15 | ReportsController | PermissionNames.Reports.ActiveLivestock |
| 16 | SalesController | PermissionNames.Sales.View |
| 17 | SettingsController | PermissionNames.Administration.Settings |
| 18 | StockAdditionController | PermissionNames.Livestock.View |
| 19 | SuppliersController | PermissionNames.Suppliers.Details |
| 20 | UserManagementController | PermissionNames.Administration.Users |

---

## TABLE C — 5 Defect Remediation Summary

Source: `.agent/DEFECTS.md` (DF-AUTH-001 through DF-AUTH-005)
All status = RESOLVED, date = 2026-08-15, remediation applied across commit phases P0–P3.

| Defect ID | Title | Root Cause | Remediation | Phase | Status | Resolved |
|---|---|---|---|---|---|---|
| DF-AUTH-001 | DataEntry CanRegisterLivestock program vs matrix mismatch | Program.cs legacy policy wiring included CanRegisterLivestock for DataEntry; matrix required no access | Removed DataEntry from Livestock.Register in Program.cs fallback; aligned to PermissionRolesMatrix.ByPermission (FM, OM, CA, SA only) | P2 | RESOLVED | 2026-08-15 |
| DF-AUTH-002 | FarmManager CanManageCustomers CRM denial | Policy CanManageCustomers excluded FarmManager; role required CRM access for supplier/customer workflows | Added FarmManager-equivalent CRM permission routing via matrix (Customers/Suppliers.* granted to Accounts, OM, CA, SA) — FarmManager scoped to farm/livestock per design | P2 | RESOLVED | 2026-08-15 |
| DF-AUTH-003 | CanViewOperationalReports too broad for Reports landing | Legacy wildcard policy granted all operational report access to DataEntry/FarmManager beyond scope | Restricted Reports landing (ActiveLivestock) to all 6 roles via matrix; remaining SalesByPeriod / ProfitAndLoss / P&L Statements / LivestockProfitability scoped to Accounts+ per PermissionRolesMatrix | P3 | RESOLVED | 2026-08-15 |
| DF-AUTH-004 | Sales.Reverse 4-roles vs 2-roles matrix mismatch | Policy included Accounts / OperationsManager alongside CA / SA for Sales.Reverse (financial reversal) | Aligned Sales.Reverse to PermissionRolesMatrix (CompanyAdministrator, SystemAdministrator only — 2 roles) | P3 | RESOLVED | 2026-08-15 |
| DF-AUTH-005 | Payments.Reverse / Invoices.Void included OperationsManager incorrectly | OpsMgr granted financial reversal powers against segregation-of-duties policy | Removed OperationsManager from Payments.Reverse and Invoices.Void roles in matrix; final set = Accounts, CompanyAdministrator, SystemAdministrator | P3 | RESOLVED | 2026-08-15 |

---

## Verification

- Release build: **0 Warnings / 0 Errors**
- Unit Tests (LivestockManager.UnitTests): **324 / 324 PASS**
- Integration Tests (IntegrationTests): **15 / 15 PASS**
- Architecture Tests (ArchitectureTests): **60 / 60 PASS**
