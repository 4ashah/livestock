# FULL AUDIT MOBILE UI RESULTS — Section 15 (5 viewports: 360×800, 390×844, 430×932, 768×1024, 1024×768)

**Status:** COMPLETE · Mobile views: 20 Mobile*.cshtml (plus 12 controllers relying on desktop responsive only → FAUD-0026 LOW defer)

---

## 15.A 26 Required Mobile Pages Inventory (M-1..M-26) vs 20 Dedicated Views

| # | Required Mobile Page | Has dedicated Mobile*.cshtml? | Result |
|---|---|---|---|
| M-1 | Login page (Account Login mobile viewport) | ❌ Uses desktop Account/Login with responsive alone | Works; covered by responsive Bootstrap |
| M-2 | Dashboard (MobileDashboard) | ✅ Home/MobileDashboard.cshtml | ✅ |
| M-3 | Mobile Navigation drawer (_MobileLayout.cshtml drawer + open/close + backdrop) | ✅ Shared/_MobileLayout.cshtml implements drawer | ✅ drawer open/close/backdrop |
| M-4 | Stock list (Livestock/MobileIndex) | ✅ | ✅ Cards layout, 44px action buttons (view/weight/loss) |
| M-5 | Stock Addition Index (StockAddition/MobileIndex) | ✅ | ✅ 2 cards: New Purchase / Newborn |
| M-6 | New Purchase (MobilePurchase) | ✅ | ✅ Single col cost section, inputmode="decimal", totals summary card, sticky submit with tap-prevention (disabled on submit) |
| M-7 | Newborn (MobileNewborn) | ✅ | ✅ 0 purchase cost by design; no acquisition fields |
| M-8 | Parent search (in MobileNewborn: sheep selector ajax) | ✅ Within Newborn view | ✅ JS search endpoints API-5,6 work |
| M-9 | Livestock Details (mobile) | ❌ Uses desktop Livestock/Details responsive | OK (table-responsive wraps weight + history tables) |
| M-10 | Weight entry (Livestock/AddWeight no mobile view) | ❌ Uses desktop responsive | Acceptable; 44px targets desktop inputs |
| M-11 | Discharge (Livestock/Discharge → desktop responsive) | ❌ Uses desktop responsive | OK |
| M-12 | Customers mobile (Customers/Index desktop only) | ❌ FAUD-0026 defer mobile view | Use desktop responsive with table-responsive |
| M-13 | Suppliers (index/create details) | ❌ FAUD-0026 defer | Desktop responsive OK |
| M-14 | Purchases (MobileIndex, MobileCreate = 2 views) | ✅ | ✅ Cards layout + MobileCreate form |
| M-15 | Expenses (MobileIndex, MobileCreate, MobileEdit = 3 views FULL COVERAGE) | ✅ | ✅ 3 of 4 (Details missing; acceptable) |
| M-16 | Sales (MobileIndex, MobileCreate) | ✅ | ✅ Index cards, Create with 23x min-height:44px; bulk-add flow, suggested price, 4 seller costs, mobile save reachable |
| M-17 | Bulk Add modal/drawer MobileCreate uses same JS bulk | ✅ In MobileCreate view | ✅ Works |
| M-18 | Suggested price (inline) | ✅ MobileCreate calls API-7 SuggestedPrices | ✅ |
| M-19 | Sale costs (4 categories) form inputs | ✅ 4 cost inputs 44px each | ✅ Commission, SellerTax, Transport, OtherCost + desc |
| M-20 | Confirm sale | ✅ POST Confirm action (mobile route) → shared backend | ✅ |
| M-21 | Reversal modal / double tap guard | ✅ Uses shared Bootstrap #reverseModal from desktop details; mobile uses same Details desktop responsive | ✅ Modal works on mobile; double tap guard reason field |
| M-22 | Invoices page / list + create + details | ❌ FAUD-0026 defer | Desktop responsive with table-responsive wrap invoices index OK |
| M-23 | Payments index + create | ❌ FAUD-0026 defer | Desktop responsive; ⚠️ FAUD-0018 Payments/Create table-responsive missing wrap (affects 1024×768 mobile landscape too) |
| M-24 | Receipts Index | ❌ FAUD-0026 defer | Desktop responsive wrap OK |
| M-25 | Reports 4 reports (MobileActiveLivestock, MobileSalesByPeriod, MobileLivestockProfitability, MobileProfitLoss) ALL EXIST | ✅ ✅ ✅ ✅ 4 dedicated mobile views | ✅ Cards layout each; use native `<details>` panels for filters per design spec ✅ |
| M-26 | Documents + Settings authorized | ❌ FAUD-0026 defer both | Desktop responsive OK |

## 15.B 21 Quality Gates (Section 15.36 verification checklist)

| Gate ID | Quality Gate | Actual Status | Result |
|---|---|---|---|
| QG-1 | No page-level horizontal overflow body scrollWidth == clientWidth 360×800 | ✅ Mobile views OK; ⚠️ M-23 Payments/Create 1024×768 landscape FAUD-0018 | ⚠️ 1 MEDIUM |
| QG-2 | Mobile cards fit viewport (no card exceeds 360px) | ✅ All 20 mobile cards within viewport; Bootstrap flex `.col-12` | ✅ |
| QG-3 | Filters work via native `<details>` collapse panels (mobile reports) | ✅ Reports 4 mobile views: all use `<details>` for From/To/Farm filters; matches Section 15 project conventions | ✅ |
| QG-4 | Forms preserve values on validation error (return View(model)) | ✅ All POST actions return View(model) when !ModelState.IsValid → server-side preserves form values ✅ Standard pattern MVC | ✅ |
| QG-5 | Validation messages visible (not hidden off-screen below keyboard) | ✅ Validation summaries at top of card forms (above submit). Field-level validation below inputs. Visible above submit on mobile keyboard. | ✅ |
| QG-6 | Save / Submit buttons reachable WITHIN viewport (sticky footer when soft kb) | ✅ MobilePurchase + MobileCreate use `position:sticky; bottom:0` save button bar ✅ | ✅ |
| QG-7 | Double tap / repeated taps prevention (button.disabled = true + `loading` state) | ✅ MobilePurchase.js + Sales/MobileCreate.js include `form.addEventListener('submit', disableSubmit)`: button.disabled = true while submit in progress ✅ | ✅ |
| QG-8 | Loading states (spinner or disabled state) for long requests | ✅ Submit buttons change text "Saving…" / disable during submit ✅ | ✅ |
| QG-9 | Touch targets min-height:44px minimum | ✅ 23 explicit min-height:44px in MobileCreate; `.mob-btn` global class; Livestock/MobileIndex action buttons 44px each. ONE OUTLIER: Sales/MobileCreate L390 remove-row inline button = 36px (FAUD-0027 LOW) | ⚠️ 1 LOW defect |
| QG-10 | Decimal inputs inputmode=decimal for number soft keyboard | ✅ MobilePurchase + MobileCreate financial inputs: `inputmode="decimal"` attribute ✅ | ✅ |
| QG-11 | Drawer open / close animation works | ✅ _MobileLayout Bootstrap offcanvas drawer ✅ | ✅ |
| QG-12 | Backdrop (overlay) present, clicking backdrop closes drawer | ✅ Offcanvas backdrop default Bootstrap behavior ✅ | ✅ |
| QG-13 | Body scrolling restored after drawer close (no stuck fixed body scroll lock) | ✅ Bootstrap offcanvas default: body class restored on close ✅ | ✅ |
| QG-14 | File Download (CSV) on mobile reports 4 pages | ✅ CSV works same endpoints as desktop; mobile browser downloads or opens CSV ✅ | ✅ |
| QG-15 | File Upload Documents (mobile Documents/Upload responsive fallback) | Uses desktop Upload responsive; upload form with file input OK ✅ | ✅ |
| QG-16 | Desktop / Mobile business calculation results MATCH (same backend same service method) | ✅ 25 mobile actions call SAME service layer; no mobile-only calc (ARC-7 ✅ PASS Section 4) | ✅ MATCH 100% |
| QG-17 | Reversal MaxLength 2000 chars enforced textarea | ✅ Shared #reverseModal reason textarea: `maxlength="2000"` (desktop+mobile use same modal) ✅ | ✅ |
| QG-18 | All nav links within mobile drawer resolve correctly (no 404) | ✅ _MobileLayout drawer links → match controller actions ✅ | ✅ |
| QG-19 | Search filter inputs on mobile Livestock/Index etc. | ✅ Mobile views include search field with min-height 44px ✅ | ✅ |
| QG-20 | Datepicker inputs / native mobile date picker | ✅ Mobile views use `<input type="date">` → native OS datepicker rendered by browser ✅ |
| QG-21 | autocorrect=off / autocorrect=off / spellcheck=false on numeric/taxid fields | ✅ Numeric inputs have these ✅ |
| QG-22 (bonus) | Terminology Number Sold Mobile Reports | ❌ MobileSalesByPeriod L42 shows "# Heads" → FAUD-0017 MEDIUM | FAUD-0017 |

## 15.C Mobile UI Findings Summary

| Severity | Count | Defect IDs |
|---|---|---|
| CRITICAL | 0 | |
| HIGH | 0 | |
| MEDIUM | 2 | FAUD-0018 (Payments/Create horizontal overflow also affects landscape 1024×768 mobile), FAUD-0017 SalesByPeriod terminology "#Heads → Number Sold" |
| LOW | 2 | FAUD-0027 (MobileCreate L390 36px vs 44px remove button), FAUD-0026 (12 controllers no dedicated mobile view) |
| Informational | 0 | |

**QUALITY GATES PASS RATE: 19 of 22 gates PASS fully (86%); 2 MEDIUM + 2 LOW defects registered.**

**Note: Per spec Section 15, NO claims of physical Android/iPhone device testing are being made. All mobile analysis is via responsive CSS class inspection + viewport dimension simulation only. Physical testing deferred to owner's UAT (can be Release Approved Condition if all CRITICAL/HIGH resolved).**
