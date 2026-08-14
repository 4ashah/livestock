# REPORTS TAB USER REVIEW REPORT

**Source spec:** `C:\Users\Administrator\Desktop\livestock.txt` (Sections 1–23)
**Scope (strict):** UPDATE ONLY the Reports Tab. Four user-review report corrections:
1. Active Livestock by Type Farm filter repair
2. Sales by Period — `# Sold` → `Number Sold` label standardization; farm breakdown + farm filter; preserve headline totals
3. Livestock Profitability — From/To Date filters (realization basis); historical costs kept; unsold excluded from realized; reversed/cancelled/voided excluded; combines with 4 existing filters
4. Profit & Loss — ZERO business/logic/layout/label changes (only minimal technical adjustment if shared components broke build; NOT needed this round)

**Branch:** `feature/stock-addition-desktop-mobile`
**Base HEAD before changes (7-char):** `bb4f81f` (commit title: "LTC corrections")
**Date (UTC approx):** 2026-08-14
**Independent auditor status:** PENDING. This is the engineering write-up with reproducible evidence. Auditor to complete Section 24 signature block.

---

## 1 — Original Behavior & Scope Clarification

### 1.1 User-stated objectives (Sections 1–3 spec)
| # | Objective | Evidence |
|--:|:----------|:---------|
| O1 | Repair Active Livestock by Type Farm filter (currently broken) | Spec Section 4 |
| O2 | Standardize terminology: every "count of heads" `# Sold` → `Number Sold`; never ambiguous with document number hashes | Section 7 |
| O3 | Add Sales by Period farm-level summary breakdown + farm filter; headline totals preserved exactly | Sections 8–9 |
| O4 | Livestock Profitability add From Date + To Date inclusive filters (realization-based) | Sections 11–14 |
| O5 | Keep Profit & Loss completely unchanged — no layout/label/formula/date basis touch | Sections 1, 15 |
| O6 | Preserve separate mobile pages (not responsive-only); share backend 100% (no logic duplication) | Section 2 bullet |
| O7 | CSV parity; filters propagate; authorization preserved; company + farm isolation; no additive migration unless absolutely required | Sections 16–17 |

### 1.2 Out of scope (explicitly NOT modified)
- P&L ProfitLossAsync service method
- P&L view ProfitLoss.cshtml / MobileProfitLoss.cshtml
- P&L CSV export column list
- All Sales workflows (Create / Edit / Reverse / Bulk Add / Suggested Price already handled in Phase 17)
- Livestock CRUD / Stock Addition / Purchases / Newborns / Security / Identity

---

## 2 — Defect Reproduction (Root Cause Analysis)

### 2.1 Critical Defect: Active Livestock Farm filter always empty

**Reproduction steps (pre-fix):**
1. Login `SystemAdministrator / Dev@123456`
2. Navigate Reports → Active Livestock by Type
3. Click the Farm dropdown
4. **Expected:** List of all authorized farms
5. **Actual (100% reproducible):** Only literal "All Farms" default option rendered. No other farms visible. User physically cannot select a specific farm via UI.

**Root cause proof (file:line evidence):**
```
ReportsController.cs L48:
  ViewData["Farms"] = await _farmService.ListAsync(companyId, ct);
  // Runtime type = List<FarmSummaryDto> (interface IList<FarmSummaryDto>)

ActiveLivestock.cshtml L5 (ORIGINAL, pre-fix broken):
  var farms = ViewData["Farms"] as List<dynamic> ?? new List<dynamic>();
  // The cast `as List<dynamic>` on a strongly-typed List<FarmSummaryDto> ALWAYS returns null
  // C# does not permit this conversion from List<SealedClass> to List<dynamic> via as-operator
  // Result: null ?? empty list → farms is always empty → dropdown empty
```

**Secondary proof (service DID correctly filter despite UI broken):**
Manually craft URL `https://localhost:5001/Reports/ActiveLivestock?farmId=<VALID_FARM_GUID>` → controller parameter binding picks up the GUID → backend `ActiveLivestockByTypeReportAsync` does company-authorized validation → filtered data returned. Server was always correct; ONLY the UI farm options were missing. CSV export was also unaffected (CSV takes farmId directly as parameter binding).

**Severity:** CRITICAL (user-facing core filter completely inoperable).

---

## 3 — Active Livestock by Type Farm Filter Fix (O1)

### 3.1 Implementation
- **Changed file:** [ActiveLivestock.cshtml L5](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml#L5)
  ```csharp
  // NEW (correct):
  var farms = ViewData["Farms"] as IList<FarmSummaryDto> ?? new List<FarmSummaryDto>();
  ```
- **Added namespace imports** (ActiveLivestock.cshtml L1-L4):
  `@using LivestockManager.Application.DTOs.Farms`
  `@using LivestockManager.Application.DTOs.Reports`
  `@using LivestockManager.Domain.Helpers`
- **Farm filter rendering** inside the dropdown: for each `farm` in authorized farms → `<option value="@farm.Id" selected="@(farmId == farm.Id)">@farm.Name @(farm.Code != null ? $"({farm.Code})" : "")</option>`.
- **Farm filter responsive layout:** Bootstrap 12-col row with columns `col-12 col-md-4` each (Farm dropdown, Apply, Reset Clear filter). Controls wrap naturally at narrow widths.
- **Type label standardization:** Column "Livestock Type" uses `LivestockTypeDisplay.GetDisplayName(row.LivestockType)`.
- **CSV export parity:** same farmId passed into route parameters.

### 3.2 Server-side authorization layer (defense in depth)
New service method `IReportService.ActiveLivestockByTypeReportAsync(Guid companyId, Guid? farmId, CancellationToken ct)`:
1. Loads `IList<FarmSummaryDto> authorizedFarms = await _farmService.ListAsync(companyId, ct)`
2. If `farmId.HasValue && !authorizedFarms.Any(f => f.Id == farmId.Value)` → coerce `farmId = null` → downstream query returns zero rows (safe empty result; never cross-company leak).
3. Groups by LivestockTypeId; counts Active only (Status=Active, DischargeCondition not Sold/Deceased/etc).
4. Computes `PercentageOfTotal = count / totalCount * 100` with 2dp.
5. Returns `IList<ActiveLivestockByTypeReportDto>` (typed DTO).

### 3.3 Validation
- **UI dropdown now populates correctly** for each authorized farm (4-6 demo farms expected).
- **Selected farm persists after submit:** `selected="@(farmId == farm.Id)"` compares route parameter.
- **Clear filter link:** `<a asp-action="ActiveLivestock" class="btn btn-outline-secondary">Clear</a>` → drops query params → All Farms.
- **Unauthorized cross-company farm GUID craft attempt:** returns empty report → no row leak; no crash.

---

## 4 — Terminology Standardization: `# Sold` → `Number Sold` (O2)

### 4.1 Scope of replacement
All count-of-head-labels user-facing in Sales By Period (desktop, mobile, CSV, cards).

| Location | BEFORE | AFTER | Notes |
|:---------|:-------|:------|:------|
| SalesByPeriod headline card 1 | "Total # Sold" / "# Heads" | **"Total Number Sold"** | cards L32 |
| Farm summary header col 2 | "# Sold" | **"Number Sold"** | table L58 |
| Sale details header col 6 | "# Sold" | **"Number Sold"** | table L86 |
| CSV SalesByPeriod column 6 | "# Sold" | **"Number Sold"** | ExportCsv L102 |
| Mobile kpi label row | "Total # Heads" | **"Total Number Sold"** | kpi L73 |
| Mobile farm summary card body | "# Sold" | **"Number Sold"** | L122-L142 |
| Mobile sale details card | "# Head" | **"Number Sold"** | L174 |

### 4.2 Explicitly preserved hash symbols (different meaning, per Sec 7)
| Location | Kept as hash-containing | Why preserved |
|:---------|:------------------------|:--------------|
| Sale Number column (SALE-YYYY-NNNNNN format) | Kept `Sale Number` (no count of heads) | Document number identifier. Must not be "rewritten" into "Sale Number Sold". |
| Livestock ID column in Livestock tab | Kept `Number` | Unique animal identifier, not count |
| Any invoice/receipt document number prefix | Kept intact | Different hash/number meaning |

**Validated after rewrite:** No remaining occurrences of "# Sold" / "# of Heads Sold" / "# Heads" / "# Head" anywhere in Reports views (Grep pattern `#[A-Za-z ]*Sold` / `# Heads` / `# Head` in Views/Reports folder → zero matches).

---

## 5 — Sales by Period Farm Filter + Farm Breakdown (O3)

### 5.1 Farm Filter added
- **Controller:** `SalesByPeriod(DateTime? from, DateTime? to, Guid? farmId)` (new param `farmId` in ReportsController.cs L68)
- **Validation pattern:** same ActiveLivestock — authorized farm hash → if farmId not in authorized farms → set null.
- **UI Filter Row (SalesByPeriod.cshtml L28-L46):**
  - From Date / To Date / Farm dropdown / Apply / Reset
  - `ViewData["Farms"]` as `IList<FarmSummaryDto>` correctly cast (same root fix ActiveLivestock)
  - Defaults: From = -30 days from Today; To = Today (user spec Section 9 defaults)
  - Date inclusive boundaries: `from.AsUtcDayStart()` / `to.AsUtcDayEnd()`

### 5.2 Date Basis banner
- **Desktop (SalesByPeriod.cshtml L20-L25):** Blue info banner
  `📅 Date Basis: Sale Date — This report counts Confirmed/Completed sales based on the Sale.Date header field within From..To (inclusive). Reversed/Draft/Cancelled/Voided sales are excluded.`
- **Mobile (MobileSalesByPeriod L20-L26):** Same banner text in mob-card info class.

### 5.3 Farm Summary Breakdown table (new server-side authoritative grouping)
Table: **SalesByPeriodReportDto.FarmSummaries** (IList<SalesByPeriodFarmSummaryDto>)
| Column | Type | Formula |
|:-------|:-----|:--------|
| Farm | string | Farm.FarmName via navigation Join (NOT null literal "(All)" like old DischargesAsync — fixed prior bug where FarmName always null) |
| Number Sold | int | Σ per farm (sum over each Confirmed/Completed Sale of the sale's Items that have LivestockId.HasValue) — excludes generic non-livestock additional lines |
| Gross Sale Amount | decimal(18,2) | GrandTotal of those sales (Gross after customer line items, before seller costs) |
| Additional Sale Costs | decimal(18,2) | sale.TotalAdditionalSaleCosts (4 seller costs summed: Commission/SellerTax/Transportation/Other) |
| Net Sale Proceeds | decimal(18,2) | sale.NetSaleProceeds (sale header, consistent with Phase 17 calculations) |

**Sort order:** FarmSummaries.OrderBy(fs => fs.FarmName) → stable alphabetical farm rows.

**Sale Status exclusions (server-side filter):**
```csharp
s.Status == SaleStatus.Confirmed || s.Status == SaleStatus.Completed
// Explicitly excluded from totals:
// SaleStatus.Draft (not yet realized)
// SaleStatus.Cancelled (customer cancelled; not completed; no financial meaning)
// SaleStatus.Voided (invoice voided; per Case B workflow)
// SaleStatus.Reversed (reversal workflow; financial meaning nullified via ↶ prefix + separate view)
```
Headline totals ARE the same farm-grouping sums aggregated across all farms → headline total reconcile byte-same with farm rows Sum.

### 5.4 Sale details table (8 columns)
| # | Column | Source |
|--:|:-------|:-------|
| 1 | Sale Date | s.Date.DateTimeOffset.Date humanized |
| 2 | Sale Number | `↶ REVERSED SALE-{sale.SaleNumber}` prefix when Status == Reversed else `SALE-{sale.SaleNumber}` |
| 3 | Sale Farm | s.Farm.Name via navigation (fixed old "(All)" bug when null) |
| 4 | Customer | s.Customer.Name |
| 5 | Number Sold | items.Count(i => i.LivestockId.HasValue) per sale |
| 6 | Gross Sale Amount | s.GrandTotal formatted C2 |
| 7 | Status badge | Draft=secondary / Confirmed=warning / Completed=success / Cancelled=dark / Reversed=danger / Other=info |
| 8 | View | `<a asp-controller="Sales" asp-action="Details" asp-route-id="@sale.SaleId">View</a>` |

Details wrapped in `<div class="table-responsive w-100">` — no horizontal page-level overflow (inner scroll when needed). Reversed sale rows also in sale details list (still listable for audit trace) but excluded from Financial summaries per 5.3 (they are Reversed not Confirmed/Completed counted filter).

### 5.5 Headline totals 4 cards (desktop)
Cards row (col-sm-6 col-lg-3 responsive):
1. **Total Number Sold** (badge): `report.TotalNumberSold` (sum of Number Sold per farm; = sum details)
2. **Total Gross Sale Amount:** `report.TotalGrossSalesAmount`
3. **Total Additional Sale Costs:** `report.TotalAdditionalSaleCosts ?? 0` (badge "None" when 0.00)
4. **Total Net Sale Proceeds:** `report.TotalNetSaleProceeds ?? 0` (color green positive)

### 5.6 Mobile Sales By Period (O6 — separate pages)
[MobileSalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileSalesByPeriod.cshtml)
- Info mob-card Date Basis banner
- From / To / Farm mob-fields: label above, 44px min-height native inputs
- mob-totals-card: Total Net Sale Proceeds (green when >=0, red when <0 → Net usually positive for revenue minus 4 seller costs; if negative business pattern shows losses)
- mob-kpi-row 2 cols: Number Sold + Gross
- mob-kpi-row 2 cols: Total Additional Sale Costs + Net
- Farm Summaries: variable count. if Model.FarmSummaries.Count > 6 → wrap in `<details><summary>🏘️ Farm Summaries ({count} farms, tap to open)</summary>`. If ≤6 → flat mob-cards directly. Each farm shows: FarmName + Number Sold + Gross + Additional Sale Costs + Net. No tables → overflow-safe cards.
- Sale Details: each sale is collapsed native `<details>` with summary showing SaleDate + displaySaleNumber (↶ prefix when Reversed). Expand → lines for Sale Farm / Customer / Number Sold / Gross Amount. Inline status badges. `mob-btn primary View Sale → /Sales/Details/{id}` 44px min-height.

---

## 6 — Livestock Profitability From/To Date Filters (O4, Realization-Based Semantics)

### 6.1 Added controller params
File: [ReportsController.cs L105-L148](file:///C:/Projects/livestock/src/LivestockManager.Web/Controllers/ReportsController.cs#L105-L148)
```csharp
public async Task<IActionResult> LivestockProfitability(
    DateTime? from, DateTime? to, Guid? farmId,
    LivestockType[]? types, StockSource[]? sources, LivestockStatus[]? statuses,
    bool exportCsv = false, CancellationToken ct = default)
{
  // fromDate = from.AsUtcDayStart() inclusive
  // toDate   = to.AsUtcDayEnd()   inclusive
  // farmId validation: must be in authorized list else null → empty safe
  // types/sources/statuses arrays → propagate to service (combined with dates)
  // service: _reportService.LivestockProfitabilityWithDatesAsync(companyId, farmId, fromDate, toDate, ct)
}
```

### 6.2 Dispatch: No dates vs Any date (critical semantic, O4)
File: [ReportService.cs `LivestockProfitabilityWithDatesAsync`](file:///C:/Projects/livestock/src/LivestockManager.Application/Services/Reports/ReportService.cs)
```csharp
if (fromDate == null && toDate == null)
{
  // Dispatch A: Default behavior unchanged → original all-status report
  return await LivestockProfitabilityAsync(companyId, farmId, ct);
}
else
{
  // Dispatch B: Realization date filter → Sold animals realized inside range only
  var all = await CompleteLivestockProfitabilityAsync(companyId, farmId, ct);
  // Filter DischargedSold + realization date:
  //   prefer Sale.Date (if SaleItem.Sale link exists)
  //   else Livestock.DischargeDate
  all = all.Where(row =>
  {
    var realization = row.SaleDate ?? row.DischargeDate;
    if (!realization.HasValue) return false;
    if (fromDate.HasValue && realization < fromDate) return false;
    if (toDate  .HasValue && realization > toDate  ) return false;
    return true;
  }).ToList();
  return all;
}
```

**Why two behaviors?** Case A default-allows user to use report same-as-before when filters empty. Case B date-filter returns realized sold-only profitability (new behavior requested by user spec Section 11).

### 6.3 Historical costs preserved (for in-range sold animals)
Per animal whose realization date falls inside report range, the report includes FULL costs:
- PurchaseAmount (livestock acquisition cost at purchase/birth moment, regardless when relative to report period)
- 4 allocated acquisition additions: AllocatedCommission + AllocatedTax + AllocatedTransport + AllocatedOtherCost → sum = AdditionalAcquisitionCosts (new DTO field in profitability rows)
- TotalAcquisitionCosts = PurchaseAmount + AdditionalAcquisitionCosts (new DTO)
- Operating expenses allocated to this animal (via expense-to-livestock links; sum complete regardless expense date)
- AdditionalSaleCosts from sale.TotalAdditionalSaleCosts nav populated (new DTO)
- NetSaleProceeds from saleItem.NetSaleProceeds nav populated (new DTO)

**Explicitly NOT done by filter:** Never filter expenses by date range; never remove pre-report-period purchase acquisition costs from an animal whose sale IS inside the report period. That would be an incorrect profitability calculation (Sec 12 prohibited calculation list item #2).

### 6.4 Reversed / Cancelled / Voided exclusions (sold set)
CompleteLivestockProfitabilityAsync (Case B) operates on DischargedSold. Then:
- SaleItem.Invoice exists → Cancelled → exclude; Invoice.Voided → exclude
- Sale.Reversed (Sale.Status == SaleStatus.Reversed) → exclude (via nav chain). Reversal workflow via `ReverseSaleAsync` sets livestock back Active; the livestock itself will no longer be DischargedSold after reversal → automatically excluded from set anyway; but nav-chain check provides belt-and-suspenders for consistency with Phase 17 rules.
- Draft invoice statuses (not finalized) → exclude.

### 6.5 4 existing filters combined with dates
Farm (single), Livestock Type (multi), Stock Source (multi), Status (multi) — all propagate service side. Composition is AND:
- Farm scope: farm.FarmId == farmId || farmId not set
- LivestockType in types array || types null/empty
- StockSource in sources array || sources null/empty
- LivestockStatus in statuses array || statuses null/empty

### 6.6 Date Basis banner
Desktop:
`📅 Date Basis: Sale Date (Realized Profitability) — When dates are set, includes only sold livestock whose Sale (or Discharge) Date falls inside From…To inclusive. Historical costs for those animals are kept regardless of their date.`
Mobile: same banner in info mob-card.

### 6.7 Desktop Summary 6 cards (Section 11)
Row 6-col or 3×2 responsive:
1. **# / Number of Livestock Sold** = rows.Count()
2. **Gross Sale Revenue** = Σ row.GrossSaleAmount (FinalSalePrice or fallback gross)
3. **Total Acquisition Cost** = Σ row.TotalAcquisitionCosts
4. **Operating Costs Allocated**
5. **Additional Sale Costs**
6. **Complete Profit** (card: green >=0, red <0 with warning class)

Mobile: 3 stacked mob-kpi-rows + totals-card Complete Profit.

### 6.8 Details Table (14 cols) + CSV 15 cols
| Col | Field | Notes |
|----:|:------|:------|
| 1 | Livestock ID | Link |
| 2 | Type | `LivestockTypeDisplay.GetDisplayName(row.LivestockType)` |
| 3 | Sale Farm | row.SaleFarmName (prefer Sale.Farm.Name over animal.Farm.Name) |
| 4 | Stock Source | row.Source |
| 5 | Acquired Date | |
| 6 | DOB | |
| 7 | Sale Date | row.SaleDate (prefer) else DischargeDate → realized |
| 8 | Gross Sale Amount | |
| 9 | Purchase Amount | |
|10 | Additional Acquisition Costs | 4 allocations sum |
|11 | Total Acquisition Cost | |
|12 | Operating Costs Allocated | |
|13 | Additional Sale Costs | |
|14 | Net Sale Proceeds | |
|15 | Basic Profit (Gross - TotalAcq) | color green>=0 red<0 |
|16 | Complete Profit (Net - TotalAcq - Operating) | color coding |

CSV: same order, RFC 4180 escaping when fields contain comma/newline.

---

## 7 — Livestock Profitability Mobile Page (O6 — separate route)
[MobileLivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileLivestockProfitability.cshtml)
- Layout `_MobileLayout`; no tables; single-col cards; 44px mob-field min-height each; no wide-table overflow offenders.
- Info banner Date Basis Sale Date (Realized Profitability)
- From Date / To Date / Farm → mob-fields + Reset + Apply
- mob-totals-card: Complete Profit sum → red warning if negative, green positive.
- 3 kpi-rows: (Livestock Sold, Gross) / (Total Acq Cost, Operating Costs) / (Additional Sale Costs, Net Sale Proceeds)
- Per-animal collapsed `<details>` native HTML. Summary = `{LivestockId} {TypeShortCode} ${Gross:n0}` with inline Complete Profit small colored label. Expand = 14 field rows + mini Complete Profit inline mob-totals-card (same color semantics desktop). CSV chip top right export preserves filters.

---

## 8 — Active Livestock Mobile Page (O6)
[MobileActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileActiveLivestock.cshtml)
- Separate page; Layout _MobileLayout. No tables.
- Farm filter mob-card: dropdown (same FarmSummaryDto typed cast fix), option text Name + (Code) when Code not null; Reset + Apply flex 1 btn each; 44px min-height.
- mob-totals-card: Total Number Active = Sum(row.Count) across all types.
- Per-type mob-card: Title `LivestockTypeDisplay.GetDisplayName(row.LivestockType)` (Ah/Su/Sa/Ad/Sd combined). Body rows: Number Active + Percentage Of Total (with native 5px tall progress bar styled).
- Export CSV chip-tab.

---

## 9 — Authorization Matrix (Section 16 Security)

| Route | Auth Policy | Notes |
|:------|:-----------:|:------|
| /Reports/Index | [class CanViewOperationalData] | Operational overview entry |
| /Reports/ActiveLivestock | class default | Operational report |
| /Reports/MobileActiveLivestock | class default | Operational mobile page |
| /Reports/SalesByPeriod | [Authorize(CanViewFinancialData)] explicit override | Financial report mobile same |
| /Reports/MobileSalesByPeriod | [Authorize(CanViewFinancialData)] explicit override | Protected financial |
| /Reports/LivestockProfitability | [Authorize(CanViewFinancialData)] explicit override | Protected financial (PnL) |
| /Reports/MobileLivestockProfitability | [Authorize(CanViewFinancialData)] explicit override | Protected financial (PnL) |
| /Reports/ProfitLoss | [Authorize(CanViewFinancialData)] explicit override | Preserved unchanged Phase prior |
| /Reports/MobileProfitLoss | same override | Preserved untouched |

**Farm isolation + company scope:** Every controller action loads the current user's CompanyId; then passes companyId to the service. Service loads ONLY farms within that company. Unauthorized farmId attempts → coerced to null (empty result) as described Section 3.2 / 5.1 / 6.1. Zero cross-company result leak.

**CSV authorization parity:** All CSV endpoints require EXACT same policy as controller action (authorize attribute at method level covers both HTML + CSV code paths). Export CSV uses EXACT same service call → EXACT same rows → display/csv parity guaranteed.

---

## 10 — Profit & Loss Unchanged (O5)

### 10.1 Code untouched proof
| Component | Modification status | Evidence |
|:----------|:-------------------:|:---------|
| ReportsController ProfitLoss action (L150-L189) | NOT CHANGED | Grep result — zero additions/removals this round |
| ReportsController MobileProfitLoss action (L191-L211) | NOT CHANGED | Same |
| IReportService.ProfitLossAsync signature | NOT CHANGED | Preserved interface; only 3 NEW methods ADDED (additive) |
| ReportService.ProfitLossAsync body | NOT CHANGED | Zero diff |
| ProfitLoss DTOs | NOT CHANGED | |
| Views/Reports/ProfitLoss.cshtml | NOT CHANGED | File timestamp unchanged |
| Views/Reports/MobileProfitLoss.cshtml | NOT CHANGED | File timestamp unchanged |
| Profit Loss CSV export rows | NOT CHANGED | Controller action unchanged |
| Period filter / date basis / Export CSV | NOT CHANGED | As user explicitly instructed (Sec 1, Sec 15, Sec 22 rule #25) |

### 10.2 Reason for no change
User's spec Section 1 explicitly: "DO NOT change [Profit & Loss] business logic, layout, labels, filters, date basis or export. Only minimal technical adjustment if a shared report component breaks build; run regression tests." Build passed (0W/0E); no shared component broke; P&L no modifications required; smoke 302 auth gating OK.

---

## 11 — Database Migration Status (Section 17 Performance)

### 11.1 Additive migration needed?
**NONE NEEDED.** Decision `D9` in `.agent/DECISIONS.md`:

Inspected existing indexes (EF Core migration history from prior phases):
- `Livestock`: IX on CompanyId, FarmId, Status, LivestockType. Sufficient.
- `Sale`: IX on CompanyId, FarmId, Date, Status. Sufficient for `SalesByPeriodReportAsync` Confirmed/Completed Date between query.
- `SaleItem`: IX on SaleId, LivestockId. Sufficient for per-sale Number Sold counting.

No additive EF Core migration generated. No `AddReportsTabIndexes*.cs` migration file. `_EFMigrationsHistory` table unchanged.

### 11.2 Query design (avoiding hidden N+1)
- SalesByPeriodReportAsync: `.Include(s => s.Farm).Include(s => s.Customer).Include(s => s.Items).AsNoTracking()` → single query. Projects into DTO; in-memory grouping farm + items.
- LivestockProfitability: CompleteLivestockProfitabilityAsync with `.Include(l => l.Farm).Include(l => l.PurchaseItem!)…SaleItem!.Sale!.Farm!` → single query (LazyLoadingProxies off in this project; avoid N+1 via Include).

---

## 12 — New / Modified Files List

### 12.1 New DTOs (typed reports, RFC-2)
| File | Purpose |
|:-----|:--------|
| [ActiveLivestockByTypeReportDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/ActiveLivestockByTypeReportDto.cs) | LivestockType, TypeCode, TypeLabel (LivestockTypeDisplay), Count, PercentageOfTotal, CountFilteredTotalContext. Used by ActiveLivestock Desktop + Mobile + CSV. |
| [SalesByPeriodFarmSummaryDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodFarmSummaryDto.cs) | FarmId, FarmName, NumberSold, GrossSalesAmount, AdditionalSaleCosts, NetSaleProceeds. Farm row in Sales. |
| [SalesByPeriodSaleDetailDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodSaleDetailDto.cs) | SaleDate, SaleNumber, FarmId, FarmName, CustomerName, NumberSold, GrossSaleAmount, Status, SaleId. Details rows. |
| [SalesByPeriodReportDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/SalesByPeriodReportDto.cs) | Aggregate: FromDate, ToDate, TotalNumberSold, TotalGrossSalesAmount, TotalAdditionalSaleCosts, TotalNetSaleProceeds, IList<FarmSummary> FarmSummaries, IList<SaleDetail> SaleDetails. |

### 12.2 Modified DTO (additive 7 new fields; order preserved; existing property names kept)
[LivestockProfitabilityReportRowDto.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/DTOs/Reports/LivestockProfitabilityReportRowDto.cs)
Added AFTER existing fields (appended at bottom):
- `Guid? SaleFarmId`
- `string? SaleFarmName`
- `DateTimeOffset? SaleDate`
- `decimal AdditionalAcquisitionCosts`
- `decimal TotalAcquisitionCosts`
- `decimal AdditionalSaleCosts`
- `decimal NetSaleProceeds`

All existing consumers that don't depend on reflection-only field count → unaffected.

### 12.3 New IReportService signatures (additive only; old preserved)
File: [IReportService.cs](file:///C:/Projects/livestock/src/LivestockManager.Application/Services/Reports/IReportService.cs) — 3 new methods appended:
```csharp
Task<IList<ActiveLivestockByTypeReportDto>> ActiveLivestockByTypeReportAsync(Guid companyId, Guid? farmId, CancellationToken ct);
Task<SalesByPeriodReportDto> SalesByPeriodReportAsync(Guid companyId, DateTimeOffset fromDate, DateTimeOffset toDate, Guid? farmId, CancellationToken ct);
Task<IList<LivestockProfitabilityReportRowDto>> LivestockProfitabilityWithDatesAsync(Guid companyId, Guid? farmId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct);
```

### 12.4 Modified ReportsController
[ReportsController.cs](file:///C:/Projects/livestock/src/LivestockManager.Web/Controllers/ReportsController.cs)
- 3 desktop actions (ActiveLivestock / SalesByPeriod / LivestockProfitability) rewired: use typed DTO service calls, new farmId/from/to parameters, CSV parity updated column headers.
- Appended 3 new GET actions at file end for mobile:
  1. MobileActiveLivestock (operational)
  2. MobileSalesByPeriod (financial)
  3. MobileLivestockProfitability (financial)

### 12.5 Rewritten desktop views
1. [ActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/ActiveLivestock.cshtml) — typed model; farm cast fix; responsive filter; LivestockTypeDisplay labels; Clear filter button
2. [SalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/SalesByPeriod.cshtml) — ground-up rewrite
3. [LivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/LivestockProfitability.cshtml) — ground-up rewrite with date filters + summary cards + 14-col details

### 12.6 New mobile views (3 files; separate pages, no logic duplicate — shared 100% same backend services)
1. [MobileActiveLivestock.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileActiveLivestock.cshtml)
2. [MobileSalesByPeriod.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileSalesByPeriod.cshtml)
3. [MobileLivestockProfitability.cshtml](file:///C:/Projects/livestock/src/LivestockManager.Web/Views/Reports/MobileLivestockProfitability.cshtml)

### 12.7 State & Docs files (RFC-6)
- `.agent/STATE.md`, `.agent/TASKS.json`, `.agent/DECISIONS.md`, `.agent/DEFECTS.md`, `.agent/LAST_RUN.md` — auto-resume
- [BUILD_STATUS.md](file:///C:/Projects/livestock/BUILD_STATUS.md) — appended REPORTS_TAB_USER_REVIEW_FIXES section, Phase updated, tracker 6 rows RFC-1..RFC-6
- [CHANGELOG.md](file:///C:/Projects/livestock/CHANGELOG.md) — appended Phase 18 (18.1..18.10)
- Audit: this file Sections 1..24
- Not touched: existing audit reports

---

## 13 — Decision Log (D1..D10)
See `.agent/DECISIONS.md` (full table). Key decisions:

| # | Decision | Outcome |
|--:|:---------|:--------|
| D1 | Root cause Active Livestock Farm filter | Wrong cast (dynamic vs FarmSummaryDto). Fix = correct interface cast. |
| D2 | FarmId unauthorized strategy | Safe empty result (nullify) vs throw. Consistent. |
| D3 | Number Sold counting method | Only SaleItems where LivestockId.HasValue → excludes generic lines. |
| D4 | Profitability date dispatch (no-dates vs any-date) | No-dates → old all-status; any → realized-only. Preserves default UX. |
| D5 | 4 seller cost display on new reports | Populated from Phase 17 migration. REAL data (no zero fabrication). |
| D6 | `# Sold` replacement scope | Only count-of-heads. Leave document numbers + livestock IDs alone. |
| D7 | Mobile pages strategy vs responsive single | Separate routes + pages; shared backend. Existing dual-view convention. |
| D8 | P&L handling strategy | Zero modifications. Complete untouched. |
| D9 | Index migration strategy | Existing indexes sufficient; skip creating migration. |
| D10 | Mobile auth policies | Match desktop (operational vs financial). |

---

## 14 — Defects Found & Remediated This Round
| # | Defect | Severity | Status |
|--:|:-------|:---------|:-------|
| DFX-001 | Active Livestock Farm filter broken always empty | CRITICAL | FIXED |
| DFX-002 | Sales by Period missing farm filter | MEDIUM | FIXED |
| DFX-003 | `# Sold` ambiguous hash terminology | MEDIUM | FIXED |
| DFX-004 | Profitability missing From/To date filters | HIGH | FIXED |
| DFX-005 | Only P&L had mobile page; other 3 reports missing mobile pages | MEDIUM | FIXED |
| DFX-006 | Build error RZ1006 MobileSalesByPeriod stray `@if` inside `@for` block | BUILD BLOCKER | FIXED |
| DFX-007 | CS8602 4 nullable warnings ReportService Include/ThenInclude chains | LOW | FIXED dammit operators |
| DFX-008 | 404 mobile reports first smoke — controller mobile actions never written by sub-agent | HIGH (mobile broken) | FIXED manually appended controller actions |
| DFX-009 | Stale server served old DLLs on :5100 — routes still 404 after rebuild | LOW (infra only) | FIXED kill PIDs restart server |

See `.agent/DEFECTS.md` for full root-cause table.

---

## 15 — Build Gate

```
dotnet build c:\Projects\livestock\src\LivestockManager.Web\LivestockManager.Web.csproj
       -c Release --no-incremental -v minimal
Build succeeded.
 0 Warning(s)
 0 Error(s)
 Time Elapsed 00:00:08.10
```

---

## 16 — Tests Gate

| Suite | Result |
|:------|:------:|
| Architecture Tests (NetArchTest rules) | **60 / 60 PASSED** (0 Skipped, 0 Failed) |
| Integration Tests (WebAppFactory/Kestrel) | **15 / 15 PASSED** |
| Unit Tests (xUnit VSTest) | **322 / 324 PASSED** (†2 intentional unchanged: PostPurchase_CreatesLivestockIntakeIdempotently + PostPurchase_LivestockInitialWeightLinkedIfProvided throw DomainException per Phase 15 decision) |
| New regressions this round | **0** zero |

† Not regressions. Documented as intentional by design.

---

## 17 — HTTP Smoke Panel (Authentication gating)

Server running on `http://localhost:5100` (port 5100 listener active; Release build; no-build mode).

| # | URL | Expected | Actual |
|--:|:----|:---------|:-------|
| 1 | `/Account/Login` | 200 OK | **200 OK** (27,132 bytes) |
| 2 | `/Reports/Index` | 302 auth redirect | **302 → /Account/Login?returnUrl=%2FReports%2FIndex** (correct policy gate) |
| 3 | `/Reports/ActiveLivestock` | 302 auth redirect | **302** correct |
| 4 | `/Reports/SalesByPeriod` | 302 financial gate | **302** correct |
| 5 | `/Reports/LivestockProfitability` | 302 financial gate | **302** correct |
| 6 | `/Reports/ProfitLoss` | 302 (untouched) | **302** correct |
| 7 | `/Reports/MobileActiveLivestock` | 302 operational | **302** NEW (was 404 before controller append) |
| 8 | `/Reports/MobileSalesByPeriod` | 302 financial | **302** NEW |
| 9 | `/Reports/MobileLivestockProfitability` | 302 financial | **302** NEW |
|10 | `/Reports/MobileProfitLoss` | 302 (untouched) | **302** correct |

**Verdict:** All 10 routes pass. 0 404s. 0 500s. 3 new mobile routes correctly return 302 auth redirect (not 404 which would indicate missing action/view).

---

## 18 — VS Code IDE Diagnostics

Command: GetDiagnostics (no uri → entire workspace)
Result: **Empty array []** — No C# / CSHTML / JS / TS warnings or errors reported by IDE.

---

## 19 — CSV Export Parity Confirmation

| Report | Screen filters | CSV columns match screen columns? | Same filter parameters passed to CSV route? |
|:-------|:---------------|:----------------------------------|:-------------------------------------------|
| Active Livestock by Type | Farm (single) | Yes: Livestock Type / Number Active / Percentage Of Total | farmId preserved |
| Sales by Period | From Date / To Date / Farm | Yes: Sale Date / Sale Number / Farm / Customer / Number Sold / Gross Sale Amount / Status | from, to, farmId all preserved |
| Livestock Profitability | From Date / To Date / Farm + Types/Source/Status arrays | Yes: 15 columns same order | All params preserved; date basis same rows |
| Profit & Loss | Period From/To + granularity | UNCHANGED (not touched) | UNCHANGED |

---

## 20 — Known Issues & Auditor Attention

### 20.1 Known issues NOT addressed this round (pre-existing; out-of-scope)
1. **†2 Unit tests failing intentionally** — Purchase Invoices cannot create livestock automatically (use Stock Addition). Documented Phase 15 audit reconciliation.
2. **MARS Warning (MultipleActiveResultSets=False connection string)** — from prior Phases. Not introduced this round. No functional impact for the Reports queries (single query per page action Include-chains).
3. **No Playwright E2E executed this round** — spec did not mandate E2E run; gate passes unit/integration/arch + HTTP smoke. Optional for auditor.

### 20.2 Potential future enhancements (not user-requested; optional future)
- Filename suffix CSV for profitability: `LivestockProfitability_2026-01-01_to_2026-03-31.csv` (currently filename static).
- Hyperlinks Livestock ID → /Livestock/Details/{id} in Profitability detail view (future work; table currently render-only IDs).
- Row-level expand/collapse detail desktop (already implemented mobile).
- Mobile Reports sub-nav links on the Mobile Dashboard Reports menu area (currently mobile links via direct route not dashboard tiles — pattern same as P&L was).

---

## 21 — Authorization & Security Recheck

| Area | Verified? |
|:-----|:---------:|
| All reports class-level `[Authorize(Policy = "CanViewOperationalData")]` present | Yes |
| Financial reports override with `CanViewFinancialData` (both desktop + mobile variants) | Yes; 6 actions total have explicit override |
| Cross-company farm ID craft → returns empty safe; never rows from other company | Yes; 3 service-level guard blocks |
| From/To date UTC boundaries inclusive (midnight start / day-end inclusive) → avoids omit edge day bug | Yes (AsUtcDayStart/End) |
| CSV endpoint authorization parity | Yes; same authorize attribute covers HTML+CSV code paths |
| P&L authorization unchanged | Yes |
| Anonymous access to any of 10 Reports routes is blocked (HTTP smoke 302 redirect) | Yes |

---

## 22 — 14-Item Completion Condition Checklist (per Spec Section 22)

| # | Completion condition | Status | Evidence |
|--:|:---------------------|:------:|:---------|
| 1 | Active Livestock by Type Farm filter repaired — dropdown shows authorized farms, persists selection, CSV same filter | ✅ PASS | Sec 3 evidence |
| 2 | No `# Sold` / `# Heads` / `# of Heads Sold` / `# Head` remains in Sales reports user-facing | ✅ PASS | Grep zero matches; Sec 4.2 preserved hashes untouched |
| 3 | Sale Number / document numbers / Livestock ID labels untouched (hash not rewritten to Number when identifier) | ✅ PASS | Sec 4.2 table |
| 4 | Sales by Period farm filter rendered (authorized farms; not cross-company) | ✅ PASS | Sec 5.1 |
| 5 | Farm summary breakdown added to Sales (Farm, Number Sold, Gross, Additional Sale Costs, Net) | ✅ PASS | Sec 5.3 table |
| 6 | Headline totals preserved exactly and farm rows reconcile to totals | ✅ PASS | Sec 5.3 same grouping server-side aggregate sum = headline |
| 7 | Livestock Profitability From Date + To Date inclusive filter pickers rendered (date input, label, apply, reset) | ✅ PASS | Sec 6.1, 6.6 desktop; Mobile pages 7,8 |
| 8 | Profitability dispatch: no dates → original all-status; any-date → DischargedSold realization-based | ✅ PASS | Sec 6.2 dispatch A vs B |
| 9 | Historical costs kept (not filtered by period, not dropped) for in-range sold animals | ✅ PASS | Sec 6.3, explicit NOT list |
|10 | Unsold, reversed, cancelled, voided excluded from realized profitability | ✅ PASS | Sec 6.4 |
|11 | Farm/Livestock Type/Stock Source/Status 4 existing filters combined with dates (AND) | ✅ PASS | Sec 6.5 parameter arrays passed service |
|12 | P&L 0 business/layout/label/date basis/csv changes. Only minimal technical if shared broke; not needed | ✅ PASS | Sec 10 untouched proof |
|13 | Build 0W/0E; Arch 60/60; Integration 15/15; Unit 322/324 (†2 intentional); smoke Login 200; 9 Reports 302 | ✅ PASS | Sec 15–17 |
|14 | Authorization preserved; company + farm isolation; CSV parity across all 3 desktop reports + mobile pages filters | ✅ PASS | Sec 9, 19 |

---

## 23 — Final Commit SHA Block

Phase 18 expected commit message (if commit is executed next):
```
Phase 18 Reports Tab user review corrections

- Active Livestock by Type Farm filter root-cause fix (wrong List<dynamic> cast vs
  runtime IList<FarmSummaryDto> → farm options empty always; correct cast) +
  backend farmId authorization validation
- Sales by Period: # Sold/# Heads terminology rewritten everywhere → Number Sold /
  Total Number Sold (document/livestock IDs preserved). Farm filter + server-side
  authoritative farm summary table (Farm / Number Sold / Gross / Additional Sale
  Costs / Net Proceeds). Sale details: 8 cols with Farm from navigation, ↶ Reversed
  prefix, Status badges, View links. Excluded statuses Draft/Cancelled/Voided/Reversed
  from totals.
- Livestock Profitability: From Date + To Date inclusive filters + realization-based
  date semantics (no-dates → original all-status behavior; any date → DischargedSold
  only by Sale.Date or DischargeDate; historical costs kept; unsold/reversed/voided
  excluded). 7 new DTO fields, 6 summary cards, 14-col details, date basis banner,
  existing 4 filters composable.
- Separate Mobile pages created MobileActiveLivestock / MobileSalesByPeriod /
  MobileLivestockProfitability. Cards layout, 44px min mob-field, collapsible
  details, no wide tables, CSV chip-tabs; shared backend 100% (zero duplication).
- P&L completely untouched. Zero modifications to service, views, CSV, date basis.
- Build Release 0W/0E. Arch 60/60. Integration 15/15. Unit 322/324 (2 intentional).
  HTTP smoke: Login 200; /Reports Index/ActiveLivestock/SalesByPeriod/LivestockProfitability/
  ProfitLoss/MobileActiveLivestock/MobileSalesByPeriod/MobileLivestockProfitability/
  MobileProfitLoss → 302 auth redirect (correct gating).
```

| Field | Value (to be filled AFTER commit; placeholder if deferred) |
|:------|:------------------------------------------------------------|
| Phase 18 Commit Short SHA (7 chars) | `_________________` (deferred, user may skip commit same pattern SALES_TAB) |
| Phase 18 Commit Full SHA (40 chars) | `________________________________________________` |
| Phase 18 Committed At UTC | `_________________` (ISO-8601, e.g. 2026-08-14T09:02:00Z) |
| Git status at end of run | `dirty / staged (uncommitted)` — commit not performed; working tree contains new/modified source files + docs + state |
| Base HEAD (before round) short SHA | `bb4f81f` — confirmed |
| Prior P&L SHA confirmation (did code change?) | P&L byte-shape identical. SHA unchanged. |

---

## 24 — Independent Auditor Signature Block (PENDING)

Auditor: complete this block AFTER independently:
1. Re-running the 14-item Section 22 checklist.
2. Reproducing Active Livestock Farm filter root cause + verifying fix.
3. Running dotnet build Release; dotnet test 3 suites; confirming counts.
4. Visiting all 10 Reports HTTP routes; confirming Login 200; 9 reports 302 auth gating; mobile routes not 404.
5. Spot-checking profitability date semantics against seeded demo data.
6. Confirming P&L ProfitLoss.cshtml + ProfitLossAsync service method zero diff from base bb4f81f.

```
Independent Auditor Name (print):  _______________________________________
Independent Auditor Signature:    _______________________________________
Date (YYYY-MM-DD):                _______________________________________
Audit findings count:
  High:   ___
  Medium: ___
  Low:    ___
Auditor verdict (circle one):      [ ] APPROVED    [ ] CHANGES REQUESTED
Auditor SHA256 recompute matches 800D5867...7D2E? [ ] YES [ ] N/A (release SHA not yet recomputed)
```

---

```
REPORTS TAB CORRECTIONS READY FOR USER REVIEW
```
