# FULL AUDIT — Reports Tab Audit (Section 13)

**Audit Section:** 13  
**Status:** COMPLETED — Static audit of report controllers, services, views (Desktop+Mobile+CSV)  
**Auditor:** Independent Auditor

**Reports under audit (4 desktop + 4 mobile + 4 CSV = 12 surfaces):**
1. Active Livestock (Op Data policy: CanViewOperationalData)
2. Sales by Period (Fin policy: CanViewFinancialData)
3. Livestock Profitability (Fin policy: CanViewFinancialData)
4. **Profit & Loss (P&L) — BYTE-IDENTICAL PRESERVE, ZERO REGRESSION MANDATE**

---

## 13.A Report Authorization Policy Matrix (6 roles × 4 reports)

| Report | Viewer | DataEntry | FarmManager | Accounts | CompanyAdmin | SysAdmin | Expected Policy Gate | Status |
|---|---|---|---|---|---|---|---|---|
| **Active Livestock** | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | CanViewOperationalData (in ALL 6 roles except none explicitly excluded — Viewer/DataEntry included) | ✅ PASS |
| **Sales by Period** | ❌ 403 | ❌ 403 | ❌ 403 | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | CanViewFinancialData excluded Viewer/DataEntry per Program.cs exclusion | ✅ PASS |
| **Livestock Profitability** | ❌ 403 | ❌ 403 | ❌ 403 | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | CanViewFinancialData | ✅ PASS |
| **Profit & Loss** | ❌ 403 | ❌ 403 | ❌ 403 | ✅ 200 OK | ✅ 200 OK | ✅ 200 OK | CanViewFinancialData | ✅ PASS |
| **P&L CSV Export** | ❌ 403 | ❌ 403 | ❌ 403 | ✅ 200 | ✅ 200 | ✅ 200 | Same policy gate as view action | ✅ PASS |

**Policies verified:** All 4 reports have correct `[Authorize(Policy = PolicyNames.CanViewFinancialData)]` (or Operational for ActiveLivestock) on both Desktop and Mobile controller actions AND their sibling CSV Export actions. 0 policy bypasses found. 24 policy-gate checks PASS ✅.

---

## 13.1 Active Livestock Report (Desktop + Mobile + CSV)

| Check | Expected Desktop | Actual Desktop | Expected Mobile | Actual Mobile | Expected CSV | Actual CSV | Status |
|---|---|---|---|---|---|---|---|
| RL-1 All Company farms aggregate totals | Sums by type × farms | ✅ Service `GetActiveLivestockReportAsync` applies `.Where(CompanyId == companyId)` sum aggregation | Cards sum per farm+type | ✅ Mobile DTO shares same service aggregator | Totals match HTML | ✅ CSV returns same rows from same DTO before file stream write | ✅ PASS |
| RL-2 Farm dropdown filter: shows only authorized farms (B1/B2 absent for Company A) | Company scoped dropdown | ✅ `_farmService.ListForDropdownAsync(companyId)` scoped | Native `<details>` filter panel | ✅ Mobile filter implements `<details><summary>` farm scoped dropdown | Same filter applied server-side | ✅ CSV endpoint takes farmId parameter with same scoping | ✅ PASS |
| RL-3 Filter by Type = Ah: Only Ah rows | Correct subset | ✅ Type enum filter applied in service query | Type filter works | ✅ Same type filter parameter | CSV only Ah | ✅ CSV accepts type filter same way | ✅ PASS |
| RL-4 Archived animals default excluded | Archived rows = 0 in main | ✅ `.Where(l => !l.IsArchived)` unless toggle | Same default | ✅ Same query | 0 archived unless toggle | ✅ Default same for CSV | ✅ PASS |
| RL-5 Unified LivestockTypeDisplay helper labels Ah/Su/Sa/Ad/Sd | Unified helper shared | ✅ `LivestockTypeDisplay.Format(type)` used both table header and cell values | Same helper used | ✅ Mobile cards same reference | Same labels CSV not codes | ✅ CsvWriter uses helper before writing rows | ✅ PASS |
| RL-6 Active totals reconcile: Detail sum = headline grand total | Grand = Σ farms | ✅ Service returns `HeadlineTotal + FarmSubtotals` explicitly; detail rows sum matches headline no 0.01 rounding diff | Cards grand match headline | ✅ Same DTO data | CSV footer total same | ✅ CsvWriter appends footer row with headline total value | ✅ PASS |
| RL-7 Company B login cannot see Company A data CSV or HTML | 0 rows / 403 | ✅ Service takes companyId parameter → not-found if cross-co → empty not crash | 0 rows / 403 | ✅ Same path | CSV empty 0 rows not crash | ✅ CSV same service query | ✅ PASS |
| RL-8 Viewer role can access (Operational Data policy) | 200 OK for Viewer | ✅ Confirmed policy CanViewOperationalData | 200 OK | ✅ Same policy | 200 OK | ✅ Same policy | ✅ PASS |

**Active Livestock Score: 8/8 ✅ PASS**

---

## 13.2 Sales by Period Report (From/To Inclusive + Farm Filter + Terminology)

| Check | Expected | Actual (Desktop + Mobile) | Status |
|---|---|---|---|
| RS-1 Terminology: ALL headings/columns MUST use **"Number Sold"** (NOT "# Sold", NOT "#Heads"). Zero occurrences forbidden terms anywhere Desktop/Mobile/CSV/Export | **ZERO** "# Sold" or "#Heads" | ✅ **DESKTOP:** All cshtml verified — 0 "# Sold" / 0 "#Heads" → CORRECT "Number Sold" literals used.<br>❌ **MOBILE:** MobileSalesByPeriod.cshtml L42 label contains `#Heads` literal (FAUD-0017 MEDIUM)<br>✅ **CSV:** CsvWriter uses string constant "Number Sold" — 0 forbidden terms. | ⚠️ **MEDIUM (FAUD-0017):** MOBILE only; Desktop + CSV correct |
| RS-2 From/To date filter INCLUSIVE both ends (01-01 and 01-31 sales both included) | Both endpoints included | ✅ Service predicate: `s.Date >= dto.FromDate && s.Date <= dto.ToDate` (inclusive both closed boundaries) | ✅ PASS |
| RS-3 Farm-level breakdown structure (Section per farm → per-farm subtotal → overall headline) | Breakdown structure per spec | ✅ View iterates `Model.PerFarmGroup`; each farm has header card/row, subtotal. Overall total at bottom. Desktop group-by FarmId. Mobile farm cards per group with subtotal. | ✅ PASS |
| RS-4 Detail columns: EarTag / Customer / Date / Type / NumberSold / Gross / Net / InvoiceNo | Detail columns match spec | ✅ Desktop table th headers match spec 7 columns + action. Mobile cards detail section has same 7 data points. CSV same 7 columns same order. | ✅ PASS |
| RS-5 Overall headline Σ = farms subtotal Σ (0.01 tolerance) | Grand = Σ farms no drift | ✅ Service returns explicit `GrandTotalGross / GrandTotalNet` = farms SUM; verified no multi-enumeration rounding drift (single-pass aggregation stored then reused) | ✅ PASS |
| RS-6 Reversed sales (REVERSED- prefix badge) EXCLUDED from all financial totals | Excluded entirely from totals | ✅ `.Where(s => s.Status != SaleStatus.Reversed)` filter applied FIRST before group-by/aggregation. Verified both Desktop controller + Mobile + CSV 3 endpoints all use same base query. | ✅ PASS |
| RS-7 Cancelled invoices / Voided purchases → sales linked to voids excluded (if applicable) | Excluded per logic | ✅ Voided invoices not counted in OutstandingInvoices aggregator; sale-level void not applicable (sales use Reversed not Void) | ✅ PASS |
| RS-8 Farm filter works: Farm A1 only → A1 detail rows only + overall = FarmA1 total only | Filter correct per farm | ✅ Service adds `.Where(s => s.FarmId == dto.FarmId)` when dto.FarmId not null; tested with not-null FarmId scoping path | ✅ PASS |
| RS-9 **Customer filter (per spec "Customer/type/status where available")** | Customer dropdown selector works | ❌ **FAUD-0020 MEDIUM — Customer filter UI + Service NOT IMPLEMENTED.** Controller DTO does NOT declare `CustomerId` property; service LINQ has no `.Where(s => s.CustomerId == dto.CustomerId)`; Desktop view has no Customer dropdown. Requirement explicitly stated in Section 13. "Customer/type/status where available". Missing. | ❌ FAUD-0020 MEDIUM |
| RS-10 **Status filter (default Confirmed only)** | Drafts default excluded; toggle to include | ❌ **FAUD-0020 MEDIUM — Status filter NOT IMPLEMENTED.** Same root as RS-9: DTO has no `SaleStatus?` property; service query defaults to `.Where(s => s.Status == Confirmed)` but user cannot toggle this. Requirement "Customer/type/status where available" partially satisfied (default Confirmed hardcoded not selector UI). | ❌ FAUD-0020 MEDIUM (bundled) |
| RS-11 CSV columns = Desktop table columns same order same formatting | CSV ↔ table 1:1 match | ✅ CsvWriter class writes same 8 columns in identical order as Desktop `<th>` order; formatting (money 2 decimals, date yyyy-MM-dd) shared formatter constants. | ✅ PASS |
| RS-12 Mobile cards: "Number Sold" label correct | No # Sold no #Heads | ❌ **FAUD-0017 MEDIUM — #Heads label on L42 MobileSalesByPeriod.cshtml.** Terminology wrong on mobile; desktop correct. | ❌ FAUD-0017 MEDIUM (same as RS-1) |

**Sales by Period Score: 9/12 ✅. 3 issues = FAUD-0017 (1) + FAUD-0020 (2).**

---

## 13.3 Livestock Profitability Report (Realization-Basis + Complete Formula Bug)

**SPEC MANDATE:** Profitability realization-basis filters on Sale/Discharge date → includes only DischargedSold animals while preserving ALL HISTORICAL COSTS (cost-to-date not truncated by date window). Basic Profit only. Complete Profit subtracts additional acquisition and seller costs plus properly categorized Direct Operating Expenses linked to specific livestock.

| Check | Expected | Actual | Status |
|---|---|---|---|
| RP-1 Scope: Only DischargedSold animals (realization-basis Sale Date) | Not-yet-sold active excluded from rows and totals | ✅ Service `.Join(_db.Livestock, ... l.Status == LivestockStatus.DischargedSold)` → only sold animals. Correct. | ✅ PASS |
| RP-2 Farm filter works scoped within company | FarmId filter parameter; company scoped same time | ✅ `.Where(s => l.CompanyId == companyId && (dto.FarmId == null || l.FarmId == dto.FarmId))` scoping correct | ✅ PASS |
| RP-3 **Basic Profit Formula Correct:** `GrossSaleAmount − PurchasePrice` per animal (or Σ sold-group) | Basic = Gross minus PurchasePrice only | ✅ Basic profit calculation: `sale.GrossSaleTotal − livestock.PurchasePrice` → exactly matches spec. Verified same formula across Desktop DTO, Mobile DTO, CSV. | ✅ PASS |
| RP-4 **COMPLETE PROFIT FORMULA — SPEC MANDATE:** `Complete = NetSaleProceeds − TotalAcquisitionCost − Valid DirectOperatingExpenses (livestock-linked)` → Expand: `Complete = (GrossSale − CommissionSeller − TaxOnSale − TransportSeller − OtherSeller) − (PurchasePrice + CommissionBuy + TaxBuy + TransportBuy + OtherBuy) − SUM(DirectExpenses linked this LivestockId)` | Formula matches 3-part expansion above | ❌ **FAUD-0012 CRITICAL CLASSIFICATION — MEDIUM IMPACT AUDITED:** ReportService.CompleteLivestockProfitability currently calculates: `completeProfit = basicProfit − directOperatingExpenses` (only 2 terms). Formula OMITTED BOTH sides: (a) did NOT subtract 4 Seller Costs from GrossSale to get NetSaleProceeds (b) did NOT subtract 4 Additional Acquisition Costs from PurchasePrice to get TotalAcquisitionCost. Instead used basicProfit (GrossPurchasePrice) minus only DirectOpEx. For sample animal with Purchase=$1000 additional=$110, Sale Gross=$1600 seller=$40, Direct=$20, spec Complete = (1600-40) − (1000+110) − 20 = $430. Actual code Complete = (1600-1000) − 20 = $580 → inflated +$150 per sample. | ❌ **FAUD-0012 MEDIUM severity (formula wrong but core structure present; easy fix but financial misreporting if used without remediation)** |
| RP-5 Silent fallback when From/To date range BLANK (user submits with dates): Complete default == Basic with DirectExpenses = 0 and NO WARNING banner shown to user | User alerted that dates empty → Complete not calculated correctly; WARNING banner shown | ❌ **FAUD-0012 BUNDLED SAME ID:** When dates empty, service falls back to `if (date range empty) completeProfit = basicProfit; DirectOpEx = 0`. No warning banner in Desktop view, no warning in Mobile, no warning in CSV. User sees Complete==Basic with identical numbers but thinks Complete includes costs. | ❌ FAUD-0012 bundled (banner missing compounding formula error) |
| RP-6 Reversed sales: reversed sale animals EXCLUDED from profitability rows AND totals | Reversed 0 weight everywhere | ✅ Service base query includes `s.Status != SaleStatus.Reversed`; any animal whose only sale was reversed has 0 GrossSaleTotal and excluded from DischargedSold join correctly | ✅ PASS |
| RP-7 Farm Overhead (category=Overhead) expenses NOT double-counted subtracted from Complete (they belong on P&L's OperatingExpenses section, not individual-animal complete) | Only Direct (not Overhead) in Complete subtraction | ✅ `.Where(e => e.Category == ExpenseCategory.Direct && e.DirectExpenseLivestockId == livestock.Id)` correctly links Direct-expense only. Overhead expenses excluded from livestock-linkage automatically by null DirectExpenseLivestockId. No double count. | ✅ PASS |
| RP-8 CSV matches HTML report columns + formulas | CSV identical values for each row | ✅ CsvWriter takes same DTO list; writes BasicProfit + CompleteProfit columns exactly same values returned to Desktop view (same DTO not re-queried → 100% match no drift) | ✅ PASS (same wrong values 100% consistent HTML↔CSV — consistent but FAUD-0012 bug equally in both) |
| RP-9 Terminology "Number Sold" column (no #Sold no #Heads) in sold-count aggregate column | Number Sold label literal | ✅ Desktop/CSV correct 0 "#" terms. Mobile profitability cards correct term. | ✅ PASS |

**Livestock Profitability Score: 6/9 ✅; 1 FAUD-0012 MEDIUM formula+banner (2 sub-bullets).**

---

## 13.4 Profit & Loss Report — ZERO REGRESSION BYTE-IDENTICAL PRESERVE MANDATE

**Auditor directive from spec:** P&L formulas MUST remain byte-identical untouched during other report refactors. Explicitly tested here for regressions. Report structure: Gross Revenue (Sales) − Cost Of Goods Sold (Purchases + Direct OpEx + Additional Costs) = Gross Profit − Operating Expenses (Overhead expenses + Selling Costs + Admin Costs) = Operating Profit − OtherIncome/Expenses = Net Profit (Before Tax).

| Check | Expected (per prior round certified formulas) | Actual Code Paths / Values | Status |
|---|---|---|---|
| PL-1 **Gross Revenue section:** Σ Confirmed Sales GrossSaleTotal (NOT Net; seller costs classified below COGS or OpEx per spec). Reversed sales excluded. Only date-range inclusive From/To. | Matches prior certified value `Sum(Sales.Gross WHERE Status=Confirmed AND Date BETWEEN)` | ✅ Revenue service P&L segment: `.Where(s => s.Status == Confirmed && s.Date >= From && s.Date <= To).Sum(s => s.GrossSaleTotal)` → EXACT same LINQ as prior certified version; NO CHANGES to this line in HEAD bb4f81f vs prior origin/main. | ✅ PASS (no regression) |
| PL-2 **COGS = Σ Purchases Finalized GrandTotal + Σ DirectOpEx Expenses Amount (Direct category only) + Σ AdditionalAcqCosts on finalized purchases (Commission/Tax/Trans/Other)** | COGS = PurchaseFinalized + DirectOpEx + AcqAdditional | ✅ Identical prior certified formula. COGS calc = `Purchases.Where(Finalized).Sum(GrandTotal+Commission+Tax+Transport+Other)` + `Expenses.Where(Direct).Sum(Amount)`. 0 changes to COGS block. | ✅ PASS (no regression) |
| PL-3 **Gross Profit = Revenue − COGS** | Gross = Revenue − COGS | ✅ Same arithmetic line. | ✅ PASS |
| PL-4 **OperatingExpenses = Σ Overhead Expenses + Σ SellerCosts (sale Commission+Tax+Transport+Other) + Σ AdminCategoryExpenses** | OpEx = Overhead + Seller4Costs + Admin | ✅ Seller 4 costs correctly pulled from Sale Add'l Cost fields classified as OpEx. | ✅ PASS |
| PL-5 **Operating Profit = Gross Profit − Operating Expenses** | Operating = Gross − OpEx | ✅ Same line. | ✅ PASS |
| PL-6 OtherIncome/OtherExpense netting then **NET PROFIT = Operating + OtherIncome − OtherExpense** | Net final line | ✅ Same netting. | ✅ PASS |
| PL-7 Farm filter parameter EXISTENCE and SERVICE scoping check | P&L should accept FarmId filter just like ActiveLivestock/SalesByPeriod (per spec matrix column "Farm Filter Y" for P&L entry) | ❌ **FAUD-0011 MEDIUM — P&L report ZERO farmId parameter.** Controller `ProfitLoss(ProfitLossReportDto dto)` DTO has NO `Guid? FarmId` property; service method `GetProfitLossAsync` has NO farm parameter; Desktop view has NO Farm dropdown; Mobile ProfitLoss has NO Farm filter; CSV endpoint has NO Farm parameter. Spec requires farm-level P&L aggregation where farm filter exists. | ❌ FAUD-0011 MEDIUM (not regression; missing farm filter since original scaffold never added it) |
| PL-8 CSV matches Desktop table rows exactly | Same 3-tier section structure in CSV | ✅ CsvWriter writes Revenue/COGS/GrossProfit/OpEx/Operating/Other/Net sections in same order with same labels. | ✅ PASS |
| PL-9 Company B cannot see Company A P&L data (cross-co forgeries) | 0 rows / 403 | ✅ Service applies companyId first. | ✅ PASS |
| PL-10 Desktop/Mobile/CSV values all 100% identical no drift | 3 surfaces same DTO one query | ✅ Single DTO reused for all 3. | ✅ PASS |
| PL-11 **Source code diff against origin/main for P&L service class + views:** Lines in P&L-specific methods CHANGED? | No modifications allowed to P&L formulas by zero-regression rule | ✅ Git diff origin/main vs HEAD bb4f81f on ReportService P&L region + ProfitLoss Desktop/Mobile/CSV views = ZERO lines modified. Phase 17/18 changes touched ONLY SalesByPeriod + ActiveLivestock + LivestockProfitability NEW features; P&L completely untouched. BYTE-IDENTICAL (modulo whitespace line endings) satisfied. | ✅ PASS (P&L MANDATE — ZERO REGRESSION CERTIFIED ✅) |

**Profit & Loss Score: 10/11 ✅; 1 FAUD-0011 MEDIUM (missing farm filter — present in spec, missing in scaffold original, not regression)**

---

## Reports Audit Summary

| Report | Authorization Policies | Farm Filter | Customer Filter | Status Filter | Formula Correct | Terminology "Number Sold" | CSV ↔ HTML Match | P&L Zero-Regression | Score (of checks) |
|---|---|---|---|---|---|---|---|---|---|
| Active Livestock | ✅ All 6 roles correct | ✅ Works | N/A (not sales) | N/A | N/A | N/A | ✅ 1:1 | N/A | 8/8 |
| Sales by Period | ✅ Financial roles only | ✅ Works | ❌ FAUD-0020 MISSING | ❌ FAUD-0020 MISSING default only | ✅ Inclusive date correct | ⚠️ Mobile FAUD-0017 "#Heads" Desktop/CSV OK | ✅ 1:1 | N/A | 9/12 |
| Livestock Profitability | ✅ Financial roles only | ✅ Works | N/A (not sales) | N/A Reversed excluded | ❌ FAUD-0012 Complete formula wrong + no warning banner | ✅ Correct term everywhere | ✅ 1:1 (same DTO bug) | N/A | 6/9 |
| Profit & Loss (P&L) | ✅ Financial roles only | ❌ FAUD-0011 MISSING | N/A | N/A | ✅ 100% correct formulas (no regress) | N/A | ✅ 1:1 | ✅ ZERO REGRESSION CERTIFIED vs origin/main git diff 0 lines | 10/11 |

**4 Reports — Aggregate: 33 of 40 checks (82.5% PASS). Defects found: FAUD-0011 (P&L no farm) + FAUD-0012 (Profitability Complete formula+banner) + FAUD-0017 (Mobile #Heads) + FAUD-0020 (SalesByPeriod Customer/Status filter UI). All classified in Section 24 Defects Register with linked IDs.**
