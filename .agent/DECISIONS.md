# .agent/DECISIONS.md — REMOVE_VIEWER_AND_FINALIZE_SIX_ROLES

| # | Decision | Choice | Rationale |
|---|---|---|---|
| D1 | Final role model | Enforce exactly six business roles and remove Viewer from active constants, selectors, policies, navigation, and seed data | Matches the approved role model and prevents stale low-privilege aliases from drifting back into the runtime |
| D2 | Employee basic access | Fold ordinary read/basic-entry access into `DataEntry` rather than introducing a replacement read-only role | The approved model explicitly retires Viewer and requires ordinary read access to live inside Employee/DataEntry |
| D3 | Retirement strategy | Keep a controlled `ViewerRoleRetirementService` for existing databases | Existing deployments may still contain Viewer rows and assignments; a one-time idempotent retirement path is safer than pretending the role never existed |
| D4 | Viewer-only user migration | Migrate Viewer-only users to `DataEntry`, disable the account, and write the required audit message | Satisfies the approved migration rule without granting a stronger role or silently re-enabling access |
| D5 | Viewer-plus-valid-role migration | Remove Viewer and preserve the other valid role(s) unchanged | Avoids unintended privilege changes while fully retiring the obsolete role |
| D6 | Companyless Viewer users | Keep disabled and mark for SystemAdministrator review | Prevents accidental company access assignment during migration |
| D7 | Policy normalization | Replace legacy string policy usage in active controllers with `PolicyNames` constants | Centralizes authorization and makes endpoint behavior match the final rights matrix for direct URLs and POST actions |
| D8 | Purchase vs stock-add split | Keep `PurchasesController` on purchase-invoice permissions and move `StockAdditionController` onto `CanRecordStockPurchase` / `CanRecordNewborn` | Final rights distinguish finance-managed purchase invoices from operational livestock intake/newborn recording |
| D9 | Financial visibility | Remove `FarmManager` from financial KPI/report visibility in active runtime decisions | Final rights allow operational reporting but not financial-report access for FarmManager |
| D10 | Role-assignment hardening | Server-side role assignment accepts only `RoleNames.All` for SystemAdministrator and `RoleNames.CompanySafeAssignable` for CompanyAdministrator | Prevents client-side role tampering from escalating to SystemAdministrator |
| D11 | Concurrency handling | Make final-role seeding resilient to duplicate/create-update races | Integration startup showed a concurrency failure; retrying/refreshing role rows keeps startup stable and idempotent |
| D12 | Historical references | Preserve Viewer only in clearly historical audit evidence or explicit retirement documentation | Meets the cleanup requirement without erasing audit history |
| D13 | E2E remediation | Update Playwright login selectors to target the current `UserName` login field instead of retired email-only selectors | The app login flow accepts username-or-email through `model.UserName`; the test harness had drifted and caused false negative authorization failures |

---

# DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION Decisions

| # | Decision | Choice | Rationale |
|---|---|---|---|
| DR-001 | Permission model authority | Use fine-grained `PermissionNames` 57 constants + `PermissionRolesMatrix` as the authoritative source of truth | Centralized permission constants eliminate string-literal drift; the matrix (not scattered role checks) is the single canonical reference for who can do what across the six-role model |
| DR-002 | Matrix reversal corrections | Apply 3 matrix role-set corrections: (1) `Sales.Reverse` → Admin/SystemAdministrator only (exclude Accounts + OpsMgr); (2) `Invoices.Void` → exclude OperationsManager; (3) `Payments.Reverse` → exclude OperationsManager | Audit reconciliation confirmed that financial reversals require elevated privilege; OpsMgr is operational not financial; Accounts must not be able to reverse completed Sales (only Admin/Sys can touch finalized financial reversals) |
| DR-003 | Desktop layout authorization | Migrate all desktop layouts from hardcoded `IsInRole` booleans to `IAuthorizationService` using policy names matching PermissionNames | Hardcoded `IsInRole` in `_Layout.cshtml` and views creates drift risk and cannot be audited against the matrix; policy-based checks flow through the same authorization pipeline as controllers so nav visibility matches endpoint enforcement |
| DR-004 | Capability lookup caching | Use `IUserCapabilityService` with per-request cache stored in `HttpContext.Items` | Permission lookups happen many times per request (nav, multiple views, action links); caching in HttpContext items avoids repeated role enumeration and matrix lookups while staying scoped to a single request (no cross-user leakage) |
| DR-005 | 5 Authorization defects (DF-AUTH-001 → DF-AUTH-005) remediation across phases P0–P3 | Full table of defect remediations applied and verified across all 5 defects; final status = ALL RESOLVED | See DEFECTS.md for per-defect status. All 5 remediated in code (PermissionRolesMatrix + Program.cs policy wiring) and verified against test green: Unit 324/324, Integration 15/15, Architecture 60/60, Playwright 39/39. Resolution date 2026-08-15, applied in commit phases P0 (inventory) → P3 (final wiring) |

### DR-005: 5 Defects Remediated — Detail Table

Source of truth: `.agent/DEFECTS.md` + `PermissionRolesMatrix.ByPermission` final state post-remediation.

| Defect ID | Title (short) | Commit Phase | Severity | Status | Resolved Date | Allowed roles AFTER remediation |
|---|---|---|---|---|---|---|
| DF-AUTH-001 | DataEntry CanRegisterLivestock Program vs matrix mismatch | P2 → P3 | HIGH | **RESOLVED** | 2026-08-15 | `Livestock.Register`: FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator — DataEntry NOT included (matches matrix) |
| DF-AUTH-002 | FarmManager CanManageCustomers CRM denial | P2 → P3 | HIGH | **RESOLVED** | 2026-08-15 | `Customers.Create/Details/Edit`: Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator — FarmManager NOT in CRM access (matches matrix, FarmManager scoped to farm/livestock per design) |
| DF-AUTH-003 | CanViewOperationalReports too broad for Reports landing | P3 | MEDIUM | **RESOLVED** | 2026-08-15 | `Reports.ActiveLivestock`: All 6 roles (DE, FM, AC, OM, CA, SA); remaining reports (SalesByPeriod, LivestockProfitability, ProfitAndLoss, P&L Statements): Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator only — each sub-report has its own permission |
| DF-AUTH-004 | Sales.Reverse 4-roles vs 2-roles: Accounts/OpsMgr can reverse financial sales | P0 → P3 | **CRITICAL** | **RESOLVED** | 2026-08-15 | `Sales.Reverse`: ONLY CompanyAdministrator, SystemAdministrator (2 roles) — Accounts + OperationsManager explicitly EXCLUDED from Sales.Reverse (financial tampering prevention) |
| DF-AUTH-005 | Payments.Reverse / Invoices.Void OpsMgr included incorrectly | P0 → P3 | HIGH | **RESOLVED** | 2026-08-15 | `Payments.Reverse` + `Invoices.Void`: Accounts, CompanyAdministrator, SystemAdministrator — OperationsManager explicitly EXCLUDED (segregation of duties: OpsMgr is operational, not financial) |
