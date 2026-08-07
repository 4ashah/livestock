# Project State

## Current Phase
MVP COMPLETE — All 9 Slices (S0–S9) Delivered AND Verified Live

## Last Completed Task
S9 — Final tests + IIS deploy scripts + SQL backup/restore. End-to-end verification complete: 84/84 unit tests Release PASS, Release build 0/0 errors/warnings, package-release.cmd produces 158 files in artifacts\\publish, default SQL Server (Server=.) fully seeded + 4 registered livestock Ah00001..Ah00004 persisted with sequential IDs and 3 audit rows per registration.

## Current Task
Finished. For next work, add remaining Phase-2 items (suppliers, purchases, multi-company, advanced financials, PWA offline, branded PDFs).

## Next Task
If continuing Phase 2: Supplier management → Purchase orders → Expense allocation → Multi-company → Advanced tax → Email queue → PWA offline → Replace PDF stub with QuestPDF/iTextSharp.

## Blockers
None. Default SQL Server instance (Server=.) is reachable, DB LivestockManager is seeded with 1 Company / 2 Farms / 4 Roles / 4 Users, and migrations have been applied. All acceptance criteria for MVP slices S0–S9 are met.

## Build Result
Release Build (2026-08-07): **SUCCEEDED** — 8 projects, 0 errors, 0 warnings.
Debug Build: SUCCEEDED — 8 projects, 0 errors, 0 warnings.

## Test Result
xUnit Release `dotnet test` (2026-08-07): **Passed 84, Failed 0, Skipped 0**.
7 modules covered: Livestock domain, Discharge rules, Invoice calc/transitions, Sequence generator prefixes, CSV escaping, Weight validation, Role constants.
Live browser verification: Dashboard 4 KPI cards render; Farms list 2 rows searchable; Livestock list 4 rows filterable; Register → redirect Details with sequential Ah00001..Ah00004; AuditLogs 3 rows per Create registration.

## Migration Status
Applied (2026-08-07 — Server=. default instance).
- Model migration `InitialMvp` authored under `src/LivestockManager.Infrastructure/Persistence/Migrations/` with snapshot.
- Live verification via sqlcmd COUNT(*): Companies=1, Farms=2, AspNetRoles=4, AspNetUsers=4, SequenceCounters=1, Livestock=4, LivestockWeights=4, LivestockActivities=4, AuditLogs=3 (post-fix).
- 13 business tables + Identity tables present (sys.tables count ≥ 13).

## Last Valid Git Commit
Not committed yet (git init + stable commit pending next action in this cycle; update hash after commit completes).

## Resume Instructions
```
cd C:\Projects\livestock
# 1. Ensure default SQL Server (Server=.) is reachable; otherwise update DefaultConnection in
#    src/LivestockManager.Web/appsettings.Development.json and/or appsettings.Production.json
# 2. (If DB not yet present or migrations added since last run) apply schema:
.\database-update.cmd
# 3. Run the web app on http://localhost:5100 (EnableDevSeed=true, auto-seeds if empty):
.\run-dev.cmd
# 4. Browse http://localhost:5100, login admin@livestock.dev / Admin@123456
# 5. Test suite (targets UnitTests only, 84/84 PASS expected):
.\test.cmd
# 6. Produce a self-contained Release publish drop for IIS deploy:
.\package-release.cmd
.\publish-iis.cmd   # wraps package-release + creates App_Data/files folder with ACL hints
```

## Known MVP Boundaries / Phase-2 Deferred
- Multi-company, suppliers, purchases, expense allocation, multi-invoice allocation, statements, advanced tax → Phase 2
- Email queue, PWA, offline sync → Phase 2
- Extensive document mgmt, workflow engine → Phase 2
- PDF generator: minimal valid PDF stub; replace with QuestPDF/iText for branded output in Phase 2 or later MVP iteration
- Production deployments should override DefaultConnection, set SeedDemoData=0, EnableDevSeed=false, and bind an SSL cert per DEPLOY_CHECKLIST.md §7 HTTPS Hardening.
