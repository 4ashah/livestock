# FULL AUDIT BACKEND ARCHITECTURE RESULTS — Section 4

**Status:** COMPLETE · Audited Commit: bb4f81f

---

## 4.A Dependency Direction

| Rule | Result |
|---|---|
| Web → App/Domain/Infra | ✅ PASS |
| Infra → App/Domain | ✅ PASS |
| App → Domain only | ✅ PASS |
| Domain → Zero Project Refs | ✅ PASS; ⚠️ FAUD-0021 (Domain carries EntityFramework NuGet) |
| Production → No Test Refs | ✅ PASS |
| Circular Refs | ✅ PASS (NetArchTest 5/5) |
| Desktop+Mobile Shared Backend | ✅ PASS (25 Mobile actions call same services) |

## 4.B Architecture Red Flags (18 Checks)

| Flag | Count | Severity | Defect ID |
|---|---|---|---|
| RF-1 Circular | 0 | — | — |
| RF-2 Biz Logic in Views | 0 | — | — |
| RF-3 Desktop/Mobile Dupe Calc | 0 | — | — |
| RF-4 Entities Direct Forms | 5 scaffold | LOW | — |
| RF-5 Generic Repo Dupe | 0 | — | — |
| RF-6 Hardcoded Role Strings Views | 6 | MEDIUM | FAUD-0016 |
| RF-7 Hardcoded Conn Strings | 0 | — | — |
| RF-8 DB Access in Views | 0 | — | — |
| RF-9 Controllers Bypass Services | 13/20 (trivial CRUD) | MED INFO | DocsController no IDocumentsService |
| RF-10 Mobile Separate Calcs | 0 | — | — |
| RF-11 Empty Placeholders | 0 | — | — |
| RF-12 TODO/FIXME/HACK src | 0 | — | (only wwwroot/lib/ vendor files) |
| RF-13 Unused Packages | Not verified | LOW | — |
| RF-14 Unused Classes | Not verified | LOW | — |
| RF-15 Duplicate Services | 0 | — | — |
| RF-16 Dead Routes / 404 | 6 scaffolds | LOW | FAUD-0030 (Reports Create/Edit/Details, Invoices/Sales/Payments Edit) |
| RF-17 Dead Nav Links | 0 | — | Sidebar all resolve |
| RF-18 Domain→EF Pkg Ref | 1 DirectRef | MEDIUM | FAUD-0021 |

## 4.C Service Layer Usage by Financial Controller

| Controller | Uses Service? |
|---|---|
| StockAddition | ✅ IStockAdditionService |
| Sales | ✅ ISaleService (All actions) |
| Purchases | ✅ IPurchaseService |
| Invoices | ✅ IInvoiceService |
| Payments | ✅ IPaymentService |
| Receipts | ✅ IReceiptService |
| Reports | ✅ IReportService (All 4 + CSV) |
| Expenses | ✅ IExpenseService |
| Livestock | ✅ ILivestockService |
| Home/Dashboard | ✅ IDashboardService |
| Documents | ❌ Direct _db; storage via IProtectedDocumentStorage OK |
| 8 Admin CRUD | ❌ Direct _db pattern (Accepted) |

**SUMMARY:** 9/10 financial-critical controllers use service layer. 13 trivial bypasses = accepted. Key defects FAUD-0016/0021/0030 registered.
