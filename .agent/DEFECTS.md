# .agent/DEFECTS.md — REMOVE_VIEWER_AND_FINALIZE_SIX_ROLES

| # | Defect | Status | Notes |
|---|---|---|---|
| RVR-DFX-001 | Active role model still contained Viewer in runtime constants, demo seeds, login demo accounts, navigation, and selectors | FIXED | Final six-role model is now the active runtime model |
| RVR-DFX-002 | Controllers used legacy/broad string policies instead of centralized final `PolicyNames` constants | FIXED | Active controllers normalized onto the final policy set |
| RVR-DFX-003 | `StockAdditionController` was locked behind broad livestock-management policy and did not match purchase/newborn rights split | FIXED | Purchase endpoints use `CanRecordStockPurchase`; newborn endpoints use `CanRecordNewborn`; index/success routes use `CanViewLivestock` |
| RVR-DFX-004 | FarmManager still received financial visibility in some runtime paths | FIXED | Dashboard/nav/runtime checks tightened to final policy expectations |
| RVR-DFX-005 | Development database still had one Viewer-only user | FIXED | `viewer@livestock.dev` migrated to disabled `DataEntry`; Viewer assignment count now zero |
| RVR-DFX-006 | Viewer-retirement startup service hit a role update concurrency failure under integration startup | FIXED | Retirement role-seeding path now tolerates duplicate/concurrent role updates and integration tests pass |
| RVR-DFX-007 | Current docs/status files still described Viewer as an active role | FIXED | Active documentation now describes the six-role model and keeps Viewer only as a retired historical reference where needed |
| RVR-DFX-008 | Playwright/E2E verification for the final six-role pass was failing on stale email-only login selectors | FIXED | Updated E2E selectors to match the current `UserName` login field; targeted login slice passed `8/8` and the full suite passed `39/39` |

---

# DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION Defects

| # | Defect | Severity | Status | Notes |
|---|---|---|---|---|
| DF-AUTH-001 | DataEntry CanRegisterLivestock: Program.cs grants but PermissionRolesMatrix denies | HIGH | **RESOLVED — Phase3** | Mismatch resolved: `Livestock.Register` role-set in PermissionRolesMatrix + Program.cs policy wiring now = FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator only (DataEntry excluded per design). Verified in Release build: Unit 324/324 PASS, Integration 15/15 PASS, Architecture 60/60 PASS. Resolution date = 2026-08-15. Commit phase = P2 (matrix fix) → P3 (policy wiring). |
| DF-AUTH-002 | FarmManager CanManageCustomers: matrix denies CRM access | HIGH | **RESOLVED — Phase3** | Resolved: All runtime gates (nav, controller, views) now enforce the matrix: FarmManager has NO Customers/CRM access. CRM permissions (Customers.Create/Details/Edit + Suppliers.Create/Details/Edit) = Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator. FarmManager scoped correctly to farm/livestock-only per final six-role design. Resolution date = 2026-08-15. Commit phase = P2 (matrix fix) → P3 (policy wiring). |
| DF-AUTH-003 | CanViewOperationalReports too broad for Reports landing | MEDIUM | **RESOLVED — Phase3** | Resolved: Reports landing / Index now correctly gated behind `Reports.ActiveLivestock` (all 6 roles = DE, FM, AC, OM, CA, SA). Each sub-report is independently gated: `Reports.SalesByPeriod`, `Reports.LivestockProfitability`, `Reports.ProfitAndLoss`, `Reports.ProfitAndLossStatements` = Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator only (DataEntry/FarmManager excluded from financial reports). Resolution date = 2026-08-15. Commit phase = P3 (policy wiring + attribute normalization). |
| DF-AUTH-004 | Sales.Reverse 4-roles vs 2-roles: Accounts/OpsMgr can reverse financial sales | CRITICAL | **RESOLVED — Phase3** | Critical fix applied: `Sales.Reverse` role-set in PermissionRolesMatrix + Program.cs now = ONLY CompanyAdministrator, SystemAdministrator (2 roles). Accounts role explicitly REMOVED; OperationsManager explicitly REMOVED. Prevents financial tampering: Accounts must not be able to reverse confirmed Sales transactions. Segregation-of-duties enforced. Resolution date = 2026-08-15. Commit phase = P0 (DR-002 classification) → P3 (final code wiring + verification). |
| DF-AUTH-005 | Payments.Reverse / Invoices.Void OpsMgr included incorrectly | HIGH | **RESOLVED — Phase3** | Resolved per segregation-of-duties: OperationsManager (operational role) REMOVED from both `Payments.Reverse` and `Invoices.Void`. Final allowed role-set = Accounts, CompanyAdministrator, SystemAdministrator. OpsMgr is an operational/managerial role, not a financial role. Resolution date = 2026-08-15. Commit phase = P0 (DR-002 classification) → P3 (final code wiring + verification). |
