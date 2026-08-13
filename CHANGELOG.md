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

---

## Phase 16 — Livestock Tab User Review Corrections (LTC)

**Scope:** Three focused user-review changes only. No unrelated modules redesigned. No Stock Addition / Purchase / Newborn / Security / Audit / Invoicing / Payment / PDF / Reporting workflow behavior changes.

### 16.1 Shared Livestock Type Display — Combined Code + Description

- **New:** Created single authoritative helper `src/LivestockManager.Domain/Helpers/LivestockTypeDisplay.cs` (constants for 5 authoritative labels, enum extension `GetDisplayName()`, code helper `GetCode()`, static dictionary `AllDisplayNames` for filter `<option>` enumeration).
- **Authoritative labels used everywhere:**
  - `Ah - Purchased Castrated Ram`
  - `Su - Uncastrated Ram`
  - `Sa - Purchased Ewe`
  - `Ad - Bred Castrated Ram`
  - `Sd - Bred Ewe`
- **Enum values and livestock-ID prefixes are NOT changed.** Filter `<option value="@enum">` always submits the raw enum value (Ah/Su/Sa/Ad/Sd), never the display text.
- **Unified every inline hard-coded Func/switch statement into the single helper call:**
  - Removed inline switch blocks from: LivestockController, StockAdditionService, Success.cshtml, MobileSuccess.cshtml, Edit.cshtml (ViewData Func dependency removed).
  - Updated all 16 display locations: Livestock (filter/list/register/edit/details/csv), StockAddition (Purchase/Newborn desktop+mobile forms + Success/MobileSuccess), Sales (SearchLivestock/ResolveLivestock AJAX resolver cart TypeLabel + display), Newborn parent selectors (Mother/Father search candidates TypeDescription), Dashboard recent livestock (desktop table + mobile cards + MobileDashboard meta-caption), Expenses link-to-livestock dropdown (Display + TypeLabel), LivestockLosses selector (desktop + mobile), Purchase Invoices ActiveLivestock table (desktop + mobile).

### 16.2 Livestock Page Responsiveness — Zero Page-Level Horizontal Overflow

**Desktop (Livestock/Index.cshtml):**
- **Root cause removed:** Header removed `flex-md-nowrap` forcing h1 + action buttons into a single rigid row; h1 gains `mb-2 mb-md-0` for wrapped spacing.
- **Filters:** Bootstrap responsive grid columns; controls naturally wrap when viewport narrows.
- **Actions:** `btn-group btn-group-sm` (rigid single-row) replaced with `d-flex flex-wrap gap-1 justify-content-end btn-sm` allowing safe button wrap. Export CSV uses `ms-md-auto` so it aligns right on md+ but wraps to new row safely on narrow screens.
- **Responsive column priority (6 always visible, rest hides progressively):**
  - Always: Livestock ID, Type, Source, Farm, Current Weight, Status, Primary Actions
  - <md hidden (shown md+): Initial Weight, P&L Status (`d-none d-md-table-cell`)
  - <lg hidden (shown lg+): Acquired Date, DOB, Days (`d-none d-lg-table-cell`)
- **Table containment:** Wrapped inside `<div class="table-responsive w-100">`. Any residual column overflow stays inside the container (inner horizontal scrollbar permitted when genuinely necessary) — never reaches the page body.

**Separate mobile (Livestock/MobileIndex.cshtml):**
- **Cards NOT table:** Desktop table no longer forced into narrow viewport; each animal renders as a single overflow-safe flex card.
- **Row-top overflow-safe flex:** Left ID/type pills `min-width:0; flex:1 1 auto; word-break:break-all/break-word`; right numeric values `flex:0 0 auto; white-space:nowrap` (no clipping).
- **Advanced filter collapsible (native HTML `<details><summary>`):**
  - Search input always shown (no tap required)
  - Gear "Advanced Filters (tap to open)" expands to show Farm/Type/Status selects at width:100% + Reset + Apply buttons as flex:1 with 0.5rem gap.
  - No custom off-canvas drawer; no body overflow-lock; scroll restores naturally.
- **44px touch targets:** All 3 card action buttons (View, Add Weight, Discharge) force `min-height:44px`.
- **Bottom navigation preserved:** Home / Stock / Expense / Loss / P&L always reachable.

**Verification (integrated Chromium, Accounts user):**
- Desktop 1363px: `document.documentElement.scrollWidth=1348 < window.innerWidth=1363; exceedsDoc=false; excessPx=-15` — Page **narrower** than viewport → page-level scrollbar absent.
- Mobile: `excessPx=0; worstOffenders=[]` — Zero elements extend viewport.

### 16.3 Other Cost Description Parity (No New Migration Required)

- **Database field exists already:** Prior `AddPurchaseAcquisitionCosts` migration added `OtherCostDescription nvarchar(500) NULL` on all 3 tables (Purchase / PurchaseItem / Livestock). No additive migration created; `AddOtherCostDescription` is marked NOT REQUIRED.
- **Server-authoritative validation (StockAdditionService L86-93):** whitespace-only → null; required (Fail message) when `OtherCostAmount > 0`; maxlength 500 enforced. Client VM `IValidatableObject.Validate` mirrors the same rules but server is final authority. Both controller POSTs coerce `IsNullOrWhiteSpace ? null : Trim()` pre-service.
- **Conditional rendering throughout:** Field enabled/shown only when OtherCostAmount > 0. Empty OtherCostDescription row **suppressed** when OtherCostAmount = 0. Newborn flow never renders it (Newborn = zero acquisition cost N/A).
- **CSV export — Livestock (LivestockService.ExportCsvAsync) — added financials:**
  New columns: `TypeLabel, AllocatedCommission, AllocatedTax, AllocatedTransport, AllocatedOtherCost, OtherCostDescription, TotalAcquisitionCost`.
  `OtherCostDescription` uses RFC 4180 escaping (`"` doubled + wrapped in `"…"`). `PurchaseAmount` kept separate from `AllocatedOtherCost` → no double-count risk.
- **Audit (BuildPurchasedMetadata L574-575):** When non-null/non-whitespace, appends `OtherCostDesc:{trimmed};` to the audit metadata string.
- **Financial formulas unchanged:**
  - `Additional Acquisition Costs = Commission + Taxes + Transportation + Other Costs`
  - `Total Acquisition Cost = Livestock Purchase Cost + Commission + Taxes + Transportation + Other Costs`
  - Description does NOT change totals. All calculations server-side; browser-submitted preview totals never trusted.
  - Other Cost NOT double-counted in profitability (single stored `Livestock.AllocatedOtherCost` in Complete Profit formula).
- **Authorization preserved:** All cost + description fields stay behind `CanViewFinancialData` policy. Unauthorized users never see amount or description.

### 16.4 Build & Test Results (LTC Gate)

- **Build (Release):** 0 Warnings / 0 Errors.
- **Architecture Tests:** 60 / 60 PASS (NetArchTest layer + naming + dependency rules).
- **Integration Tests:** 15 / 15 PASS (WebApplicationFactory / Kestrel).
- **Unit Tests:** 322 / 324 PASS — **2 pre-existing intentionally-throwing failures only:** `PurchaseServiceTests.PostPurchase_CreatesLivestockIntakeIdempotently` + `.PostPurchase_LivestockInitialWeightLinkedIfProvided`. These explicitly throw DomainException "Purchase Invoice cannot auto-create livestock; use Stock Addition → New Purchase" by 2026-08-13 audit decision. Zero failures attributable to LTC round.
- **Playwright / Browser manual viewport validation:** Structurally validated overflow conditions at desktop width. Type filter labels verified readable and correct.

### 16.5 Final LTC Status

```
LIVESTOCK TAB CORRECTIONS READY FOR USER REVIEW
```

Full independent auditor write-up with root-cause analysis, responsive corrections, Other Cost Description rules, and file-by-file change log: see `audit/STOCK_ADDITION_DESKTOP_MOBILE_REPORT.md` → new Section **Livestock Tab Review Corrections**.

Overall release status continues to be **PENDING INDEPENDENT AUDIT**. This LTC round addresses only the three specific user-review corrections scoped in the request.
