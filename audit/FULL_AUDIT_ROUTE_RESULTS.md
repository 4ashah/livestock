# FULL AUDIT ROUTE INVENTORY & RESULTS — Section 5

**Status:** COMPLETE
**Totals:** 20 Controllers · 127 Actions · 3 Health · 4 AllowAnonymous · 25 Mobile · 50 ValidateAntiForgeryToken

---

## 5.A Desktop Routes (Top 24 Critical) + 5.B Mobile Routes (All 20) = 44 Covered

| Desktop D- | Route | Auth | Company Scope | Result |
|---|---|---|---|---|
| D-1 | /Account/Login GET+POST | ANON | N/A | ✅ PASS |
| D-2 | /Account/Logout POST | AUTH | N/A | ✅ PASS |
| D-3 | /Home/AccessDenied | ANY | N/A | ✅ PASS |
| D-4 | /Home/Index Dashboard | CanViewOperationalData | Yes | ⚠️ FAUD-0002 Fin leak Viewer/DataEntry |
| D-5..D-9 | Companies CRUD ×5 | CanManageSystem SysAdmin | Global | ✅ PASS |
| D-10..D-13 | Farms CRUD ×4 | CanManageFarm | Yes | ✅ PASS |
| D-14 | UserManagement/Index + AddUser | CanManageSystem | Yes | ⚠️ FAUD-0015 No ambient transaction |
| D-15 | Audit + Settings GET/POST | Audit / CanManageSystem | Yes | ✅ Settings companyId guard SysAdmin only |
| D-16..D-23 | Livestock CRUD + Weights + Discharge ×8 | CanManageLivestock | Yes + Farm | ⚠️ FAUD-0003 Register fallback FirstAsync |
| D-24 | LivestockLosses CRUD ×3 + Mobile | CanManageLivestock | Yes | ✅ PASS |
| D-25..D-28 | StockAddition ×4 + Mobile×4 | CanManageLivestock | Yes | ✅ Shared backend |
| D-29..D-32 | Customers CRUD ×4 | CanManageCustomers | Yes | ✅ Entity filter OK · FAUD-0008 Nav leak |
| D-33..D-36 | Suppliers CRUD ×4 | CanManageSuppliers | Yes | ✅ PASS |
| D-37..D-39 | Purchases ×3 + Mobile×2 | CanManagePurchases | Yes | ⚠️ FAUD-0004 CreateDraft no tx |
| D-40..D-42 | Expenses ×4 + Mobile×3 | CanManageExpenses | Yes + Farm | ✅ PASS |
| D-43..D-45 | Sales ×4 + Mobile×2 + Reverse/BulkAdd | CanManageSales/Accounting | Yes + Farm | ⚠️ FAUD-0005 CreateDraft no tx |
| D-46 | Invoices ×4 + DownloadPdf | CanManageAccounting | Yes | ⚠️ FAUD-0008 Customer Nav leak |
| D-47 | Payments ×4 + Reverse | CanManageAccounting | Yes | ⚠️ FAUD-0013 ReversedByUserId Empty |
| D-48 | Receipts ×3 (Generate + DownloadPdf) | CanManageAccounting | Yes | ⚠️ FAUD-0008 Nav leak + FAUD-0014 Clock UtcNow |
| D-49..D-52 | Reports ×4 Desktop + CSV + 4 Mobile | Role Varied | Yes + Farm (mostly) | ⚠️ FAUD-0011/0012/0017/0020 (4 defects) |
| D-53 | Documents List/Upload/Download | Upload=CanViewFin; List/Dl=WRONG | Yes storage guard | ⚠️ FAUD-0010 List+Download wrong policy |

### Mobile M-1..M-20 Complete Inventory

| M- | Route | Exists? | Result |
|---|---|---|---|
| M-1 | Home/MobileDashboard | ✅ | ⚠️ FAUD-0002 same fin leak |
| M-2 | Livestock/MobileIndex | ✅ | ✅ |
| M-3 | Livestock/MobileRegister | ✅ | ✅ |
| M-4..M-5 | LivestockLosses Mobile×2 | ✅ | ✅ |
| M-6..M-8 | Expenses Mobile×3 | ✅ | ✅ Full coverage |
| M-9..M-10 | Purchases Mobile×2 | ✅ | ✅ |
| M-11..M-12 | Sales Mobile×2 | ✅ | ✅ |
| M-13..M-16 | StockAddition Mobile×4 | ✅ | ✅ All 4 pages |
| M-17..M-20 | Reports Mobile×4 (4 reports) | ✅ | ⚠️ FAUD-0017 #Heads; others per D-49-52 |

**Mobile Coverage Gap (12 controllers):** Account, Audit, Companies, Customers, Documents, Farms, Invoices, Payments, Receipts, Settings, Suppliers, UserManagement → FAUD-0026 LOW defer.

## 5.C Adversarial Route Tests (10)

| Test | Expected | Actual | Result |
|---|---|---|---|
| ADV-R1 Anon /Sales/Create | 302 Login | 302 | ✅ |
| ADV-R2 Viewer /Sales/Confirm POST | 403/AccessDenied | Forbidden Correct Policy | ✅ |
| ADV-R3 POST /Purchases/Create NoAntiforgery | 400 | 400 | ✅ |
| ADV-R4 Invalid Guid /Invoices/Details/00s | 404 | NotFound | ✅ |
| ADV-R5 Missing id /Invoices/Details | 400 binding fail | 400 | ✅ |
| ADV-R6 Cross-Co Invoice Id URL tamper | 404 | Invoice header OK; ⚠️ FAUD-0008 .Customer nav | PARTIAL |
| ADV-R7 Cross-Farm filter Livestock | 0 rows | Farm scoped service | ✅ |
| ADV-R8 Valid POST /Expenses/Create | 302 Details | 302 | ✅ |
| ADV-R9 Invalid negative Amount -100 | ModelState invalid | Validator works | ✅ |
| ADV-R10 Double Submit Sale Confirm | 1 success + unique block | Unique LivestockId index | ✅ |

## 5.D Route Totals

| Metric | Value |
|---|---|
| Controller Actions | 127 |
| Health endpoints | 3 |
| AllowAnonymous | 4 |
| Authenticated actions | 120 |
| Mobile-prefixed actions | 25 |
| POST + Antiforgery | 50 |
| 10 Adversarial Tests Pass | 9 / 10 PASS (1 PARTIAL FAUD-0008) |
| Dead Routes (Placeholder) | 6 FAUD-0030 |
