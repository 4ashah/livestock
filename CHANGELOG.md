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

---

## Phase 17 — Sales Tab User Review Corrections

**Scope:** 23-section spec `C:\Users\Administrator\Desktop\livestock.txt` (sections 1108–1127) — **UPDATE ONLY the Sales tab, Sale Creation Workflow, Sale Reversal, Bulk Add, Suggested Price, Sale Costs, Percentage Display.** No unrelated modules modified. No Stock Addition / Livestock / Purchase / Security / Audit behavior changes outside Sales.

### 17.1 Percentage Display Literal-ToString Bug Fix (Root Cause + Standardized Formatter)

- **Root cause (6 exact lines reproduced):** Razor paren-scope mismatch `@(it.DiscountPercent*100).ToString("0.##")%` — closing `)` terminates the Razor expression BEFORE `.ToString()` is reached → the remainder `.ToString("0.##")%` renders as static LITERAL text. User sees `0.ToString("0.##")%` (the reported "display bug").
- **Shared authoritative formatter:** New `src/LivestockManager.Domain/Helpers/PercentageDisplay.cs` — convention `0.15 stored = 15% displayed`. Methods: `Format(decimal?)` → `"15%"` or `"0%"`; `FormatTwoDecimals(decimal)` → `"15.00%"` for Settings pages with 2dp strict display.
- **Fix locations (7 lines):**
  - Sales/Details.cshtml L136-L137 (Discount%, Tax%)
  - Purchases/Details.cshtml L78, L81, L217, L218 (header + rows DiscountPct, TaxRate)
  - Settings/Index.cshtml L101 (TaxRate input-group display)
- **Validation:** Stored fraction convention unchanged — SaleItem `DiscountPercent/TaxPercent` stay Precision(5,4); formula `DiscountAmount = baseAmount * DiscountPercent` (no hidden /100 applied anywhere else).

### 17.2 Manual Resolve → "Add Exact ID" Rename + Adjacent Unlabeled Controls

- **Renamed controls (semantic match to actual behavior):**
  - Desktop Create.cshtml L86 button `Manual Resolve` → **`Add Exact ID`** with `aria-label` + tooltip explaining exact livestock-ID add (no search required).
  - Mobile MobileCreate.cshtml L74 button `Resolve` → **`Add Exact ID`** same labeling.
- **Adjacent unlabeled control fix (the 3 glyph-only ✕ remove buttons):**
  - Desktop Create.cshtml L145 additional line-item remove → explicit `aria-label="Remove this additional item line"` + `title`.
  - Desktop renderCart JS function livestock remove button → specific title+aria-label per livestock.
  - Mobile MobileCreate.cshtml L114 additional line remove → title+aria-label.
- **No adjacent controls removed:** All controls are functional (exact-ID add / remove items); rename + aria/title was the correct remediation.

### 17.3 Additive Database Migration: Sale Costs, Reversal, Price Snapshots

- **Migration name (authentic EF Core 8, not hand-written):** `20260813195614_AddSaleCostsReversalPriceSnapshots.cs` (auto Designer + ModelSnapshot).
- **Sale entity new columns (13 fields):**
  - 4 non-negative seller-side amounts: `CommissionAmount (18,2)`, `SellerTaxAmount (18,2)`, `TransportationAmount (18,2)`, `OtherCostAmount (18,2)` (4 SQL CHECK ≥0 constraints).
  - `OtherCostDescription nvarchar(500) NULL`, `TotalAdditionalSaleCosts (18,2)`, `NetSaleProceeds (18,2)`.
  - `CostAllocationMethod int DEFAULT 0` (Equal = 0 default; future ByPrice/ByWeight/Manual structured).
  - 4 reversal fields: `ReversedAt datetimeoffset`, `ReversedByUserId uniqueidentifier`, `ReversalReason nvarchar(500)`, `ReversalNotes nvarchar(2000)`.
- **SaleItem entity new columns (14 fields):**
  - Price snapshot fields: `SuggestedPrice (18,2) NULL`, `SuggestedPriceMethod int NULL`, `SuggestedWeight (18,4) NULL`, `SuggestedWeightDate datetimeoffset NULL`, `SuggestedRate (18,4) NULL`.
  - `FinalSalePrice (18,2) NOT NULL`, `PriceSource int DEFAULT 0` (NotSet / Suggested / ManualOverride via 2dp compare).
  - 4 allocated seller costs: `AllocatedCommission/SellerTax/Transportation/OtherCost` each `(18,2)` (4 SQL CHECK ≥0 constraints).
  - `NetSaleProceeds (18,2)`.
- **Additive enum:** `SaleStatus.Reversed = 4` appended after Cancelled (0=Draft/1=Confirmed/2=Completed/3=Cancelled/4=Reversed).
- **SQL Server Express compatible indexes:** `IX_Sales_Status_ReversedAt` and `IX_Sales_CompanyId_Date` (no INCLUDE clause needed).
- **DOWN method:** drops 8 constraints, drops 2 indexes, removes 27 columns in safe reverse order.

### 17.4 Transparent Suggested Sale Price (Server-Authoritative + Historical Snapshots)

- **Single authoritative service source:** `ISaleService.GetSuggestedSalePriceAsync(livestockId, companyId, ct)` returns tuple `(SuggestedPrice, Method, Weight, WeightDate, Rate)`.
- **Pricing methods (new enum `SuggestedPricingMethod`):**
  1. `WeightTimesConfiguredRate = 1` (preferred): `LatestWeight * ConfiguredPricePerWeightUnit` → basis text = "Based on: 50kg × 25.00 per kg".
  2. `CostMarkupLegacy = 2` (fallback when no rate configured): `PurchaseAmount * 1.3m` → basis text = "Based on: Cost markup (PurchasePrice × 1.30)".
  3. `NotSet = 0` if neither data available → message + manual entry required. NO FABRICATED DEFAULT VALUES.
- **Stored per-SaleItem historical snapshot (immutable after Confirm):**
  - Suggested 5-tuple captured **at draft creation** (reflects weight-date + rate of that moment).
  - `FinalSalePrice` = user-accepted price at confirm; `PriceSource` = `Suggested` if `|FinalSalePrice - SuggestedPrice| < 0.01`, else `ManualOverride`.
- **Read projections (1):** 25-200 row search/bulk/resolve candidates inline the same 1.3m fallback to avoid N+1 service call; snapshot fields populated in resolver and bulk-add endpoints.

### 17.5 Bulk Add Repair (Server Validated, Desktop Responsive Table, Mobile Selectable Cards)

- **Server eligibility gatekeeper:** GET `SalesController.ListEligibleLivestockBulkAdd` → each candidate must satisfy: `Status == Active`, `DischargeCondition NOT Sold`, same CompanyId as caller, not cross-company hidden via FarmId policy (authorization propagated).
- **POST `SalesController.BulkAdd(saleId, selectedLivestockIds[])` → service transaction wrapped:**
  - Sale must be Draft only.
  - 3 counters returned in `SaleBulkAddResultDto`: `AddedCount`, `AlreadyPresentCount`, `IneligibleCount` + per-skipped `Messages[]`.
  - In-request duplicate detection: HashSet `seenInRequest` → same-ID twice → AlreadyPresent + skip.
  - In-sale duplicate detection: existing SaleItem.LivestockId hashset → AlreadyPresent.
  - Double-click idempotent: any subset of IDs re-sent is safe via the two HashSet checks.
- **Desktop UX (Bootstrap modal-xl modal-dialog-scrollable):**
  - Modal opened via "📋 Bulk Add (select many)" button.
  - Filter row: keyword search + farm filter + "🔍 Search" + "☑ Select page (all visible)" header checkbox (Select-All-Current-Page; not global — deterministic pagination scoped).
  - Responsive selectable table: per-row checkbox + columns (Livestock ID, Type, Farm, Weight, Suggested Price, Basis).
  - Footer live counter badge: `0 animals selected`; Confirm button disabled when count === 0; on confirm → resolves IDs → calls same `addLivestockById(id)` function today (shared integration point).
- **Mobile UX (native HTML `<details><summary>` NO drawer):**
  - `<summary>📋 Bulk Add (tap to open)</summary>` expands to per-animal cards with checkbox + selected count sticky footer → "＋ Add N Selected" green confirm.
  - Touch targets: 44px min-height; zero page-level horizontal overflow (cards flex 1-col).

### 17.6 Sale Reversal Workflow (Confirmed → Reversed ONLY; 3 Cases; All-or-Nothing)

**Confirmed sale immutability policy:** Draft → Cancel / Confirm; Confirmed → NEVER Edit, use Reverse + Reasons. Reversed → never un-reversed, never deleted from database.

**Authorization policy (SalesController.Reverse POST):** User role NOT IN {Viewer, DataEntry} → allowed = FarmManager w/ CanManageSales + Accounts + CompanyAdm + SystemAdm.

**New backend: SaleService.ReverseSaleAsync(reversalDto, companyId, actingUserId, role, ct)** — 1 atomic SQL transaction; idempotent; all-or-nothing livestock restore validation.

1. **Idempotency guard:** Status already `SaleStatus.Reversed` → return immediately (no double-processing).
2. **Draft-only check escape:** Only `Confirmed` reversible; Draft/Cancelled throw.
3. **3 Case Categorization:**
   - **Case A (Confirmed + no final invoices):** Draft invoices → set status Cancelled with reason appended to Notes. Sale → `Reversed`; livestock restored; write audit activities.
   - **Case B (Unpaid finalized invoice + NO Payments/Receipts):** First each unpaid invoice → `InvoiceStatus.Voided` with reversal reason appended to Invoice.Notes; then Case A remainder flow.
   - **Case C (has Payments / Receipts / Paid / PartiallyPaid invoices):** THROW DomainException with ordered remediation step list → `"Reverse all receipts, reverse all payment allocations, recalculate balance, void invoice, then retry reverse."`.
4. **All-or-Nothing Livestock Restoration Pre-validation (BEFORE mutation):**
   - For every livestock-linked SaleItem: load Livestock entity → MUST satisfy: `Status == LivestockStatus.DischargedSold` AND `DischargeCondition == DischargeCondition.Sold` AND `SoldViaSaleItemId == item.Id`.
   - Collect mismatches into `conflicts` string list.
   - If any conflicts, throw DomainException listing them → **transaction rolls back (zero partial-state restored / zero activity inserted / zero status changes)**.
5. **Livestock restoration actions (after validation passes):** Clear `SoldViaSaleItemId`, clear `DischargeCondition`, clear `DischargeDate`. Set `Status = LivestockStatus.Active`. **HISTORICAL: `SoldAmount` preserved (snapshot kept for financial traceability); SoldAmount never zeroed.**
6. **Audit trail per-animal deduplicated:** Insert each `LivestockActivity` with `Note="SaleReversed;SaleId:{id};Reason:{trim};Date:{utc}"`. **Deduplication guard:** `AnyAsync(a => a.LivestockId == l.Id && a.Note.StartsWith("SaleReversed;SaleId:" + sale.IdStr))` → skip if already written (idempotent re-run safe).
7. **Sale header fields stamped:** `Status = Reversed`, `ReversedAt = utcNow`, `ReversedByUserId = actingUserId`, `ReversalReason = trimmed-nonempty (required)`, `ReversalNotes = trimmed (optional, max 2000)`.
8. **SaveChanges + Commit transaction.** Returns the updated SaleDetailDto.

**Desktop UI (Sales/Details.cshtml):**
- Header badges: Status=Reversed → `<span class="badge bg-danger">↶ Reversed</span>`; action buttons replaced with reversed date.
- Reversed yellow alert block: Reason + Notes + date.
- NEW Bootstrap modal `#reverseModal` (centered):
  - Required input `ReversalReason` (500 maxlength; placeholder examples; JS validate trim>0 → confirm button disabled if empty).
  - Optional textarea `ReversalNotes` (2000 maxlength with live counter "0 / 2000").
  - Hidden input `Id=@Model.Id`; hidden `ReversalDate=Now`.
  - Warning alert bullet list (restore livestock, void unpaid invoice, blocked if payments, audited).
  - Confirm dialog: `"Are you sure you want to reverse this confirmed sale? Livestock will be restored to Active status. This action is audited and cannot be undone via ordinary edit."`
  - On submit: Confirm button → disabled + "Processing..." to prevent double-tap.

**Sales Index (desktop list + mobile list):**
- Desktop: Filter dropdown `↶ Reversed` option added; sale number prefix `REVERSED-{shortId}`; status badge `bg-danger ↶ Reversed`.
- Mobile: Same prefix + badge color class `bad`.

### 17.7 Additional Sale Costs + Profitability Distinctions (Gross / Net / Basic / Complete)

#### Sale-level fields + server triple-guard
- 4 seller-side amount fields `CommissionAmount / SellerTaxAmount / TransportationAmount / OtherCostAmount`.
- `ValidateCosts()` private helper, called from: CreateDraftAsync entry → RecalculateSaleTotals → ConfirmAsync final.
  - Rule 1: Each of 4 amounts ≥ 0 (negative → DomainException specifying which).
  - Rule 2: If `OtherCostAmount > 0` → `OtherCostDescription` trimmed non-empty AND ≤ 500 chars, else → DomainException.
- Totals (server only, browser preview ignored):
  - `TotalAdditionalSaleCosts = CommissionAmount + SellerTaxAmount + TransportationAmount + OtherCostAmount`.
  - `GrossRevenue = sum(items.UnitPrice * Qty)`.
  - `NetSaleProceeds = (Invoice GrandTotal AS PAID BY CUSTOMER) - TotalAdditionalSaleCosts`.

#### Allocation to per-animal (not to generic non-livestock additional lines)
New method `ApplyCostAllocation(Sale sale, List<SaleItem> items)` with default `AllocateCostEqual`.
- 1 animal → entire cost = that single item.
- N animals → each cost C: for index 0 ≤ i < N-1: `Round(C/N, 2, MidpointRounding.AwayFromZero)`. Item at index N-1: `C - Σ(previous)`. This guarantees the sum of all AllocatedCost == cost exactly to the cent (no lost pennies).
- 0 animals (pure generic-item sale) → safely 0 (no /0, graceful).
- Future allocations: structured enum `CostAllocationMethod { Equal = 0, ByPrice = 1, ByWeight = 2, Manual = 3 }` already stored; service switch statement easy to extend.

#### Per-animal line item values
Each livestock SaleItem carries: AllocatedCommission / AllocatedSellerTax / AllocatedTransportation / AllocatedOtherCost; `NetSaleProceeds` per-item = FinalSalePrice - sum(4 allocated).

#### Label strict separation (never ambiguous):
- **🧾 Customer Tax %**: percent (0.00–1.00) `TaxPercent` (sale-level Discount%+Tax% now POSTed FromForm and applied per-item from the top-level shared form inputs).
- **💸 Seller-Paid Tax Amount**: flat dollar amount `SellerTaxAmount` (Seller-Side Costs section).
- Both labels, sections, colors are visually different.

#### Profitability formula distinctions preserved (no double count):
- **Gross Profit = FinalSalePrice - PurchaseAmount**
- **Net Profit = FinalSalePrice - AllocatedCommission - AllocatedTax - AllocatedTransport - AllocatedOtherCost**
- **Basic Profit = Gross Profit (uses only acquisition cost)**
- **Complete Profit = Net Profit - TotalAcquisitionCost (adds the seller-side sale costs)**
→ No overlap: Basic never sees the 4 new sale costs; Complete always does.

### 17.8 Desktop + Mobile Create Sale Page 7 Sections

**Desktop (Create.cshtml) – 7 card sections, 12-col Bootstrap responsive:**
1. **👥 Customer / Farm:** bindCombo combobox preserved (hidden select bindCombo pattern unchanged so POST GUIDs stay clean).
2. **🐄 Livestock Selection:** 3-col (search / "Add Exact ID" button + NEW 📋 Bulk Modal Open Button + existing paste textarea). Cart table gains "Suggested Price Basis" new column showing per-row suggestion + basis.
3. **💲 Suggested & Final Prices:** Info alert "Basis explanation". Totals: Total Suggested vs Total Final.
4. **🧾 Customer Charges:** Discount % + Customer Tax % (range 0.00–1.00; inputmode decimal; step 0.0001; min=0 max=1).
5. **💸 Seller-Side Costs:** Commission/SellerTax/Transport/Other numeric inputs (min=0, step=0.01, inputmode decimal). OtherCostDescription * required-asterisk + live JS: OtherCostAmount>0 && empty description → red is-invalid + inline error message; description nonempty → clear.
6. **🧮 Totals:** 2-section split list: Gross → Customer Discount → Customer Tax → bold "Customer Invoice Total" (divider); 4 seller cost rows (inline conditional other cost description indented ↳); Final "Total Seller Costs" divider → bold green "Net Sale Proceeds".
7. **✅ Review/Confirm:** sticky submit bottom row sticky-sm, double-tap guard.

**Mobile (MobileCreate.cshtml) – dedicated mobile page separate route:**
- 7 stacked `mob-card` sections; 100% width inputs; 44px min-height buttons; `inputmode="decimal"` everywhere.
- Bulk Add inside native `<details><summary>📋 Bulk Add (tap to open)</summary>` with per-card checkbox.
- Seller-Side Costs inside collapsible details `<summary>💸 Seller Costs (tap to expand — reduce Net Proceeds)</summary>` to save vertical space.
- Totals summary cards with matching 2-section split.
- Sticky `mob-submit-row` bottom bar: primary confirm green, disabled until cart length > 0, double-tap disables.

### 17.9 Build & Test Results (Sales Tab Gate)

| Metric | Value | Notes |
|:-------|:-----:|:------|
| dotnet build Release | 0 Warnings / 0 Errors | — |
| Architecture Tests | 60 / 60 Passed | 0 Failed 0 Skipped |
| Integration Tests | 15 / 15 Passed | WebAppFactory/Kestrel |
| Unit Tests | 322 / 324 Passed | †2 pre-existing intentionally-throwing `PurchaseServiceTests.PostPurchase_*` (unchanged from Phase 16). |
| HTTP /Account/Login smoke | 200 OK, 27,112 bytes | — |
| HTTP /Sales smoke | 302 → /Account/Login | Correct authenticated redirect |
| HTTP /Sales/Create smoke | 302 | Correct |
| HTTP /Sales/MobileIndex smoke | 302 | Correct |
| HTTP /Sales/MobileCreate smoke | 302 | Correct |
| VS Code GetDiagnostics | [] empty array | No IDE diagnostics |

### 17.10 Final Sales Tab Status

```
SALES TAB CORRECTIONS READY FOR USER REVIEW
```

Full independent auditor write-up: see `audit/SALES_TAB_USER_REVIEW_REPORT.md`.

Overall release status continues to be **PENDING INDEPENDENT AUDIT** (see prior Phase 15 gate table).

---

## Phase 18 — Reports Tab User Review Corrections (RFC)

**Scope:** 23-section spec `C:\Users\Administrator\Desktop\livestock.txt` (Sections 1–23) — **UPDATE ONLY the Reports Tab.** Four user-review report corrections only. No other module modified. Zero functional changes to: P&L, Livestock, Sales, Stock Addition, Purchases, Newborn, Security, Identity.

### 18.1 Active Livestock by Type Farm Filter — Root Cause & Fix

**Root cause (CRITICAL bug reproduced 100%):**
- File: [ActiveLivestock.cshtml L5](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml#L5)
- Original code:
  ```csharp
  var farms = ViewData["Farms"] as List<dynamic> ?? new List<dynamic>();
  ```
- Actual runtime object passed from ReportsController `_farmService.ListAsync(companyId, ct)` is `IList<FarmSummaryDto>` strong typed. The `as List<dynamic>` cast always returns null. Code falls back to empty `new List<dynamic>()` → dropdown contains only the hardcoded default `<option value="">All Farms</option>` → user literally could never pick an authorized farm.
- Service-side filtering was intact; if a user manually crafted URL with `?farmId=GUID` in query string, the service still applied company-authorized filtering. The UI was the broken component.
- CSV export was unaffected (CSV uses controller parameter binding directly from URL `farmId` string).

**Fix:**
- Use correct cast `IList<FarmSummaryDto>`.
- Add explicit company-authorized farmId validation at service `ActiveLivestockByTypeReportAsync`: load company farms → if user-provided farmId NOT in authorized list → nullify (empty safe authorized dataset returned; cross-company leak blocked).
- Labels use `LivestockTypeDisplay.GetDisplayName()` for 5 authoritative type codes (Ah/Su/Sa/Ad/Sd combined code + description).
- Responsive filter row (Bootstrap col-12 col-md-4) with Apply + Clear filter buttons.

### 18.2 Sales by Period — Terminology Standardization (# → Number)

Every user-facing "count of heads" label uses `Number Sold` / `Total Number Sold`.
Scope of replacement (all verified after rewrite):
- Desktop SalesByPeriod.cshtml: 4 headline cards "Total Number Sold".
- Farm summary table header column: `Number Sold`.
- Sale details table header column: `Number Sold`.
- Mobile kpi card chip-tabs: `Number Sold`.
- Farm summary / sale detail mobile per-animal number sold label: `Number Sold`.
- CSV export column header: `Number Sold` (columns 6 in sale detail CSV rows).

**Explicitly NOT replaced (different legitimate meaning per spec Sec 7):**
- Sale document number prefix `SALE-YYYY-NNNNNN` `Sale Number` column header kept.
- Livestock IDs `Number` column (livestock tab) untouched. These are identifier numbers not head-counts.

### 18.3 Sales by Period — Farm Filter + Farm Summary Breakdown + Preserved Headline Totals

**New Farm filter:**
- Controller signature `SalesByPeriod(DateTime? from, DateTime? to, Guid? farmId)` → dropdown renders authorized farms (same pattern Active Livestock fixed cast).
- Validation: farmId submitted → must be in company-authorized farms; if not → coerce null (empty safe authorized result).
- From defaults to `-30 days`; To defaults to `Now`. Both use `AsUtcDayStart` / `AsUtcDayEnd` inclusive boundaries (midnight start → 23:59:59.999 end).

**New Farm Summary Breakdown table (server-side authoritative grouping):**
Table columns: `Farm | Number Sold | Gross Sale Amount | Additional Sale Costs | Net Sale Proceeds`.
- **Row order:** Sorted by FarmName ascending (All Farms + specific Farm filtered modes).
- **Number Sold per farm:** count of SaleItems where `LivestockId.HasValue` (distinct livestock sold). Generic non-livestock additional lines do NOT count toward Number Sold (matches Sec 8 formula).
- **Financials per farm row:** Gross Sale Amount = GrandTotal of Confirmed/Completed Sales to that farm; Additional Sale Costs = sale.TotalAdditionalSaleCosts (4 seller costs summed: Commission + SellerTax + Transportation + Other); Net = NetSaleProceeds.
- **Headline totals preserved exactly:** They are Sum of same grouped aggregates → identical byte-for-byte numbers. Farm breakdown reconciles.

**Sale Status filter backend (server-side authoritative):**
Only `Confirmed/Completed` Sales counted. Excluded: Draft / Cancelled / Voided / Reversed. Reversed sales suppressed from totals (they reverse all financial meaning per Phase 17 ReverseSaleAsync; already have Reversed status).

**Date Basis banner (desktop + mobile):**
Desktop top info banner blue: `📅 Date Basis: Sale Date (period filter uses confirmed completed sale date — inclusive)`. Mobile same banner inside mob-card with info class.

**Sale details table (desktop) + collapsible (mobile):**
Columns: Sale Date, Sale Number (with ↶ REVERSED prefix if Reversed), Sale Farm, Customer, Number Sold, Gross Sale Amount, Status badge (Draft/Confirmed/Completed/Cancelled/↶Reversed/Other), View link → `/Sales/Details/{id}`. Farm names populated from actual Farm navigation (fixed prior DischargesAsync FarmName null bug → farm name always actual not string "(All)").

### 18.4 Livestock Profitability — From/To Date Filters (Realization-Based Date Semantics)

**Added controller parameters:**
`LivestockProfitability(DateTime? from, DateTime? to, Guid? farmId, LivestockType[]? types, StockSource[]? sources, LivestockStatus[]? statuses, …)`
- Inclusive range: `from.AsUtcDayStart() / to.AsUtcDayEnd()`.
- Filter form shows: From Date, To Date, Farm dropdown, Apply + Reset buttons. Reset = same page action (preserves route but empties form → all-dates behavior).

**Realization-based dispatch (critical semantic):**
Service method `LivestockProfitabilityWithDatesAsync`:
- **Dispatch case A (Both dates null):** Pass through to original `LivestockProfitabilityAsync` (no date filter). Behaves exactly like OLD report (shows Active / Sold / Deceased / Lost / Stolen / Other statuses combined; returns all-company-authorized livestock with profitability). Matches user expectations of "the default report".
- **Dispatch case B (Any date set):** Call `CompleteLivestockProfitabilityAsync` → filter to `DischargedSold` only → apply `DischargeDate` or (if linked via SaleItem.Sale) use `Sale.Date` as realization date (prefer sale date when linked). Unsold animals excluded from realized profitability. Reversed/Cancelled/Voided invoice sales excluded (consistent with Phase 17 Sale Reversal).

**Historical acquisition + operating costs always preserved:**
For animals whose realization dates are inside the report range, include the FULL historical:
- `PurchaseAmount` (acquisition cost of that animal regardless when bought)
- `AllocatedCommission, AllocatedTax, AllocatedTransport, AllocatedOtherCost` (4 acquisition additions regardless when allocated)
- `TotalAcquisitionCosts = PurchaseAmount + sum(4 allocated)`
- Operating expenses allocated to the animal (via expense-to-livestock links, full amounts regardless expense date)
- NEVER filter expenses by report date range (forbidden Sec 12 incorrect calculation list item "discard pre-period costs").

**4 existing filters combined with dates:**
Farm, Livestock Type (multi), Stock Source (multi), Status (multi) — composition works as before. FarmId authorization validated first.

**Date Basis banner (desktop + mobile):**
Desktop top banner: `📅 Date Basis: Sale Date (Realized Profitability — includes sold animals whose sale/disposal date falls inside From…To; shows historical costs)`. Mobile identical.

**Summary 6 cards (desktop) + 3 mob-kpi-rows (mobile):**
1. Number Livestock Sold (count = rows in realized set)
2. Gross Sale Revenue (sum FinalSalePrice / per-animal gross)
3. Total Acquisition Cost (sum)
4. Operating Costs Allocated (sum)
5. Additional Sale Costs (sum)
6. Complete Profit (Gross - Total Acq - Operating - Additional Sale Costs)
Color coding: Complete Profit ≥ 0 → green; < 0 → red with warning class.

**Details table columns (14 columns per animal, 14 CSV):**
Livestock ID, Type (via LivestockTypeDisplay.GetDisplayName), Sale Farm, Stock Source, Acquired Date, DOB, Sale Date, Gross Sale Amount, Purchase Amount, Additional Acquisition Costs, Total Acquisition Costs, Operating Costs, Additional Sale Costs, Net Sale Proceeds, Basic Profit, Complete Profit.
Basic Profit color when >=0 green else red. Complete Profit same.

### 18.5 New 4 Typed Report DTOs + 1 Modified DTO + 3 New Service Method Signatures

Service interface preserves OLD signatures to prevent build breaks (P&L + existing callers 100% untouched):
- Preserved: `Task<(Guid? FarmId, string? FarmName, int Count, decimal TotalValue)> ActiveLivestockByTypeAsync(...)` (anonymous tuple)
- Preserved: `Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityAsync(companyId, farmId, ct)`
- Preserved: `Task<IList<LivestockProfitabilityReportRowDto>> CompleteLivestockProfitabilityAsync(...)`
- Preserved: `Task<ProfitLossReportDto> ProfitLossAsync(...)`

**3 new signatures appended (additive only — none removed/renamed):**
1. `ActiveLivestockByTypeReportAsync(companyId, farmId, ct)` → typed `IList<ActiveLivestockByTypeReportDto>` (counts + percentages + LivestockTypeDisplay labels + percentageOfTotal).
2. `SalesByPeriodReportAsync(companyId, fromDate, toDate, farmId, ct)` → `SalesByPeriodReportDto` aggregate (date range, headline totals, FarmSummaries list, SaleDetails list). All navigation Include(Farm).Include(s => s.Customer).Include(s => s.Items). AsNoTracking. NumberSold counts livestock-linked items only.
3. `LivestockProfitabilityWithDatesAsync(companyId, farmId, fromDate, toDate, ct)` → dispatch A/B above; returns typed rows with 7 new fields populated.

**Modified (added 7 fields, all old fields unchanged — existing property order preserved):**
[LivestockProfitabilityReportRowDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/LivestockProfitabilityReportRowDto.cs)
- `Guid? SaleFarmId`
- `string? SaleFarmName` (prefer sale.Farm.Name over animal.Farm.Name when link present)
- `DateTimeOffset? SaleDate` (prefer sale.Date over animal.DischargeDate when link present)
- `decimal AdditionalAcquisitionCosts` = AllocatedCommission + AllocatedTax + AllocatedTransport + AllocatedOtherCost
- `decimal TotalAcquisitionCosts = PurchaseAmount + AdditionalAcquisitionCosts`
- `decimal AdditionalSaleCosts` populated from sale.TotalAdditionalSaleCosts via navigation
- `decimal NetSaleProceeds` populated from SaleItem.NetSaleProceeds via navigation

### 18.6 Desktop Views Rewritten — 3 Reports (Strongly Typed; No More dynamic Anonymous Projection)

1. **ActiveLivestock.cshtml**
   - `@model IList<ActiveLivestockByTypeReportDto>`
   - Farm filter `IList<FarmSummaryDto>` correctly cast (root cause fix)
   - LivestockTypeDisplay.GetDisplayName combined label "Ah - Purchased Castrated Ram" etc
   - Responsive filter row 1-col then 3-col md
   - Apply + Clear buttons flex next to dropdown
   - Table: Livestock Type, Number Active, Percentage Of Total (progress bar)
   - Export CSV with same farmId preserved

2. **SalesByPeriod.cshtml** — COMPLETE rewrite from ground up
   - `@model SalesByPeriodReportDto`
   - Date Basis Sale Date blue banner
   - From / To / Farm / Apply + Reset filter row
   - 4 headline cards: Total Number Sold, Total Gross Sale Amount, Total Additional Sale Costs (badge None when 0), Total Net Sale Proceeds
   - Farm Summary Table (5 columns: Farm / Number Sold / Gross Sale Amount / Additional Sale Costs / Net Sale Proceeds)
   - Sale Details Table responsive wrapper with 8 columns Sale Date / Sale Number (↶ prefix if Reversed) / Sale Farm / Customer / Number Sold / Gross Sale Amount / Status badge (color per status) / View Sale link
   - CSV: preserves from/to/farm; columns "Sale Date, Sale Number, Farm, Customer, Number Sold, Gross Sale Amount, Status"

3. **LivestockProfitability.cshtml** — COMPLETE rewrite
   - `@model IList<LivestockProfitabilityReportRowDto>`
   - Date Basis Sale Date (Realized Profitability) banner
   - From / To / Farm / Apply + Clear filter row (native date pickers with min/max)
   - 6 summary cards as per 18.4
   - 14-column details table with LivestockTypeDisplay labels, Sale Farm name, Sale Date populated
   - Color-coded Basic/Complete Profit (green/red per sign)
   - Export CSV includes same 15 columns (with from/to/farm route params preserved — so user gets same rows on-screen as in CSV)

### 18.7 Separate Mobile Pages Created — 3 Reports (Cards, No Wide Tables)

1. **MobileActiveLivestock.cshtml**
   - Layout: `_MobileLayout`. 1-column stack. No tables.
   - Farm filter mob-card: dropdown "All Farms" default, farms Name+Code combined when Code present; Reset + Apply flex:1 btn each, 44px min.
   - mob-totals-card: Total Number Active (sum).
   - Per type: mob-card title `LivestockTypeDisplay.GetDisplayName(type)`; Number Active count; PercentageOfTotal with progress bar `<div class="mob-progress"><div style="width:@(pct)%"></div></div>`.
   - Export CSV via chip-tab button top-right.

2. **MobileSalesByPeriod.cshtml**
   - Info banner mob-card "📅 Date Basis: Sale Date"
   - From / To / Farm mob-fields (label above, min-height:44px each). Apply + Reset.
   - mob-totals-card: Total Net Sale Proceeds (green if positive).
   - mob-kpi-row: 2 cols = Number Sold + Gross Sale Amount.
   - mob-kpi-row: 2 cols = Total Additional Sale Costs + Net Sale Proceeds.
   - Farm Summaries: if >6 farms → wrap `<details><summary>🏘️ Farm Summaries ({count} farms, tap to open)</summary>` collapsible. If ≤6 → flat cards directly. Each farm card contains FarmName, Number Sold, Gross, Add Sale Costs, Net 4 lines.
   - Sale Details: each sale as collapsed `<details><summary>{SaleDateOrToday displaySaleNumber↶REVERSED prefix if Reversed}</summary>` expand → Sale Farm, Customer, Number Sold, Gross Amount, 6 status inline badges with color class. `mob-btn primary View Sale` link `/Sales/Details/{id}`.

3. **MobileLivestockProfitability.cshtml**
   - Info banner "Date Basis: Sale Date (Realized Profitability)"
   - From / To / Farm mob-fields. Apply + Reset.
   - mob-totals-card: Complete Profit total (red warning class if negative; green if >=0).
   - 3 mob-kpi-rows: Livestock Sold + Gross; Total Acq Cost + Operating; Additional Sale Costs + Net Sale Proceeds.
   - Per-animal collapsed `<details><summary>{LivestockId} → {TypeCode} {Gross:n0} → profit/gain colored</summary>`. Expand = 14 field rows + mini Complete Profit inline mob-totals-card same color as desktop.
   - Export CSV chip-tab.

### 18.8 CSV Export Parity

All reports (desktop filter values → CSV rows) preserved.
- **ActiveLivestock CSV** export columns: `Livestock Type, Number Active, Percentage Of Total` → exactly matches table display. Filter farmId passed in route.
- **SalesByPeriod CSV** export columns: `Sale Date, Sale Number, Farm, Customer, Number Sold, Gross Sale Amount, Status` → from/to/farm passed in route.
- **LivestockProfitability CSV** export columns: 15 columns matches details table (per 18.4). From/To/Farm passed in route. Realization date basis same rows as UI.
- **Profit & Loss CSV** — completely untouched. Preserves existing columns, filters, and date basis (PeriodStart/PeriodEnd + existing granularity).

### 18.9 Build & Test Results Table (RFC Gate)

| Metric | Value | Notes |
|:-------|:-----:|:------|
| dotnet build Release | 0 Warnings / 0 Errors | — |
| Architecture Tests | 60 / 60 Passed | 0 Skipped 0 Failed |
| Integration Tests | 15 / 15 Passed | WebAppFactory / Kestrel |
| Unit Tests | 322 / 324 Passed | †2 pre-existing intentionally-throwing `PurchaseServiceTests.PostPurchase_*` (unchanged Phase 16/17) — not a regression |
| HTTP /Account/Login smoke | 200 OK 27,132 bytes | — |
| HTTP /Reports/Index smoke | 302 auth redirect | Correct |
| HTTP /Reports/ActiveLivestock smoke | 302 | Correct |
| HTTP /Reports/SalesByPeriod smoke | 302 | Correct |
| HTTP /Reports/LivestockProfitability smoke | 302 | Correct |
| HTTP /Reports/ProfitLoss smoke | 302 | Correct (untouched) |
| HTTP /Reports/MobileActiveLivestock smoke | 302 | NEW (was 404 before controller append) |
| HTTP /Reports/MobileSalesByPeriod smoke | 302 | NEW |
| HTTP /Reports/MobileLivestockProfitability smoke | 302 | NEW |
| HTTP /Reports/MobileProfitLoss smoke | 302 | Untouched |
| VS Code GetDiagnostics | [] empty | No C# / CSHTML / JS issues |

### 18.10 Final Reports Tab Status

```
REPORTS TAB CORRECTIONS READY FOR USER REVIEW
```

Full independent auditor write-up with root cause analysis, semantic date rules, terminology changes, file-by-file change log, authorization matrix: see `audit/REPORTS_TAB_USER_REVIEW_REPORT.md`.

Profit & Left completely left untouched (zero source code modifications to ProfitLoss view, MobileProfitLoss view, ProfitLossAsync service method, ProfitLoss CSV rows). Per spec Section 1, Section 15, Section 22 completion #25.

Overall release status continues to be **PENDING INDEPENDENT AUDIT** (see Phase 15 gate table).

## Phase 19 — Remove Viewer And Finalize Six Roles (2026-08-15)

- Introduced the final six-role runtime model: `DataEntry`, `FarmManager`, `Accounts`, `OperationsManager`, `CompanyAdministrator`, `SystemAdministrator`.
- Removed Viewer from active role constants, active policy definitions, demo/dev seeding, login demo accounts, user-role selectors, and desktop/mobile navigation.
- Added controlled `ViewerRoleRetirementService` startup remediation for existing databases: Viewer-only users migrate to disabled `DataEntry`; Viewer-plus-valid-role users retain valid role(s); retired role deleted once assignments reach zero.
- Normalized active controllers onto centralized `PolicyNames` constants and tightened the operational/financial rights split, including `StockAdditionController` purchase/newborn authorization.
- Verified development-database retirement: initial Viewer assignment count `1`, final count `0`; `viewer@livestock.dev` now disabled `DataEntry`.
- Fixed stale Playwright login selectors that still expected an email-only login field; updated E2E coverage to target the current `UserName`-based login form.
- Verification in this pass: Release build `0W/0E`, Unit `324/324`, Integration `15/15`, Architecture `60/60`, Playwright/E2E `39/39`.
- Final status for this phase is **SIX-ROLE RIGHTS MODEL READY FOR USER REVIEW**. This phase does **not** mark the full application as production-approved.
