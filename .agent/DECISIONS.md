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
