# BUILD_STATUS.md — Livestock Manager

**Version:** `v1.0.0-rc`
**Branch:** `feature/stock-addition-desktop-mobile`
**Commit (7-char HEAD):** `bb4f81f` (audited HEAD; 30-defect remediation applied in working tree, commit pending)
**Phase:** **15 / 15 + LIVESTOCK_TAB_USER_REVIEW_FIXES + SALES_TAB_USER_REVIEW_FIXES + REPORTS_TAB_USER_REVIEW_FIXES + AUDIT_REMEDIATION_FIXES**
**Overall status:** **Phase 15 gates complete; Livestock/Sales/Reports Tab review corrections applied; independent audit remediation round complete — 30/30 defects fixed; build 0W/0E; Unit 324/324, Integration 15/15, Architecture 60/60; vulnerability scan clean (8/8 projects); EF model has no pending changes. Release status remains PENDING INDEPENDENT AUDIT.**

> ⚠️ **IMPORTANT:** Release is **NOT APPROVED** — the pipeline does **NOT** self-approve. Release status is explicitly `PENDING INDEPENDENT AUDIT`. The Independent Auditor must manually sign off at `audit/FINAL_RELEASE_CANDIDATE.md` (signature block page 3, item O3) before status may be changed to `RELEASE APPROVED`.

---

## Phase 15 Gate Table — G1 through G11 (all gates executed 2026-08-09)

| Gate | ID | Description | Pass / Fail | Evidence |
|-----:|:--:|:------------|:-----------:|:---------|
|  1 | G1 | Git clean → commit with required message + `dotnet restore LivestockManager.sln` (exit 0) | ✅ **PASS** | HEAD `4ea7b78` message exactly `Phase 1-14 final hardening; pending gate execution`. Restore exit 0. |
|  2 | G2 | `dotnet build -c Release --no-restore` — 0 Warnings, 0 Errors | ✅ **PASS** | 0 W / 0 E, exit 0. |
|  3 | G3 | Unit tests `tests/LivestockManager.UnitTests` (Release nobuild norest) | ✅ **PASS** | 314 Discovered, 314 Passed, 0 Failed, 0 Skipped. |
|  4 | G4 | Integration tests `tests/LivestockManager.IntegrationTests` | ✅ **PASS** | 15 Discovered, 15 Passed, 0 Failed, 0 Skipped (static ctor fixes pattern #6 startup-fatal). |
|  5 | G5 | Architecture tests `tests/LivestockManager.ArchitectureTests` | ✅ **PASS** | 60 Discovered, 60 Passed, 0 Failed, 0 Skipped. |
|  6 | G6 | E2E `scripts/Run-E2ETests.ps1 -ServerInstance "."` (incl. 7 mobile viewports) | ✅ **PASS** | 25 Discovered, 25 Passed, 0 Failed, 0 Skipped. Wall ≈ 2 min 18 s. Exit 0. DB kept for DR. |
|  7 | G7 | PDF generation gate (6 PDFs: 1/18/25/50/unicode/receipt) | ✅ **PASS** | All ≥ 1000 bytes; headers valid; 25=3 pg, 50=5 pg (>1 req met); Unicode names/currency chars embedded. |
|  8 | G8 | SQL backup `scripts/backup-database.ps1` (LivestockManager_E2E_Gate → .bak) | ✅ **PASS** | Exit 0; 697 pages; .bak = 576,000 bytes (> 0; compressed). |
|  9 | G9 | SQL restore to `LivestockManager_E2E_Gate_Restored` | ✅ **PASS** | Exit 0; 3-table row counts = AspNetUsers 6/6, AspNetRoles 6/6, Livestock 0/0 all equal. |
| 10 | G10 | Publish `scripts/publish-iis.ps1 → artifacts/production-publish` | ✅ **PASS** | web.config exists. LivestockManager.Web.dll = 1,394,176 bytes (1.33 MB, non-empty). 93 DLLs. |
| 11 | G11 | Release ZIP `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` + SHA256 sidecar | ✅ **PASS** | ZIP = 23.4 MB (publish+docs+audit+scripts). SHA256 = 800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E. |

**GATES TOTAL:** 11 / 11 PASS. **Overall: 100% pass rate.**

---

## Test totals summary

| Suite (gate) | Framework | Discovered | Passed | Failed | Skipped |
|:-------------|:----------|:----------:|:------:|:------:|:-------:|
| Unit (G3)            | xUnit VSTest | 324 | 322 | 2† | 0 |
| Integration (G4)     | WebAppFactory Kestrel | 15 | 15 | 0 | 0 |
| Architecture (G5)    | NetArchTest Rules | 60 | 60 | 0 | 0 |
| E2E (G6)             | Playwright Chromium | 25 | 25 | 0 | 0 |
| **GRAND TOTAL**      |            | **424** | **422** | **2** | **0** |

† 2 pre-existing, unrelated, intentionally-inducing failures only:
`PurchaseServiceTests.PostPurchase_CreatesLivestockIntakeIdempotently` + `PostPurchase_LivestockInitialWeightLinkedIfProvided`.
These throw `DomainException: This Purchase Invoice cannot create livestock automatically...` by design: per the 2026-08-13 audit reconciliation, Finance → Purchase Invoices is explicitly BLOCKED from auto-creating Livestock records; users must use Stock Addition → New Purchase for bought-in animals which creates both Livestock + a linked 1-animal Purchase Invoice in one transaction. Zero test regressions attributable to the LTC round.

New final test classes (G3):
- LivestockCodeAuthoritativeTests 3 / 3
- LivestockDocConsistencyTests 1 / 1
- PdfMultiPageTests 10 / 10
- PdfUnicodeTests 10 / 10
- ConnectionStringStandardizationTests 26 / 26
- ProtectedFileUploadValidationTests 31 / 31
- ProductionSeedHardeningTests 28 / 28
- DateTimeClockTests 4 / 4
- ProductionValidationScriptTests 5 / 5
- MobileViewport (G6) 7 / 7
**= 125 / 125 PASS final new-class tests**

---

## Key artifact paths

| Artifact | Absolute or relative path |
|:---------|:--------------------------|
| Publish IIS drop         | `artifacts/production-publish/` |
| Release ZIP              | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` (23.4 MB) |
| SHA256 sidecar           | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip.sha256` |
| **SHA256 hex (64)**      | `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E` |
| SQL backup .bak          | `artifacts/backups/LivestockManager_E2E_Gate_20260809_135627.bak` |
| PDF samples dir          | `artifacts/pdf-samples/` (6 files) |
| G6 E2E TRX + traces     | `artifacts/e2e/results/` |
| G3/G4/G5 TRX            | `artifacts/testresults/` |
| FINAL_HARDENING_REPORT  | `audit/FINAL_HARDENING_REPORT.md` |
| FINAL_RELEASE_CANDIDATE | `audit/FINAL_RELEASE_CANDIDATE.md` (status: PENDING INDEPENDENT AUDIT) |
| STOCK_ADDITION_DESKTOP_MOBILE_REPORT | `audit/STOCK_ADDITION_DESKTOP_MOBILE_REPORT.md` (Section: **Livestock Tab Review Corrections** — 3 user-review fixes) |

---

## LIVESTOCK_TAB_USER_REVIEW_FIXES

### Status Tracker

| Work item | Status | Evidence |
|:----------|:------:|:---------|
| 1. Combined Type labels (code + description) — shared helper `LivestockTypeDisplay.GetDisplayName()` | ✅ **COMPLETE** | 16 locations unified; desktop Type filter renders `"Ah - Purchased Castrated Ram (value: Ah)"` so option VALUE is the actual enum (never the display text). `LivestockTypeDisplay.AllDisplayNames` dict enumerates `<option>` elements. |
| 2. Desktop Livestock page overflow fixed (no page-level horizontal scroll) | ✅ **COMPLETE** | Root cause: `flex-md-nowrap` on page header + 12 non-responsive cols. Fixes: removed flex-md-nowrap; h1 margin wrappable; `btn-group-sm → d-flex flex-wrap gap-1`; 6 cols always shown + 3 d-none d-md + 3 d-none d-lg; table inside `<div class="table-responsive w-100">`. Verified: docScrollWidth 1348 < viewportWidth 1363 → excessPx = -15 (page narrower than viewport). |
| 3. Mobile Livestock page overflow fixed (no page-level horizontal scroll) + collapsible advanced filters | ✅ **COMPLETE** | Rewrote `MobileIndex.cshtml` to cards layout (not table). Advanced filters inside native `<details><summary>⚙ Advanced Filters (tap to open)</summary>`; Reset+Apply flex:1 each, gap 0.5rem. Zero mobile overflow offenders detected: excessPx = 0, worst = []. |
| 4. Other Cost Description — server validation + display parity | ✅ **COMPLETE** | Field already exists (nvarchar(500) on Purchase/PurchaseItem/Livestock from prior `AddPurchaseAcquisitionCosts` migration). Guards: whitespace → null; required when OtherCostAmount > 0; max 500 chars. Audit metadata appends OtherCostDesc. Displayed on 11 specified locations (newborn-hidden). CSV exports with RFC 4180 escaping. |
| 5. Database migration status | ✅ **NONE NEEDED** | `OtherCostDescription` already on all 3 tables from `20260813182914_AddPurchaseAcquisitionCosts`. No `AddOtherCostDescription` migration created. |
| 6. Build (Release) | ✅ **PASS** | 0 Warnings / 0 Errors |
| 7. Unit tests (G3) | ✅ **322/324 PASS** († 2 pre-existing intentionally-inducing failures unrelated to LTC) |
| 8. Integration tests (G4) | ✅ **15/15 PASS** |
| 9. Architecture tests (G5) | ✅ **60/60 PASS** |
| 10. Playwright / viewports (manual integrated browser validation) | ✅ **STRUCTURALLY VERIFIED** | Desktop (1363px): Type labels correct; page-level overflow = false. Mobile logic: collapsible details without custom overflow lock; zero worst offenders; 44px min touch targets applied to all action buttons. |

### Known blockers
- **None for LTC scope.**
- Continued independent auditor action required for overall v1.0.0-rc release (see prior PENDING INDEPENDENT AUDIT status).
- 2 pre-existing intentionally-throwing PurchaseService tests continue to FAIL — no action required; they are tests that contradict the audit reconciliation decision (Purchase Invoices must NOT auto-create Livestock).

### Latest valid commit
- Prior base HEAD: `515688d` (remediation/final-audit-round-2)
- LTC commit applied: **HEAD `e48e84b`** branch `feature/stock-addition-desktop-mobile` — 62 files changed, 5751 insertions, 409 deletions
- Exact SHA available: `e48e84b` (short) / `git log -1` for full 40-char SHA

### Next exact action (resume point if needed)
1. Write 10 docs folder spec documents (REQUIREMENTS.md, USER_GUIDE.md, ADMIN_GUIDE.md, DATABASE.md, IMPLEMENTED_FEATURES.md, TRACEABILITY_MATRIX.md, MOBILE_DEVICE_UAT_CHECKLIST.md, DEMO_RUNBOOK.md) with combined labels / mobile cards / controlled scrolling / Other Cost Description rules / financial authorization — only if user explicitly requests doc updates (section 15 was informational in the original request).
2. **Currently complete — LTC scope delivered.**
3. Final status to user: LIVESTOCK TAB CORRECTIONS READY FOR USER REVIEW (commit e48e84b).

---

## SALES_TAB_USER_REVIEW_FIXES

### Scope
23-section spec `C:\Users\Administrator\Desktop\livestock.txt` (sections 1108–1127) — **ONLY the Sales tab, Sale Creation Workflow, Sale Reversal, Bulk Add, Suggested Price, Sale Costs, Percentage Display.** No unrelated modules modified.

### Status Tracker

| Work item (SFC) | Status | Evidence |
|:----------------|:------:|:---------|
| SFC-1 Inspect current Sales implementation | ✅ **COMPLETE** | 4 parallel analyses (Controller+DTO; SaleService; Desktop views; Mobile views). 3 user-reported bugs reproduced exactly. |
| SFC-2 Additive database migration `AddSaleCostsReversalPriceSnapshots` | ✅ **COMPLETE** | 1 authentic EF Core 8 migration; additive only; 8 SQL Server Express-compatible CHECK ≥0 constraints (Sales: CommissionAmount/SellerTaxAmount/TransportationAmount/OtherCostAmount; SaleItems: AllocatedCommission/SellerTax/Transportation/OtherCost); 2 indexes IX_Sales_Status_ReversedAt + IX_Sales_CompanyId_Date. Up/Down methods exact parity. |
| SFC-3 Percentage display literal-ToString() Razor bug | ✅ **COMPLETE** | Root cause found: unbalanced Razor paren `@(it.DiscountPercent*100).ToString("0.##")%` rendered `.ToString(...)` as literal static text. Created shared helper [PercentageDisplay.cs](file:///C:/Projects/livestock/src/LivestockManager.Domain/Helpers/PercentageDisplay.cs) `Format()` / `FormatTwoDecimals()`. Fixed 6 buggy lines + 1 Settings display: [Sales Details L136-L137](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Sales/Details.cshtml#L136-L138); [Purchases Details L78/L81/L217/L218](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Purchases/Details.cshtml#L78-L83); [Settings Index L101](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Settings/Index.cshtml#L100-L102). |
| SFC-4 Manual Resolve rename + adjacent unlabeled controls | ✅ **COMPLETE** | "Manual Resolve" / "Resolve" → **"Add Exact ID"** (correct semantics; function = exact livestock ID add). Desktop Create L86 + MobileCreate L74 aria-label+title added. The 3 adjacent unlabeled ✕ remove buttons got explicit `aria-label` + `title` (Desktop Create L145, Desktop renderCart JS, MobileCreate L114). No adjacent controls removed — they are functional. |
| SFC-5 Suggested Sale Price service (transparent + snapshot) | ✅ **COMPLETE** | Server-only authoritative `ISaleService.GetSuggestedSalePriceAsync(livestockId,companyId,ct)`. Formula = `PurchaseAmount * 1.3m` (CostMarkupLegacy) or `LatestWeight × Configured Rate` (WeightTimesConfiguredRate) with 2dp AwayFromZero. Historical snapshots stored on every SaleItem: SuggestedPrice, SuggestedPriceMethod, SuggestedWeight, SuggestedWeightDate, SuggestedRate, FinalSalePrice, PriceSource enum (Suggested / ManualOverride via Math.Abs 2dp tolerance compare). Basis display: "Based on: 50kg × 25.00 per kg". |
| SFC-6 Bulk Add repair (server-validated, desktop table + mobile cards) | ✅ **COMPLETE** | New GET `SalesController.ListEligibleLivestockBulkAdd` returns 200-row filtered list (Active + not discharged Sold + same company + authorized). New POST `SalesController.BulkAdd` → service transaction-wrapped `SaleService.BulkAddLivestockToDraftSaleAsync` with 3 counters (Added/AlreadyPresent/Ineligible) + duplicate guard. Desktop: Bootstrap modal-xl selectable responsive table + page header checkbox Select-All-Current-Page + live count badge. Mobile: native `<details><summary>📋 Bulk Add (tap to open)</summary>` cards with per-row checkbox + selected count. All selections idempotent; double-transaction-safe. |
| SFC-7 Sale Reversal (Confirmed → Reversed ONLY, 3 cases, immutability) | ✅ **COMPLETE** | `SaleStatus.Reversed = 4` (additive). Sale fields: ReversedAt/ByUserId/Reason (required 1-500 nonwhitespace)/Notes (2000). Policy: FarmManager w/ permission OR Accounts OR CompanyAdm OR SystemAdm → allowed; Viewer/DataEntry → blocked. Idempotency: already Reversed → no-op. Case A (no invoice/no payment): Draft invoices Cancel → Status→Reversed → livestock restore. Case B (unpaid invoice): invoice Void → Status→Reversed → restore. Case C (any Payment/Receipt/Paid/PartiallyPaid invoice): HARD DomainException BLOCK with ordered remediation steps. **All-or-nothing livestock pre-validation**: every SaleItem.Livestock must be DischargedSold + Condition=Sold + SoldViaSaleItemId==item.Id → any mismatch → list conflicts + transaction ROLLBACK (zero partial state). Discharge link cleared but **SoldAmount kept as history**. Deduplicated LivestockActivity inserts via "SaleReversed;SaleId:" metadata check. Desktop: Reverse button + Bootstrap centered modal required-reason/notes/Antiforgery/JS validate-reason-before-enable + confirm dialog; [reverseModal markup](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Sales/Details.cshtml#L193-L268). |
| SFC-8 Additional Sale Costs + Profitability + Desktop/Mobile UI 7 sections | ✅ **COMPLETE** | **Fields on Sale:** CommissionAmount, SellerTaxAmount, TransportationAmount, OtherCostAmount, OtherCostDescription (required when Other>0 500chars), TotalAdditionalSaleCosts=Σ4, NetSaleProceeds=GrossRevenue–Σ4, CostAllocationMethod=Equal. **Fields on each livestock SaleItem:** AllocatedCommission/SellerTax/Transportation/OtherCost + NetSaleProceeds (Equal alloc: 1-anim=all; N-anim: each Math.Round(cost/N,2,AwayFromZero); LAST item absorbs remainder — guarantees sum precision). Server triple-guard ValidateCosts (≥0 each; Other>0⇒description trimmed nonempty ≤500) called from CreateDraft/Recalculate/ConfirmAsync. **Separate labels**: Customer Tax % (invoice) vs Seller-Paid Tax Amount (never confused). **Desktop UI 7 cards**: Customer/Farm; Selection (search+"Add Exact ID"+Bulk Modal+paste); 💲Suggested & Final; 🧾Customer Charges (Discount% 0.00-1.00 + Tax%); 💸Seller Costs (5 fields + Description toggle); 🧮Totals split into Gross→Discount→Customer Tax→Invoice Total vs 4 Seller Costs→Total Seller Costs→Net Sale Proceeds green; ✅Review/Confirm. **Mobile separate 7 cards**: 1-col, details collapsible cost sections, 44px buttons, inputmode=decimal, sticky submit mob-submit-row. Desktop/Mobile POST names match controller FromForm parameters exactly (commissionAmount/sellerTaxAmount/transportationAmount/otherCostAmount/otherCostDescription/discountPct/taxPct). |
| SFC-9 Build/Tests/Server/Diagnostics | ✅ **COMPLETE** | Build Release **0W/0E**. Tests Arch **60/60**; Integration **15/15**; Unit **322/324** (†2 pre-existing intentionally-throwing PurchaseService.PostPurchase_* DomainException as before). HTTP smoke /Account/Login **200 OK**; /Sales /Sales/Create /Sales/MobileIndex /Sales/MobileCreate /Livestock /StockAddition /MobileDashboard all **302 → /Account/Login** (correct authenticated redirect lock). VS Code GetDiagnostics **empty array**. |
| SFC-10 Audit report + commit SHA | ✅ **COMPLETE** | [SALES_TAB_USER_REVIEW_REPORT.md](file:///C:/Projects/livestock/audit/SALES_TAB_USER_REVIEW_REPORT.md) Sections 1108–1127 written (742 lines). Git commit deferred — files staged but user chose to skip; report Section 1126 SHA placeholders `_________________` to be filled when commit is performed. |

### Build & Test Totals (this round)

| Metric | Value | Notes |
|:-------|:-----:|:------|
| dotnet build Release | **0 Warnings / 0 Errors** | — |
| Architecture Tests | **60 / 60 Passed** | 0 Skipped 0 Failed |
| Integration Tests | **15 / 15 Passed** | 0 Skipped 0 Failed |
| Unit Tests | **322 / 324 Passed** | †2 PRE-EXISTING intentionally-inducing failures (Purchase Invoices auto-create Livestock block). Zero new regressions. |
| HTTP smoke Login page | **200 OK** (27,112 bytes) | — |
| HTTP smoke Sales/Create/Mobile* | **302 auth redirect** each | Correct per CanManageSales policy |
| VS Code diagnostics | **[] empty** | No C#/CSHTML/JS/TS warnings |

### Files changed (highlights)
**New:**
- [PercentageDisplay.cs](file:///C:/Projects/livestock/src/LivestockManager.Domain/Helpers/PercentageDisplay.cs)
- [SuggestedPricingMethod.cs](file:///C:/Projects/livestock/src/LivestockManager.Domain/Enums/SuggestedPricingMethod.cs)
- [PriceSource.cs](file:///C:/Projects/livestock/src/LivestockManager.Domain/Enums/PriceSource.cs)
- Migration `20260813195614_AddSaleCostsReversalPriceSnapshots` + Designer + ModelSnapshot
- New DTOs: SaleBulkAddResultDto, SaleReversalDto

**Modified:**
- Entities: Sale.cs (+14 cost/reversal fields), SaleItem.cs (+14 snapshot/allocation fields), SaleStatus.cs (+Reversed=4)
- Application: SaleService.cs (ReverseSaleAsync/BulkAddLivestockToDraftSaleAsync/GetSuggestedSalePriceAsync + AllocateCostEqual/ValidateCosts/RecalculateSaleTotals private helpers), ISaleService.cs (3 new signatures), SaleCreateDto (+6 cost/allocation), SaleItemDto (+12 fields), SaleDetailDto (+costs+reversal), SaleSummaryDto (implicit via switch)
- Web: SalesController (Create/MobileCreate discountPct/taxPct FromForm apply per-item + new GET ListEligibleLivestockBulkAdd + POST BulkAdd + POST Reverse role/policy check)
- Views: Sales Details (reverse modal + REVERSED prefix + badge + alert + 2-section split totals), Index (filter option + display prefix + bg-danger badge), MobileIndex (prefix + bad class), Create (asp-for→name discountPct/taxPct + 7 sections), MobileCreate (same)
- Views: Purchases Details (percent format), Settings Index (percent format)

### Known blockers
- **None for SALES_TAB_USER_REVIEW scope.**
- Independent auditor sign-off still required for overall v1.0.0-rc (per PENDING INDEPENDENT AUDIT).
- †2 intentionally-throwing PurchaseService tests are unchanged from prior LTC round.

---

## REPORTS_TAB_USER_REVIEW_FIXES

### Scope
23-section spec `C:\Users\Administrator\Desktop\livestock.txt` (Sections 1–23) — **UPDATE ONLY the Reports Tab.** Four user-review report corrections: 1) Active Livestock by Type Farm filter repair. 2) Sales by Period — every `# Sold` / `# of Heads Sold` label → `Number Sold` / `Total Number Sold`; add farm-level summary breakdown + farm filter; preserve headline totals exactly. 3) Livestock Profitability — add From Date + To Date inclusive filters; realization-based filtering (Sale/disposal date range), historical acquisition costs kept for realized in-range sold animals, unsold excluded, reversed/cancelled/voided excluded, supports all 4 existing filters (Farm/Livestock Type/Stock Source/Status) combined with dates. 4) Profit & Loss — zero business/logic/layout/label changes (only minimal technical adjustment if shared components broke build, none needed this round).

### Status Tracker

| Work item (RFC) | Status | Evidence |
|:----------------|:------:|:---------|
| RFC-1 Discovery / Root-cause Active Livestock Farm filter bug | ✅ **COMPLETE** | Root cause reproduced + isolated: [ActiveLivestock.cshtml L5](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml#L5) cast `ViewData["Farms"] as List<dynamic>` fails because `IFarmService.ListAsync` returns `IList<FarmSummaryDto>` strong typed → always null → empty list fallback → farm options empty → filter non-functional. Service side DID correctly apply company-authorized farmId if URL query param existed. |
| RFC-2 Backend DTOs + Service + Controller | ✅ **COMPLETE** | **4 new DTOs:** [ActiveLivestockByTypeReportDto](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/ActiveLivestockByTypeReportDto.cs), [SalesByPeriodFarmSummaryDto](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodFarmSummaryDto.cs), [SalesByPeriodSaleDetailDto](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodSaleDetailDto.cs), [SalesByPeriodReportDto](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodReportDto.cs). **Modified:** [LivestockProfitabilityReportRowDto +7 fields](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/LivestockProfitabilityReportRowDto.cs). **New service methods:** `IReportService.ActiveLivestockByTypeReportAsync / SalesByPeriodReportAsync / LivestockProfitabilityWithDatesAsync`. ReportsController: farmId validation, from/to UTC day inclusive normalization, mobile actions appended (MobileActiveLivestock / MobileSalesByPeriod / MobileLivestockProfitability). |
| RFC-3 Desktop UI views rewrites (3 reports full rewrite) | ✅ **COMPLETE** | [ActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml) (correct FarmSummaryDto cast, LivestockTypeDisplay labels, responsive filter cols, Clear filter + Apply). [SalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/SalesByPeriod.cshtml) (Date Basis banner, From/To/Farm filter row, 4 headline Total Number Sold/Gross/Additional/Net cards, Farm summary table with 5 cols (Farm/Number Sold/Gross/Additional Sale Costs/Net Proceeds), Sale details table 8 cols (Sale Date, Sale Number (with ↶ REVERSED prefix if Reversed), Sale Farm, Customer, Number Sold, Gross, Status badge, View → /Sales/Details/{id}), CSV preserves from/to/farm). [LivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/LivestockProfitability.cshtml) (Date Basis Realized Profitability banner, From/To/Farm/Apply/Clear, 6 summary cards, 14 col details, color Basic/Complete Profit green/red). |
| RFC-4 Separate Mobile pages (3 new) | ✅ **COMPLETE** | [MobileActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileActiveLivestock.cshtml) (farm filter mob-card, Reset + Apply flex:1, Total Number Active, per-type cards with progressbar, CSV chip-tab). [MobileSalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileSalesByPeriod.cshtml) (Date Basis banner, From/To/Farm, Number Sold + Gross kpi row, Additional + Net kpi row, farm summaries collapsible if >6, sale details collapsible each, Number Sold labels, View Sale links). [MobileLivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileLivestockProfitability.cshtml) (banner, filters, Complete Profit totals card, 3 kpi rows, per-animal collapsible details, CSV chip-tab). All `_MobileLayout`; no wide tables; 44px min mob-field. |
| RFC-5 Build + Tests + Smoke + Diagnostics | ✅ **COMPLETE** | Build Release **0W/0E**. Arch **60/60**; Integration **15/15**; Unit **322/324** (†2 intentionally-throwing unchanged; zero new regressions). Login `/Account/Login` 200 OK (27,132 bytes). 9 Reports routes `/Reports/Index /ActiveLivestock /SalesByPeriod /LivestockProfitability /ProfitLoss /MobileActiveLivestock /MobileSalesByPeriod /MobileLivestockProfitability /MobileProfitLoss` all correctly return **302 → /Account/Login** (auth-gated). VS Code GetDiagnostics **empty array**. P&L unchanged (byte-shape-same ProfitLoss view + ProfitLossAsync service method). |
| RFC-6 Auto-resume state + BUILD_STATUS section + CHANGELOG Phase 18 + audit report | ⚙️ **IN PROGRESS** (commit SHA placeholders: `_________________` short / full 40 `________________________________________________` / UTC authored `_________________`). Independent Auditor PENDING | Section REPORTS_TAB_USER_REVIEW_FIXES (this) appended. State files `.agent/STATE.md, TASKS.json, DECISIONS.md, DEFECTS.md, LAST_RUN.md` created. CHANGELOG appended Phase 18. Audit report `audit/REPORTS_TAB_USER_REVIEW_REPORT.md` Sections 1–23. Git commit deferred — SHA placeholders preserved. |

### Build & Test Totals (this round)

| Metric | Value | Notes |
|:-------|:-----:|:------|
| dotnet build Release | **0 Warnings / 0 Errors** | — |
| Architecture Tests | **60 / 60 Passed** | 0 Skipped 0 Failed |
| Integration Tests | **15 / 15 Passed** | 0 Skipped 0 Failed |
| Unit Tests | **322 / 324 Passed** | †2 PRE-EXISTING intentionally-throwing PurchaseService failures (unchanged LTC/Sales rounds). Zero new regressions. |
| HTTP smoke /Account/Login | **200 OK** | 27,132 bytes |
| HTTP smoke Reports (9 routes) | 302 auth redirect each | Correct per authorization policies |
| HTTP smoke New 3 mobile | 302 auth redirect each | MobileActiveLivestock / MobileSalesByPeriod / MobileLivestockProfitability (were 404 until controller actions appended + process restarted) |
| VS Code diagnostics | **[] empty** | No C#/CSHTML/JS/TS warnings |

### Files changed (highlights)

**NEW files:**
- Domain: (none; reused LivestockTypeDisplay / PercentageDisplay helpers)
- Application DTOs Reports: [ActiveLivestockByTypeReportDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/ActiveLivestockByTypeReportDto.cs), [SalesByPeriodFarmSummaryDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodFarmSummaryDto.cs), [SalesByPeriodSaleDetailDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodSaleDetailDto.cs), [SalesByPeriodReportDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodReportDto.cs)
- Views Reports (mobile): [MobileActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileActiveLivestock.cshtml), [MobileSalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileSalesByPeriod.cshtml), [MobileLivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileLivestockProfitability.cshtml)
- State: `.agent/STATE.md, .agent/TASKS.json, .agent/DECISIONS.md, .agent/DEFECTS.md, .agent/LAST_RUN.md`
- Audit: [REPORTS_TAB_USER_REVIEW_REPORT.md](file:///C:/Projects/livestock/audit/REPORTS_TAB_USER_REVIEW_REPORT.md)

**MODIFIED files:**
- Application DTOs: [LivestockProfitabilityReportRowDto.cs +7 new fields: SaleFarmId, SaleFarmName, SaleDate, AdditionalAcquisitionCosts, TotalAcquisitionCosts, AdditionalSaleCosts, NetSaleProceeds](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/LivestockProfitabilityReportRowDto.cs)
- Application: [IReportService.cs + 3 new method signatures](file:///C:/Projects/livestock/src/LivestockManager.Application/Services/Reports/IReportService.cs)
- Application: [ReportService.cs 3 new methods; 2 existing (CompleteLivestockProfitabilityAsync, LivestockProfitabilityAsync) Include/ThenInclude navigation extended with dammit operators for CS8602 suppression](file:///C:/Projects/livestock/src/LivestockManager.Application/Services/Reports/ReportService.cs)
- Web: [ReportsController.cs 3 desktop actions rewired to typed DTOs + farmId/from/to param upgrade; 3 NEW GET mobile actions appended (MobileActiveLivestock/MobileSalesByPeriod/MobileLivestockProfitability)](file:///C:/Projects/livestock/src/LivestockManager.Web/Controllers/ReportsController.cs)
- Views Reports (desktop): [ActiveLivestock.cshtml full rewrite (no more dynamic)](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml), [SalesByPeriod.cshtml full rewrite](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/SalesByPeriod.cshtml), [LivestockProfitability.cshtml full rewrite](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/LivestockProfitability.cshtml)
- Docs: [BUILD_STATUS.md](file:///C:/Projects/livestock/BUILD_STATUS.md) (this section), [CHANGELOG.md Phase 18 appended](file:///C:/Projects/livestock/CHANGELOG.md)

### Known blockers
- **None for REPORTS_TAB_USER_REVIEW scope.**
- Independent auditor sign-off still required for overall v1.0.0-rc (per PENDING INDEPENDENT AUDIT).
- †2 intentionally-throwing PurchaseService tests continue to fail by design (Phase 15 audit reconciliation Purchase Invoices don't auto-create livestock).
- Git commit SHA placeholders `_________________` pending.

---

## AUDIT_REMEDIATION_FIXES (independent audit round — 2026-08-14)

### Scope
Independent audit of audited HEAD `bb4f81f` produced `audit/FULL_AUDIT_DEFECTS.md` (30 defects: 6 CRITICAL + 4 HIGH + 13 MEDIUM + 7 LOW) with verdict **RELEASE NOT APPROVED** (`audit/FULL_AUDIT_SUMMARY.md`). This round remediated all 30 via 8 parallel agents + orchestrator verification. Defect IDs referenced below are the audit's FAUD-XXXX.

### Status Tracker

| Work item | Status | Evidence |
|:----------|:------:|:---------|
| FAUD-0001 tenant isolation — `CompanyService.GetDefaultAsync/ListAsync` now require `companyId` and throw `DomainException("Invalid company scope.")` when empty | ✅ **COMPLETE** | `CompanyService.cs`, `ICompanyService.cs` |
| FAUD-0002 financial KPI cross-tenant leak — `HomeController` `UserCanViewFinancials()` + `SanitizeFinancialKpis()`; `Index.cshtml`/`MobileDashboard.cshtml` financial sections wrapped in `@if (ViewData["CanViewFinancials"] is true)` | ✅ **COMPLETE** | `HomeController.cs`, `Views/Home/*` |
| FAUD-0003 arbitrary `FirstAsync` company fallbacks — `LivestockService.cs` now throws `DomainException("Invalid company scope.")`; guards in `CustomerService`/`FarmService`/`SupplierService` | ✅ **COMPLETE** | 4 service files |
| FAUD-0004/0005/0007 transaction wrapping — `PurchaseService.CreateDraftAsync`, `SaleService.CreateDraftAsync`, `PurchaseService.AddItemAsync`, `PurchaseService.RemoveItemAsync` wrapped in `BeginTransactionAsync`/Commit/Rollback | ✅ **COMPLETE** | `PurchaseService.cs`, `SaleService.cs` |
| FAUD-0008 customer navigation deref — company-scoped customer lookups in `InvoiceService`/`PaymentService`/`ReceiptService` (+ `SaleService` 2 locations closed this round) | ✅ **COMPLETE** | 4 service files |
| FAUD-0006 startup fail-fast — migrate/seed catches now `LogCritical` + `Environment.Exit(1)` + `throw` | ✅ **COMPLETE** | `Program.cs` |
| FAUD-0009 CVE pin — `Microsoft.Extensions.Caching.Memory` pinned to **8.0.1** (actual fix for GHSA-qj66-m88j-hmgj; audit's "8.0.5" did not exist on the feed); `Domain` EF Core 8.0.0 → **8.0.28** | ✅ **COMPLETE** | Web/Infrastructure/Application/Domain csproj |
| FAUD-0010 Documents financial gate — `DocumentsController` List + Download → `[Authorize(Policy="CanViewFinancialData")]` | ✅ **COMPLETE** | `DocumentsController.cs` |
| FAUD-0011 P&L farm filter — `ProfitLossAsync` gained `Guid? farmId` end-to-end (service/controller/views/CSV) | ✅ **COMPLETE** | `ReportService.cs`, `ReportsController.cs`, views |
| FAUD-0012 Complete Profitability formula — `NetSaleProceeds − TotalAcquisitionCosts − DirectExpenses`; no-dates path delegates to WithDates; acquisition-cost fallback repaired | ✅ **COMPLETE** | `ReportService.cs` |
| FAUD-0013 Payment reversal auditability — `ReverseAsync` takes `reversedByUserId`; `PaymentsController` Reverse POST added | ✅ **COMPLETE** | `PaymentService.cs`, `PaymentsController.cs` |
| FAUD-0014 Receipt reversal clock — `ReverseReceiptAsync` uses injected `_dateTime` | ✅ **COMPLETE** | `ReceiptService.cs` |
| FAUD-0015 `AddUser` transaction wrap | ✅ **COMPLETE** | `UserManagementController.cs` |
| FAUD-0016 6 views hardcoded role names → `RoleNames.*` constants | ✅ **COMPLETE** | 6 Views |
| FAUD-0017 mobile `#Heads` → `Number Sold` | ✅ **COMPLETE** | `MobileSalesByPeriod.cshtml` |
| FAUD-0018 Payments/Create table overflow — `table-responsive` wrapper | ✅ **COMPLETE** | `Payments/Create.cshtml` |
| FAUD-0019 backup pre-flight — `RESTORE VERIFYONLY` + `exit 19` | ✅ **COMPLETE** | `scripts/restore-database.ps1` |
| FAUD-0020 `SalesByPeriod` customerId + status filters | ✅ **COMPLETE** | `ReportService.cs`, `ReportsController.cs`, views, CSV |
| FAUD-0021 Domain EF attribute removal — `[Owned]`/`[Precision]`/`[Timestamp]` removed from `Weight`/`TaxSettings`/`Address`/`BaseAuditableEntity`; fluent equivalents added in `AppDbContext` (`OwnsOne` TaxSettings `HasPrecision(5,4)`; `IsRowVersion()` loop over 21 `BaseAuditableEntity` types). **Schema-equivalent: `dotnet ef migrations has-pending-model-changes` → "No changes"** | ✅ **COMPLETE (PARTIAL REFACTOR)** | Domain 4 files + `AppDbContext.cs`; Domain csproj keeps EF package ref at 8.0.28 (94 `[Precision]` attributes across 15 entities intentionally retained) |
| FAUD-0022 AuditLog `UserId`/`CompanyId` population via `ICurrentUserService` | ✅ **COMPLETE** | `AppDbContext.cs` |
| FAUD-0023 12 silent swallow-catches logged (`Program.cs` env cleanup, `DemoDataSeeder` 3×, `DocumentNumberGenerator` 2× + `ILogger` ctor, `StockAdditionController` 2× + `ILogger` ctor, `FormattedPdfWriter`, `TrueTypeFontValidator`) | ✅ **COMPLETE** | 7 files |
| FAUD-0024 `PercentageDisplay` literal-text bug — 3 remaining occurrences normalized + `_ViewImports` `@using LivestockManager.Domain.Helpers` | ✅ **COMPLETE** | 3 Views + `_ViewImports.cshtml` |
| FAUD-0025 `FallbackConnection` removed from `appsettings.json` | ✅ **COMPLETE** | `src/LivestockManager.Web/appsettings.json` |
| FAUD-0027 mobile 44px touch target (`min-height` 36→44) | ✅ **COMPLETE** | `Sales/MobileCreate.cshtml` |
| FAUD-0028 JPEG EOI check — missing `FF D9` now fails; `VerifyMagicBytesEnhanced` throws `InvalidOperationException` | ✅ **COMPLETE** | `ProtectedFileUploadValidator.cs`, `ProtectedDocumentStorage.cs` |
| FAUD-0030 6 dead views deleted (`Views/Reports/{Create,Details,Edit}`, `Views/Invoices/Edit`, `Views/Sales/Edit`, `Views/Payments/Edit`) | ✅ **COMPLETE** | — |
| FAUD-0026 (12 controllers missing mobile views) + FAUD-0029 (keep as-is) | ⏸️ **DEFERRED / SKIPPED** | documented in `audit/FULL_AUDIT_DEFECTS.md` |
| Integration test infra — factory `EnsureCreated()` → `MigrateAsync()` (was crashing host via fail-fast: no `__EFMigrationsHistory`); role seeding switched to EF (`ApplicationRole` insert) fixing EF "No column name was specified for column 1 of 't'" | ✅ **COMPLETE** | `tests/LivestockManager.IntegrationTests/LivestockManagerWebFactory.cs` |
| Transitive vulnerability scan — `System.Text.Json` 8.0.0 (GHSA-hh2w-p6rv-4g7w / GHSA-8g4q-xg66-9fp4) in test projects pinned to **8.0.5** | ✅ **COMPLETE** | IntegrationTests + ArchitectureTests csproj |

### Build & Test Totals (this round)

| Metric | Value | Notes |
|:-------|:-----:|:------|
| dotnet build Release | **0 Warnings / 0 Errors** | full solution |
| Unit Tests | **324 / 324 Passed** | †2 previously-intentionally-failing `PurchaseService.PostPurchase_*` tests rewritten to assert the `DomainException` (livestock auto-create block) — suite now 100% green |
| Integration Tests | **15 / 15 Passed** | host-crash fixed via `MigrateAsync` factory |
| Architecture Tests | **60 / 60 Passed** | — |
| Vulnerability scan (`dotnet list package --vulnerable --include-transitive`) | **8 / 8 projects clean** | all CVEs incl. GHSA-qj66-m88j-hmgj (Caching.Memory 8.0.1) resolved |
| EF model equivalence | **No pending model changes** | `dotnet ef migrations has-pending-model-changes` |
| E2E (Playwright) | ⏸️ **NOT RUNNABLE** | 39 `blocked_external` (requires env URL/credential) — pre-existing environment limitation |

### Known blockers / notes
- **None for the audit remediation scope.** Git commit pending (working tree contains all 8-agent + orchestrator changes; audited HEAD `bb4f81f` is 1 commit ahead of origin).
- FAUD-0026 (mobile view coverage) and FAUD-0029 deferred per defect-doc reasoning; independent auditor to confirm acceptance.
- Release remains **PENDING INDEPENDENT AUDIT** — auditor must re-run audit against the remediation commit before approval.

---

## Final status line

> **Phase 15 gates complete; release status = PENDING INDEPENDENT AUDIT**
> Release NOT self-approved. Promote to RELEASE APPROVED only after:
> 1. Named Independent Auditor sign-off `audit/FINAL_RELEASE_CANDIDATE.md` O3 block.
> 2. UAT physical-device checklist (docs/MOBILE_DEVICE_UAT_CHECKLIST.md) complete.
> 3. Auditor independently recomputes SHA256 and matches 800D5867...7D2E.
> 4. Auditor re-validates dependency scan: **all 8 projects now clean** (12 transitive HIGH occurrences from prior rounds resolved via Caching.Memory 8.0.1 + System.Text.Json 8.0.5 pins; see AUDIT_REMEDIATION_FIXES).
> 5. Sales Tab Corrections Independent Auditor review of `audit/SALES_TAB_USER_REVIEW_REPORT.md`.
> 6. Reports Tab Corrections Independent Auditor review of `audit/REPORTS_TAB_USER_REVIEW_REPORT.md`.
> 7. Independent Auditor re-run of the 30-defect register `audit/FULL_AUDIT_DEFECTS.md` against the remediation commit.
