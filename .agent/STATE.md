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
