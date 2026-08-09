# CHANGELOG

Actual per-Phase changes based on source-code audit evidence.

---

## Phase 0 — Initial Repository Scaffold
- Created 8-project solution: Domain, Application, Infrastructure, Web + 4 test projects (Unit, Integration, Architecture, EndToEnd).
- All projects target `net8.0` with Nullable enable; ImplicitUsings enable.
- Initial SQL Server + EF Core persistence skeleton; Identity scaffold (ApplicationUser + ApplicationRole).
- Basic project directory layout: src/, tests/, scripts/, docs/, audit/.

## Phase 1 — Source Inventory & Defect Audit
- Comprehensive source inventory created: `audit/PHASE1_SOURCE_INVENTORY.md`.
- Discovered 22 of 23 doc-claimed features NOT EXISTS in source (Section H).
- Created defect register: `audit/DEFECTS.md` + SECURITY/DATABASE/UI findings.
- Connection-string canonical key identified: `ConnectionStrings__LivestockManagerDb` (with legacy DefaultConnection fallback).

## Phase 2 — Authoritative Ovine Livestock Codes + PurchaseAmount Rules
- Added 5 authoritative `LivestockType` enum values in Domain: AH=PurchasedCastratedRam, SU=UncastratedRam, SA=PurchasedEwe, AD=BredCastratedRam, SD=BredEwe.
- Enforced PurchaseAmount conditional rules: purchased types require non-zero PurchaseAmount; bred types do not.
- Created 6 roles: Viewer, DataEntry, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator.
- Added authorization policies: CanViewOperationalData, CanManageLivestock, CanViewFinancialData, CanManageFarm (via CanManageCompany), plus CanManageSales/Accounting/System.

## Phase 3 — PDF Generation (Multi-Page + Unicode Font)
- Implemented `FormattedPdfWriter` with page-tree multi-page capability and explicit content-stream page breaks.
- Embedded NotoSans Unicode font with Type0 Identity-H encoding for Unicode glyph coverage.
- Implemented `StubPdfGenerator` as testing/fallback stub.
- Registered `IPdfGenerator` interface in DI.

## Phase 4 — Document Numbering & Sequencing Year-Scoped Per Company
- Implemented `EfSequenceGenerator` with atomic UPDATE-OUTPUT / INSERT pattern on `SequenceCounters` table.
- Composite sequencing key: CompanyId + DocumentType + Year (YYYY scoped per company per year).
- Implemented `DocumentNumberGenerator` with `{Prefix}-{YYYY}-{NNNNNN}` zero-padded format.

## Phase 5 — Connection-String Standardization + Fail-Closed Production Guards
- Created `ConnectionStringStartupValidator` with early validation BEFORE WebApplication builder.
- Canonical key: `ConnectionStrings__LivestockManagerDb` (double-underscore env-var form).
- Legacy fallback to `ConnectionStrings__DefaultConnection` with explicit warning log.
- Fail-closed: connection-string validation failure → `Environment.Exit(1)`.

## Phase 6 — Protected Document Storage + 9-Extension Allow-List + Magic-Byte Validation
- Implemented `IProtectedDocumentStorage` (file on-disk storage outside `wwwroot`).
- Cross-company isolation: files stored in company-guid subdirectory.
- 9-extension allow-list: `.pdf`, `.png`, `.jpg`, `.jpeg`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.csv` (validated in 5 independent locations).
- Magic-byte header validation (signature byte comparison) — extension mismatched with magic bytes denied.
- Files stored outside `wwwroot` (not publicly browsable).

## Phase 7 — Production Seed Fail-Closed; Testing/Development Explicit Opt-In
- Added early seed-flag validation in Program.cs (lines 79-92): any seed-flag true in Production → `Environment.Exit(1)`.
- Added `ProductionSeedGuard.CheckAndThrowIfUnsafe` second-check post-builder.
- `DemoDataSeeder.SeedAsync` with explicit opt-in guards:
  - Production environment → NEVER seed.
  - Testing environment → seed ONLY if `EnableE2ESeed=1`.
  - Development environment → seed ONLY if `EnableDevSeed=true/1`.

## Phase 8 — Clock/Timezone Health Check + Mandatory Clock Verification
- Added "clock" health check: `TimeZoneInfo.Local.Id` non-empty; `DateTime.UtcNow` sanity range (1970–2100).
- Health check tags: live + ready (included in both /health/live and /health/ready).
- Mandatory startup logs: UTC start time, Local timezone ID, Local offset, Clock verification notice.
- Three health endpoints: `/health/live`, `/health/ready`, `/health` (all return structured JSON).

## Phase 9 — Mobile Responsive Drawer + Viewport E2E Matrix
- Implemented mobile sidebar drawer in `_Layout.cshtml` inline CSS + vanilla JS (no jQuery/BS collapse dependency).
- Breakpoint: <992px → drawer hidden off-canvas via `transform: translateX(-100%)`; hamburger toggle + backdrop.
- site.css: 3 explicit `@media` blocks for mobile/tablet-small/desktop with responsive paddings/font-sizes/tables.
- Bootstrap 5.3 grid used pervasively (container-fluid, row, col-md-*, col-12, g-*).
- Added E2E viewport matrix tests:
  - Workflow_16: 375×812 @ 3x DPR (iPhone mobile)
  - Workflow_17: 1920×1080 @ 1x DPR (desktop)
- Both viewport tests render `/` without assertion failure.

## Phase 10 — Documentation Correction Against Proven Source
- Created `docs/IMPLEMENTED_FEATURES.md`: lists ONLY features PROVEN EXISTS with file:line evidence.
- Created `docs/ROADMAP.md`: lists 22+ NOT-IMPLEMENTED features previously claimed by docs.
- Created `CHANGELOG.md` (this file): per-Phase actual-change summary.
- Corrected existing docs to remove false claims (Hangfire, SendGrid, SMTP, Azure Blob, S3, QuestPDF, MediatR, Generic Repos, Company impersonation, Maintenance mode, User invitations, MFA reset, Quota enforcement, Break-glass reset, Password-force-change, TLOG backup, SQL Agent, Point-in-time restore, Email reminders, Custom report builder/XLSX multi-sheet, Native mobile, PWA, App Store).
- Corrected `docs/TRACEABILITY_MATRIX.md` implemented/not-implemented columns.
- Verified `docs/E2E_TESTING.md` uses canonical connection-string env var `ConnectionStrings__LivestockManagerDb`.

## Phase 11 — Repository Cleanup Inventory + gitignore + Maintenance Docs
- Created `audit/CLEANUP_INVENTORY.md`: full inventory (60+ entries) of tracked/untracked files with categorization.
- Deleted generated artifacts: artifacts/publish, artifacts/production-publish, artifacts/e2e/.playwright-browsers, artifacts/e2e/{results,screenshots,traces,videos} non-placeholder content, **/bin, **/obj, TestResults, .vs, coverage/, stale *.bak/*.zip/*.user/*.suo.
- Updated root `.gitignore` with: `.vs/`, `**/bin/`, `**/obj/`, `TestResults/`, `artifacts/{publish,production-publish,e2e/.playwright-browsers,e2e/results,e2e/screenshots,e2e/traces,e2e/videos,logs,backups}/`, `App_Data/`, `*.bak`, `*.trx`, `*.zip`, `*.pfx`, `*.snk`, `*.user`, `*.suo`, `coverage/`, `coverage.*`, `appsettings.Production.json`.
- Preserved `.gitkeep` placeholder files.
- Created `docs/REPOSITORY_MAINTENANCE.md`: tracked-vs-generated, clean locally, rebuild steps, preserve-audit-artifacts, create-review-ZIP, create-release-package + SHA256.
- Build verification: `dotnet restore` → `dotnet build LivestockManager.sln -c Release --no-restore` → exit 0.
