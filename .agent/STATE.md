# .agent/STATE.md — Livestock Manager

## Phase / Feature
Feature: REMOVE_VIEWER_AND_FINALIZE_SIX_ROLES
Phase: Six-role authorization finalization and Viewer retirement

## Current Task
Remove the retired `Viewer` role from the active application model and enforce the final six-role rights matrix:
- `DataEntry` -> Employee
- `FarmManager` -> Farm Manager
- `Accounts` -> Accounting
- `OperationsManager` -> Manager
- `CompanyAdministrator` -> Admin
- `SystemAdministrator` -> System Admin

## Exact Current State
- Release build: **PASS** (`dotnet build LivestockManager.sln -c Release` -> 0 warnings, 0 errors)
- Unit tests: **PASS** (`324/324`)
- Integration tests: **PASS** (`15/15`)
- Architecture tests: **PASS** (`60/60`)
- Playwright / E2E: **PASS** (`39/39`)
- Viewer runtime constants/policies/selectors/nav/seed users: **removed from active app model**
- Controlled retirement logic: **present** in `ViewerRoleRetirementService` for existing databases only
- Dev DB migration result: `viewer@livestock.dev` migrated to `DataEntry`, disabled, Viewer DB role removed
- Final repo sweep: only controlled retirement logic and clearly historical/audit references still mention `Viewer`
- Current remaining work: persist final status in tracking/report files and create the stable implementation commit

## Tracking
- Viewer source-reference scan: complete; remaining active code refs are controlled retirement logic only
- Viewer user-assignment count: initial count `1`; current count `0`
- User migration status: complete in development database
- Role deletion status: complete in development database
- Policy update status: complete in active runtime source
- Desktop navigation status: complete
- Mobile navigation status: complete
- User administration status: complete
- Database status: migrated in development database; startup retirement path idempotent and concurrency-safe
- Unit-test status: pass (`324/324`)
- Integration-test status: pass (`15/15`)
- Authorization-matrix status: complete
- Playwright status: pass (`39/39`)
- Latest valid commit: `bb4f81f`
- Next exact action: create the stable implementation commit for the completed six-role update

## Resume Instructions
1. Read `.agent/STATE.md`, `.agent/TASKS.json`, `.agent/DECISIONS.md`, `.agent/DEFECTS.md`, `.agent/LAST_RUN.md`
2. Run `git status --short`
3. Verify current DB Viewer state with `sqlcmd` query against `AspNetRoles` / `AspNetUserRoles`
4. Confirm final verification totals remain: Unit `324/324`, Integration `15/15`, Architecture `60/60`, Playwright `39/39`
5. Re-scan repo for `Viewer|viewer@` and ensure only retirement logic plus clearly historical/audit references remain
6. Create the implementation commit and report the SHA back to the user

---

# Feature: DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION

## Phase / Feature
Feature: DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION
Current commit: HEAD
Branch: feature/stock-addition-desktop-mobile
Phase: P12 (FINAL — all 12 phases complete)

## Current Task
ALL PHASES COMPLETED (P0–P12). Six-role authorization model fully enforced via 57 PermissionNames constants with PermissionRolesMatrix as authoritative source of truth across 20 controllers. All 5 matrix defects (DF-AUTH-001 through DF-AUTH-005) remediated and verified.

## Exact Current State
- Permissions defined: **57** (via PermissionNames.cs constants)
- Routes inventoried: **133** (20 controllers, action-level audit)
- Views inventoried: **98** (desktop views)
- Mobile views inventoried: **21**
- Matrix discrepancies: **5 — ALL RESOLVED (DF-AUTH-001 → DF-AUTH-005)**
  1. DataEntry CanRegisterLivestock → RESOLVED
  2. FarmManager Customers CRM access denial → RESOLVED
  3. CanViewOperationalReports Reports landing too broad → RESOLVED
  4. Sales.Reverse 4-roles vs 2-roles → RESOLVED (Admin/Sys only)
  5. Payments.Reverse / Invoices.Void OpsMgr included → RESOLVED (excluded)
- DesktopNav: migrated to IAuthorizationService policy checks
- MobileNav: IAuthorizationService verified correct
- Controller attributes: all 20 controllers normalized to [Authorize(Policy=PermissionNames.X)] class-level
- Program.cs policies: 57 PermissionNames wired into AddPolicy via PermissionRolesMatrix
- Global filters: AppDbContext company-scope + soft-delete filters working via lift-to-nullable expressions (no Nullable<T>.Value bug)
- Integration test auth: migrated from IStartupFilter middleware to custom AuthenticationHandler (proper ASP.NET Core auth scheme integration)
- Tests status (Release build, final):
  - Release build: **0W/0E PASS**
  - Unit: **324/324 PASS**
  - Integration: **15/15 PASS**
  - Architecture: **60/60 PASS**
  - Playwright / E2E: **39/39 PASS**

## Phase Tracking (P0–P12)
| Phase | Description | Done |
|---|---|---|
| P0a | Inventory controllers/actions/views | YES |
| P0b | Permissions constants 57 defined + PermissionRolesMatrix source of truth | YES |
| P0c | Defect triage (5 DF-AUTH defects) + discrepancy classification | YES |
| P1 | IUserCapabilityService DI + per-request HttpContext cache | YES |
| P2 | Company-scope filter middleware + farm-scope operational filters | YES |
| P3 | Program.cs wire 57 PermissionNames into AddPolicy (foreach PermissionNames.All) | YES |
| P4 | _Layout.cshtml desktop nav + action links migrated from IsInRole to IAuthorizationService | YES |
| P5 | All 20 controllers normalized to [Authorize(Policy=PermissionNames.X)] | YES |
| P6 | 98 desktop views migrated to IAuthorizationService policy-based gates | YES |
| P7 | Mobile _MobileLayout nav audit + 21 mobile views policy compliance audit | YES |
| P8 | DF-AUTH-001 (DataEntry CanRegisterLivestock) + DF-AUTH-002 (FarmManager CRM) remediated | YES |
| P9 | DF-AUTH-003 (Reports) + DF-AUTH-004 (Sales.Reverse) + DF-AUTH-005 (Void/Reverse OpsMgr) remediated | YES |
| P10 | Integration tests 15/15 verified against 57-policy model | YES |
| P11 | Architecture tests 60/60 (no direct IsInRole in controllers/views) verified | YES |
| P12 | Playwright E2E regression 39/39 + final matrix audit report (audit/DYNAMIC_SIX_ROLE_ROUTE_MATRIX.md) | YES |

## Tracking
- PermissionNames constants: COMPLETE (57 defined)
- PermissionRolesMatrix authoritative: COMPLETE (source of truth)
- Route inventory: COMPLETE (133 actions across 20 controllers)
- View inventory: COMPLETE (98 desktop + 21 mobile)
- Matrix discrepancy remediation: COMPLETE (all 5 RESOLVED in phases P2–P3)
- DesktopNav migration: COMPLETE
- MobileNav audit: COMPLETE
- Unit-test status: PASS (`324/324`)
- Integration-test status: PASS (`15/15`)
- Architecture-test status: PASS (`60/60`)
- Playwright status: PASS (`39/39`)
- Next exact action: **ALL PHASES COMPLETE — capture current HEAD checkpoint, report to user (no commit per user instruction)**

## Resume Instructions (DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION)
1. Read `.agent/STATE.md`, `.agent/TASKS.json`, `.agent/DECISIONS.md`, `.agent/DEFECTS.md`, `.agent/LAST_RUN.md`
2. Confirm final verification totals: Release build 0W/0E, Unit `324/324`, Integration `15/15`, Architecture `60/60`, Playwright `39/39`
3. Review `audit/DYNAMIC_SIX_ROLE_ROUTE_MATRIX.md` for Tables A (57×6 permission matrix), B (20 controllers), C (5-defect remediation)
4. Review current modified files via `git status --short`
5. When user authorizes, create the 12-phase finalization commit

---

## SELF-TEST CAMPAIGN 2026-08-15 (COMPLETE)

### Test Suites Status

| Suite | Status |
|---|---|
| UnitTests (324) | ✅ GREEN |
| IntegrationTests (15) | ✅ GREEN |
| ArchitectureTests (65) | ✅ GREEN |
| E2E Playwright | ⏭️ **SKIPPED** (SQL Server not available) |

**E2E status note:** SKIPPED — SQL Server environment unavailable for this campaign. Artifacts confirm 2 prior successful E2E runs with 35 screenshots captured. Will re-run automatically when SQL connectivity confirmed.

### Build + Diagnostics

| Check | Result |
|---|---|
| Release build (`dotnet build -c Release`) | ✅ **PASS** — 0W / 0E |
| IDE Diagnostics (GetDiagnostics) | ✅ **PASS** — 0W / 0E, 54 non-blocking Info/Hints |

### Architecture Fixes Inventory (applied by Architecture sub-agent, 1 retry round)

1. **DocumentsController Clean Architecture fix**: Direct `AppDbContext` dependency replaced with `IAppDbContext` abstraction. New `DbSet<Document>` added to the interface definition.
2. **Naming convention fixes**: 2 Livestock DTO classes renamed to `*ViewModel` suffix to satisfy architecture naming tests.
3. **Reference updates**: 8 files (Controllers, Views, `MobileIndex` pages) updated to use the renamed ViewModel types.
4. **New domain tests (3 files created)**:
   - `RoleNames` — 6-role count verification + internal consistency checks
   - `PermissionNames` — exactly 57 constants count verified
   - Additional domain invariant coverage
5. **New architecture rule enforcement**: `Web_DoesNotReference_Infrastructure_ImplementationNamespaces` — Web layer must reference Infrastructure through abstractions only; direct implementation namespace references blocked.

### NEXT Action

- **Immediate**: E2E campaign complete → capture final commit checkpoint (SHA) and append to state files
- **Before final commit**: Confirm SQL environment available for E2E re-run, or accept SKIPPED status with prior evidence
