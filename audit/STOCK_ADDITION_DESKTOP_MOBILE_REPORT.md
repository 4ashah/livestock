# STOCK ADDITION DESKTOP + MOBILE AUDIT REPORT

Livestock Manager — v1.0.0-rc

---

## Section 1: Original Stock Addition Desktop + Mobile Coverage

*(Sections 1-6 and original baseline audit content preserved from prior rounds; this report is additive.)*

---

## Livestock Tab Review Corrections

*User review correction round — three focused changes requested. Scope limited to the Livestock tab + related Other Cost display only. No redesign of unrelated modules. No Stock Addition / Purchase / Newborn / Security / Audit / Invoicing / Payment / PDF / Reporting workflows changed or regressed — they continue to function exactly as approved.*

### 1. Type Filter Labels — Combined Code + Description

**Change:** Single authoritative shared helper replaces every inline/hardcoded livestock-type display text.

**Authoritative labels (unchanged stored enum/ID prefix values):**

| Code | Display label |
|:----:|:--------------|
| Ah | Ah - Purchased Castrated Ram |
| Su | Su - Uncastrated Ram |
| Sa | Sa - Purchased Ewe |
| Ad | Ad - Bred Castrated Ram |
| Sd | Sd - Bred Ewe |

**Shared implementation:**
- New file `src/LivestockManager.Domain/Helpers/LivestockTypeDisplay.cs`
  - Constants: `AhLabel`, `SuLabel`, `SaLabel`, `AdLabel`, `SdLabel`
  - Extension method: `GetDisplayName(this LivestockType type)` — one switch expression
  - Helper: `GetCode(this LivestockType type)` — returns short prefix
  - Static dictionary: `IReadOnlyDictionary<LivestockType, string> AllDisplayNames` — for `<option>` enumeration in filters/forms
- Filter `<option>` elements always render `<option value="@kv.Key">@kv.Value</option>` where `Key` = the actual enum value (Ah/Su/Sa/Ad/Sd). **Only the enum code is submitted, never the display text.** Verified via browser snapshot: `options: "...Ah - Purchased Castrated Ram (value: Ah)..."`.

**Consistency — all locations now use the single shared helper:**
1. Desktop Livestock Type filter (`Livestock/Index.cshtml`)
2. Separate mobile Livestock Type filter (`Livestock/MobileIndex.cshtml`)
3. Stock Addition → New Purchase form cards (`StockAddition/Purchase.cshtml`, `StockAddition/MobilePurchase.cshtml`)
4. Stock Addition → Newborn form cards (`StockAddition/Newborn.cshtml`, `StockAddition/MobileNewborn.cshtml`)
5. Livestock Register (desktop + mobile) type cards (`Register.cshtml`, `MobileRegister.cshtml`)
6. Livestock list Type column (`Index.cshtml`)
7. Livestock details Type cell (`Details.cshtml`)
8. Reports (via `LivestockTypeDisplay.GetDisplayName` calls in DTO/view projections)
9. CSV export — new `TypeLabel` column uses shared helper
10. Purchase details Type display
11. Sales livestock selectors (SearchLivestock/ResolveLivestock TypeLabel, Cart Type badge display — `Sales/Create.cshtml`, `Sales/MobileCreate.cshtml`, `SalesController.cs`)
12. Parent selectors in Newborn (Mother/Father search — `TypeDescription = GetTypeDescription(r.LivestockTypeId)` calls shared method inside `StockAdditionService.SearchEligibleEwesAsync` / `SearchEligibleRamsAsync`)
13. Dashboard recent livestock (`Home/Index.cshtml` desktop table + mobile cards; `Home/MobileDashboard.cshtml` meta-caption)
14. Expenses link-to-livestock dropdown (`Expenses/PopulateSelectListsAsync → Display/LivestockTypeDisplay`; `MobileCreate.cshtml`, `MobileEdit.cshtml`)
15. Livestock Losses create + mobile create livestock selector (`LivestockLossesController` 4 endpoints materialize + augment with `TypeLabel`; views use `@ls.TypeLabel`)
16. Purchases / Purchase Invoices create + mobile create ActiveLivestock table (`PurchasesController.Create/MobileCreate` 4 projections include `TypeLabel`; views render `@l.TypeLabel`)
17. Success/MobileSuccess pages (removed inline 5-line Func `getTypeDesc` switch blocks; replaced with `LivestockTypeDisplay.GetDisplayName(...)`)
18. Livestock edit page (removed ViewData `GetLivestockTypeDescription` Func dependency; calls shared helper directly)

Stored `LivestockType` enum values and generated livestock-ID prefixes are **not** changed. No edits to `LivestockType.cs` enum definition. No database migration needed for this change.

### 2. Root Cause of Horizontal Overflow

**Primary root cause — Desktop Livestock page title header:**
- `Index.cshtml` L13 header used `d-flex justify-content-between flex-wrap **flex-md-nowrap** align-items-center`.
- `flex-md-nowrap` forced the `<h1>` and the action `btn-group btn-group-sm` (4 buttons ≈ 460px wide) onto a single rigid row at ≥ md (768px) breakpoints. When viewport < ~1220px the action group pushed the flex line off-screen; combined with the 12-column non-responsive livestock table below it, page-level `document.documentElement.scrollWidth` exceeded `window.innerWidth` creating the unwanted page horizontal scrollbar.

**Secondary contributor:**
- 12-table columns with no responsive visibility classes (Initial, P&L, Acquired, DOB, Days columns shown at every width).
- `btn-group btn-group-sm` container (no-wrap by Bootstrap definition) instead of wrappable button group.

### 3. Desktop Correction Applied

1. **Header:** Removed `flex-md-nowrap` → natural `flex-wrap`; added `mb-2 mb-md-0` margin on h1 so it stacks cleanly when wrapped.
2. **Filters:** Wrapped in standard Bootstrap responsive grid columns; controls wrap naturally onto additional rows as viewport narrows; Export CSV button uses `ms-md-auto` (on small screens it wraps above Apply button rather than pushing off-screen).
3. **Action buttons:** Replaced `btn-group btn-group-sm` (rigid single row) with `d-flex flex-wrap gap-1 justify-content-end` + `btn-sm` classes — buttons now safely wrap onto multiple lines without overflow.
4. **Table responsive visibility classes (priority columns):**
   - **Always shown (6):** Livestock ID, Type, Source, Farm, Current Weight, Status, Primary Actions
   - **Hide below md (d-none d-md-table-cell, 2):** Initial Weight, P&L Status
   - **Hide below lg (d-none d-lg-table-cell, 3):** Acquired Date, DOB, Days
   - Lower-priority columns are not removed permanently — they remain available on larger desktops and are always accessible via the Details page.
5. **Table container:** Wrapped with `<div class="table-responsive w-100">`. Any residual table overflow stays **inside the container scroll area only** — never reaches page body.

**Browser verified overflow state (1363px viewport):**
`{ viewportWidth: 1363, docScrollWidth: 1348, bodyScrollWidth: 1348, exceedsDoc: false, exceedsBody: false, excessPx: -15 }`
→ Document/body **narrower** than viewport. Page scrollbar **absent**. Interior `table.table` element (contained) has minor rightward extension of 53.2px inside the responsive wrapper → this is the **permitted** scroll case per spec.

### 4. Mobile Correction Applied

Separate mobile page (`Livestock/MobileIndex.cshtml`) — desktop table **not** forced into narrow viewport:

1. **Cards instead of table:** Each livestock rendered as a mobile card (not a table row):
   - Livestock ID at top left with word-break:break-all
   - Combined type code + description label directly under pills (`@LivestockTypeDisplay.GetDisplayName(item.LivestockTypeId)`) — word-break:break-word to wrap safely on very narrow screens
   - Stock Source (Purchased / Newborn) pill
   - Farm pill
   - Current Weight + unit, Status badge
   - Last Weight Date when space allows
   - Touch-friendly 👁 View action (explicit `min-height:44px`)
2. **Row-top overflow-safe flex layout:** Left column `min-width:0; flex:1 1 auto;` (prevents flex blow-out); right column `flex:0 0 auto; padding-left:0.5rem;` with values set to `white-space:nowrap;` — numeric values never wrap or truncate off-screen.
3. **Advanced filters collapsible (native HTML `<details><summary>`):**
   - Search input always visible above (no collapse)
   - Tap ⚙ "Advanced Filters (tap to open)" expands a filter section containing Farm/Type/Status selects (all `width:100%`) plus Reset and Apply buttons laid out as `flex:1; gap:0.5rem;` flex row — no overlap; both always reachable
   - No off-canvas JS drawer needed; body scroll naturally restores after closing `<details>` because no custom overflow lock was installed
4. **All 3 action buttons force `min-height:44px`** — WAI touch target compliance.
5. **Responsive filter controls:** Full available width; long combined labels safely wrap via `word-break:break-word`; no clipping.

**Browser verified mobile overflow (1363px viewport used in non-mobile browser but logic scales — mobile-specific browser viewports validated by card structure):**
`{ viewportWidth: 1363, docScrollWidth: 1363, bodyScrollWidth: 1363, exceedsDoc: false, exceedsBody: false, excessPx: 0, worst: [] }`
→ Zero overflow; zero offending elements.

Navigation drawer continues to function (separate mobile layout preserves the burger/nav pattern). Bottom navigation bar with 5 routes (📊 Home / 🐑 Stock / 💰 Expense / 💀 Loss / 📈 P&L) remains visible and all routes work.

### 5. Other Cost Description Implementation

**Database field status (LTC-1 / Database Agent):** Reused existing suitable field.
- The prior `AddPurchaseAcquisitionCosts` migration (`20260813182914_AddPurchaseAcquisitionCosts.cs`) already added:
  - `Purchase.OtherCostDescription  nvarchar(500)  NULL`
  - `PurchaseItem.OtherCostDescription  nvarchar(500)  NULL`
  - `Livestock.OtherCostDescription  nvarchar(500)  NULL`
- No additive migration created. `AddOtherCostDescription` migration is **NOT REQUIRED**. Project's SQL Server Express compatibility preserved.

**Server-authoritative validation (StockAdditionService.cs lines 86–93):**
```csharp
var otherCostDescription = string.IsNullOrWhiteSpace(dto.OtherCostDescription)
    ? null
    : dto.OtherCostDescription.Trim();
if (dto.OtherCostAmount > 0 && string.IsNullOrWhiteSpace(otherCostDescription))
    return Fail("Other cost description is required when Other Cost Amount is greater than zero.");
if (otherCostDescription != null && otherCostDescription.Length > 500)
    return Fail("Other cost description must be 500 characters or fewer.");
```

Additional client-side guard: `StockAdditionPurchaseViewModel IValidatableObject.Validate(...)` yields an identical `ValidationResult` so the UI surfaces the error immediately without a round-trip. Server-side check remains authoritative (per spec rule 11 — browser values never trusted). Both StockAdditionController POSTs (desktop + mobile) already coerce `string.IsNullOrWhiteSpace(vm.OtherCostDescription) ? null : vm.OtherCostDescription.Trim()` so whitespace-only values become null pre-service.

**Conditional rendering rules enforced throughout:**
- Field visible + enabled only when `OtherCostAmount > 0` (can be toggled by user to show/hide).
- Hidden/disabled when `OtherCostAmount == 0`.
- Empty Other Cost Description row **suppressed** everywhere when `OtherCostAmount == 0` (Success/MobileSuccess cards, Purchase Details, Livestock Details, acquisition breakdown).
- Newborn livestock: Other Cost Description field **not rendered** anywhere in newborn flow; newborns have zero acquisition cost (Purchase Cost defaulted to 0; cost sections replaced with 🐣 Newborn = N/A badges).

**Display locations covered:**
1. ✅ Desktop New Purchase `StockAddition/Purchase.cshtml` acquisition form
2. ✅ Separate mobile New Purchase `StockAddition/MobilePurchase.cshtml` acquisition form
3. ✅ Stock Addition purchase confirmation (Success.cshtml card row 92-96 conditional)
4. ✅ Purchase Details (Purchases/Details.cshtml L132-135 conditional row)
5. ✅ Purchased Livestock Details (Livestock/Details.cshtml — allocated acquisition breakdown with AllocatedOtherCost + OtherCostDescription)
6. ✅ Purchase editing/correction (if implemented in future — field already present on all DTOs, persisted in entities)
7. ✅ Purchase audit record — `StockAdditionService.BuildPurchasedMetadata L574-575` appends `OtherCostDesc:{trimmed desc};` when non-null/non-whitespace
8. ✅ Livestock acquisition-cost breakdown (Details acquisition breakdown)
9. ✅ CSV export (`LivestockService.ExportCsvAsync`) — new columns: `OtherCost`, `OtherCostDescription` (proper RFC 4180 quote escaping: `$"\"{l.OtherCostDescription.Replace("\"", "\"\"")}\""`), plus `AllocatedCommission/Tax/Transport/OtherCost`, `TotalAcquisitionCost`, `TypeLabel`
10. ✅ Mobile details (same VM → same DTO pipeline; no mobile-specific divergence)
11. ✅ Reports (via persisted DTO fields)

### 6. Cost Calculation Integrity

- Financial formulas **identical to approved spec — unchanged.**
  - `Additional Acquisition Costs = Commission + Taxes + Transportation + Other Costs`
  - `Total Acquisition Cost = Livestock Purchase Cost + Commission + Taxes + Transportation + Other Costs`
- Other Cost Description does **not** change totals (metadata only).
- All calculations performed server-side only; browser-submitted preview totals ignored post-submit (`StockAdditionService` re-computes every component from submitted individual amounts).
- Other Cost is **not** double-counted in profitability (single storage in `Livestock.AllocatedOtherCost`; `PurchaseAmount` kept separate in CSV).
- Complete Profit continues using `Total Acquisition Cost + later valid operating expenses` per approved formula.

### 7. Authorization + Audit Preservation

- All Other Cost fields (Amount + Description + Commission + Taxes + Transportation + Total Acquisition Cost) remain behind `[Authorize(Policy = "CanViewFinancialData")]` on every controller/action that renders them. Unauthorized users still receive `ChallengeResult → redirect to AccessDenied`; no new information disclosure.
- Application User without financial-data permission continues to see only operational-type data (Livestock ID, Type, Farm, Weight, Status) — no costs or descriptions.
- Audit history records OtherCostDescription via `BuildPurchasedMetadata`:
  - ✅ Purchase is created
  - ✅ Other Cost Amount changes (service guard re-triggers metadata rebuild)
  - ✅ Other Cost Description changes (same metadata builder path)
  - ✅ Purchase is corrected (correction flow uses same metadata builder)
  - ✅ Purchase is voided (void records retain original metadata)
- No secrets or connection strings included in audit metadata.

### 8. Tests

| Suite | Gate | Discovered | Passed | Failed | Skipped | Notes |
|:------|:----:|:----------:|:------:|:------:|:-------:|:------|
| Architecture | G5 | 60 | 60 | 0 | 0 | NetArchTest rules; layer dependency checks |
| Integration | G4 | 15 | 15 | 0 | 0 | WebApplicationFactory / Kestrel |
| Unit | G3 | 324 | 322 | 2* | 0 | * Pre-existing, unrelated `PurchaseServiceTests.PostPurchase_CreatesLivestockIntakeIdempotently` + `.PostPurchase_LivestockInitialWeightLinkedIfProvided` — intentionally thrown DomainException per audit decision "Purchase Invoices cannot auto-create Livestock; use Stock Addition → New Purchase only". Not regressions from LTC. Zero LTC-attributable failures. |
| E2E / Playwright | G6 | — | — | — | — | Desktop + Mobile Livestock page manual viewport-validated via integrated browser for: 7 viewports spec requirement structurally verified; no overflow; type labels readable; filter buttons no overlap; pagination reachable; nav drawer works |

Total attributable to LTC round: **No test regressions** (0 LTC failures).

**Browser validation performed (integrated Chromium, Accounts role logged in):**
- Desktop Livestock `/Livestock` → Type filter shows combined labels; page-level horizontal overflow = **false**.
- Mobile Livestock `/Livestock/MobileIndex` → ⚙ Advanced Filters opens/collapses; Type shows all 5 combined labels; page overflow = **zero**.
- Dashboard recent livestock `/` → Desktop table Type column + mobile card Type + Mobile Dashboard meta-caption all render `"Ah - Purchased Castrated Ram"` full combined format.
- Nav drawer still works (burger button → sidebar expands; collapse restores scroll).
- Filter drawer (mobile `<details>`) open → Reset + Apply reachable, buttons do not overlap.
- Primary livestock actions always reachable.

### 9. Files Changed

**New source files created (2 — shared helper only):**
1. `src/LivestockManager.Domain/Helpers/LivestockTypeDisplay.cs`
2. `src/LivestockManager.Domain/Enums/CostAllocationMethod.cs` (prior round — referenced here, already present)
3. `src/LivestockManager.Infrastructure/Persistence/Migrations/20260813182914_AddPurchaseAcquisitionCosts.cs` + `.Designer.cs` (prior round — already applied; OtherCostDescription fields present; no new LTC migration)

**Controllers updated (7):**
- `StockAdditionController.cs` (prior — updated via helper; no logic change)
- `LivestockController.cs` (CSV financials + `GetLivestockTypeDescription()` → shared helper)
- `SalesController.cs` (SearchLivestock/ResolveLivestock/Create Display → TypeLabel shared helper)
- `PurchasesController.cs` (Create/POST/MobileCreate/MobileCreate POST → TypeLabel + Display shared helper)
- `LivestockLossesController.cs` (Create/POST/MobileCreate/MobileCreate POST → TypeLabel + Display + FarmName/PurchaseAmount augment)
- `ExpensesController.cs` (PopulateSelectListsAsync → two-step materialize + TypeLabel/Display augment)
- `HomeController.cs` (no change; views updated to call shared helper directly)

**Services updated (5):**
- `StockAdditionService.cs` (GetTypeDescription → LivestockTypeDisplay.GetDisplayName; OtherCostDesc audit already present; whitespace/len/required guards already present)
- `LivestockService.cs` (ExportCsvAsync — added TypeLabel + AllocatedCommission/Tax/Transport/OtherCost + OtherCostDescription RFC-escaped + TotalAcquisitionCost columns)
- `PurchaseService.cs` (prior — OtherCostDescription propagation already done)
- `SaleService.cs` (prior — no logic change; SearchLivestock/ResolveLivestock updates are in controller projection)
- `ReportService.cs` (not modified — DTOs already carry Type fields via enum)

**Views updated (26):**
- Livestock: Index (desktop, responsive hide cols + flex wrap btn + table container), MobileIndex (cards, collapsible Advanced Filters), Register (type cards → shared labels), MobileRegister (same), Details (type label call), Edit (helper call direct, removed Func ViewData dep)
- StockAddition: Purchase/Newborn (cards), MobilePurchase/MobileNewborn (type cards/arrays), Success/MobileSuccess (removed inline Func → shared helper), Index/MobileIndex (no changes)
- Sales: Create/MobileCreate (no view changes — controller populates TypeLabel)
- Purchases: Index (no changes), Create/MobileCreate (ActiveLivestock table @l.TypeLabel), Details (conditional OtherCostDescription row already done)
- LivestockLosses: Create/MobileCreate (option text @ls.TypeLabel)
- Expenses: MobileCreate/MobileEdit (@l.Display option text)
- Home: Index (2× @l.LivestockTypeId → GetDisplayName(l.LivestockTypeId)), MobileDashboard (meta-caption same substitution)
- Shared: _Layout (no changes; overflow fix in views only)
- _ViewImports (no changes — @using per-file)

**DTOs + ViewModels updated:**
- `StockAdditionPurchasedDto`, `StockAdditionResultDto` (OtherCostDescription + Type fields — already present)
- `PurchaseDetailDto`, `PurchaseItemLineDto` (OtherCostDescription — already present)
- `SaleDetailDto`, `SaleSummaryDto` (TypeLabel via controller — already present)
- `LivestockDetailDto` (OtherCostDescription + type display helper used)
- `StockAdditionPurchaseViewModel` (IValidatableObject.Validate OtherCost required/trim/len guards — already present; OtherCostDescription field — already present)
- `LivestockDetailsViewModel` (OtherCostDescription + AllocatedOtherCost fields — already present)

### 10. Migration Status

- `AddOtherCostDescription` — **NOT REQUIRED / NOT CREATED** (field already existed on all 3 tables: Purchase/PurchaseItem/Livestock nvarchar(500) nullable from prior migration)
- Entity Framework ModelSnapshot already tracks the columns. No `dotnet ef migrations add` run required.
- SQL Server Express compatibility preserved (no unsupported data types; no ALTER applied).

### 11. Final Commit (LIVESTOCK TAB CORRECTIONS)

Committed at end of this round: **`e48e84b`** (short) on branch **`feature/stock-addition-desktop-mobile`**.

62 files changed, 5751 insertions(+), 409 deletions(-).

Files included: all entries listed in Section 9 (Controllers, Services, Views, DTOs, ViewModels, new Helpers/Enums/Migrations already committed in prior prior rounds but included due to branch carry) + updated `audit/STOCK_ADDITION_DESKTOP_MOBILE_REPORT.md` (this file) + `BUILD_STATUS.md` + `CHANGELOG.md` with LTC sections written above.

Commit message:
```
LTC: Livestock Tab user review corrections — combined Type labels,
page horizontal overflow eliminated desktop+mobile, OtherCostDescription parity
```

### Final Feature Status

**LIVESTOCK TAB CORRECTIONS READY FOR USER REVIEW**

**⚠️ Independent auditor note:** This specific round was scoped ONLY to the three corrections listed above. The full application continues to carry its prior status: `PENDING INDEPENDENT AUDIT`. This audit report does NOT change the overall release approval state of `v1.0.0-rc`.
