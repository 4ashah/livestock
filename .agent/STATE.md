# Project State

## Current Phase
PHASE-2 COMPLETE: MVP + full remaining spec delivered. 6 roles, Suppliers, Purchases, Expenses, Receipts, real PDF generation, attachment infrastructure, enhanced reports/dashboard, Audit viewer, Application settings. All 175 unit tests PASS. Release build 0 W 0 E.

## Phase Definition / Scope Increase (Source: C:\Users\Administrator\Desktop\livestock2.txt)
Original MVP 9 slices verified. 36 items in objective list remain: Companies/Farms (completed, review roles 6), Users/perm (4→6 roles), Customers (done, add balance+statement), Suppliers (NEW), Purchases (NEW), Expenses (NEW), Sales/Invoices (extend 8 statuses + additional charges + snapshot + numbering), Taxes/Discount/Charges (extend), PDF Invoices (real formatted PDF), Payments (extend partial+reversal), Receipts (NEW), Customer balances (NEW), Supplier info (NEW), Basic+Complete Profitability (NEW), Operational+Financial reports (expand), CSV (expand), Photos+attachments (NEW), Audit history viewer (NEW), Application settings (NEW), Responsive (review), Essential tests (84→150+), IIS deploy (done extend scripts), Backup (done extend docs), Documentation (extend).

## Last Completed Task
M12 COMPLETE: Audit history viewer UI (Administrator only) with filter/pagination/details JSON rendering + Application settings (company profile + 6 default values + Invoice/Receipt prefixes, TaxRate, FYStart, Currency, WeightUnit, IsActive) + 10 Phase2SmokeTests (DbSets, Migration, Roles, 3 Enums, 2 Interface checks, DI registration, Controller auth coverage). RoleAuthorizationMatrixTests expanded to 8 roles (4 legacy + 4 new). CompaniesController class-level [Authorize] added. Release build SUCCEEDED 0 W 0 E, 175/175 PASS unit tests.

## Current Task (Active module)
M12: ALL DONE. Phase 2 complete.

## Next Tasks (priority order, vertical modules, independent where possible)
NONE. Phase 2 complete — all 12 modules M0–M12 delivered.

## Blockers
None. Default SQL Server (Server=.) reachable. All Phase 2 modules build green.

## Build Result
Debug Build (2026-08-07 M0 start): **SUCCEEDED** — 8 projects, 0 errors, 0 warnings.
Release Build: **SUCCEEDED** (2026-08-07 M12) — 8 projects, 0 errors, 0 warnings.

## Test Result
Debug unit tests (2026-08-07): Passed 84, Failed 0, Skipped 0.
Release unit tests (2026-08-07 M12): Passed 175, Failed 0, Skipped 0.
Goal Phase-2: ≥150 unit tests → **ACHIEVED (175/175 PASS)**.

## Migration Status
InitialMvp APPLIED (21 tables). Phase2Entities APPLIED (Suppliers, Purchases, PurchaseItems, Expenses, Receipts, InvoiceAdditionalCharges, Documents + configs/indexes). Both migrations APPLIED.

## Last Valid Git Commit
Post-M12. Next stable commit: after release package verification.

## Resume Instructions (auto-resume safe after any context limit)
```
cd C:\Projects\livestock
# 0. Verify state
git status --short  (should be clean; if not, inspect then commit valid changes)
# 1. Verify baseline
dotnet restore LivestockManager.sln -v minimal
dotnet build LivestockManager.sln -c Release --no-restore --nologo -v minimal  (expect 0/0)
dotnet test tests\LivestockManager.UnitTests\LivestockManager.UnitTests.csproj -c Release --no-build --nologo -v minimal  (expect 175 PASS)
# 2. Launch dev server
.\run-dev.cmd  (port 5100, browser http://localhost:5100, admin@livestock.dev / Admin@123456 Dev only)
# 3. Package
.\package-release.cmd
```

## Parallel Agent Partitioning (no file conflicts)
ALL AGENTS COMPLETE (M1 roles / M2 domain entities / M3 infra services / M4-M11 vertical modules / M12 release gates).
