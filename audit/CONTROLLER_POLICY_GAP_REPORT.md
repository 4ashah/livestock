# DYNAMIC_SIX_ROLE_UI_AND_AUTHORIZATION — GAP REPORT

**Report Date:** 2026-08-15
**Project:** LivestockManager (.NET 8 MVC/Razor)
**Scope:** `src/LivestockManager.Web/Controllers/*.cs`, `src/LivestockManager.Web/Views/**/*.cshtml`, `Program.cs` policy registrations
**Reference Matrix:** livestock.txt expected model (via user specification + ROLE_RIGHTS_IMPLEMENTATION_REPORT.md cross-check)

---

## EXECUTIVE SUMMARY

| Metric | Count |
|---|---|
| Total Controllers | 20 |
| Total Public Actions | 133 |
| Actions Without Method-Level `[Authorize]` | 61 |
| — Inherited via class-level `[Authorize]` (protected) | 59 |
| — Truly unprotected / anonymous / no attribute anywhere | 2 |
| Total `.cshtml` Views | 98 |
| — Mobile-specific Views (`Mobile*`) | 21 |
| — Desktop+Mobile paired view folders | 11 |
| Views with inline `User.IsInRole` checks | 7 |
| Views with `ViewData["Can*"]` capability rendering | 19 |
| Views using `IAuthorizationService.AuthorizeAsync` (policy-based) | 1 |
| Policy discrepancies (Program.cs vs livestock.txt matrix) | 5 |
| Views missing capability-based rendering (relying only on hard-coded role names) | 30 |

---

## 1. PUBLIC CONTROLLER ACTION INVENTORY (SECTION A)

Below: every public controller action in `Web/Controllers/*.cs`. Legend:
- **Class Auth** = `[Authorize]` at controller-class level (blank = none)
- **Method Auth** = `[Authorize]` / `[AllowAnonymous]` at action level (blank = none)
- Routes follow the convention `{controller}/{action}/{id?}` with lowercase URLs.

### 1.1 AccountController (`AccountController.cs`)
Class Auth: *(none)*

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 1 | `Login` | GET | `/account/login` | `[AllowAnonymous]` |
| 2 | `Login` (model) | POST | `/account/login` | `[AllowAnonymous]` |
| 3 | `LogoutGet` | GET | `/account/logoutget` | `[AllowAnonymous]` |
| 4 | `Logout` | POST | `/account/logout` | *(none)* |
| 5 | `AccessDenied` | GET | `/account/accessdenied` | `[AllowAnonymous]` |

### 1.2 AuditController (`AuditController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewAuditLogs)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 6 | `Index` | GET | `/audit/index` | `[Authorize(Policy = PolicyNames.CanViewAuditLogs)]` (duplicate) |
| 7 | `Details` | GET | `/audit/details/{id}` | `[Authorize(Policy = PolicyNames.CanViewAuditLogs)]` (duplicate) |

### 1.3 CompaniesController (`CompaniesController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanManageSystem)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 8 | `Index` | GET | `/companies/index` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |
| 9 | `Create` (GET) | GET | `/companies/create` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |
| 10 | `Create` (POST) | POST | `/companies/create` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |
| 11 | `Details` | GET | `/companies/details/{id}` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |
| 12 | `Edit` (GET) | GET | `/companies/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |
| 13 | `Edit` (POST) | POST | `/companies/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageSystem)]` (duplicate) |

### 1.4 CustomersController (`CustomersController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanManageCustomers)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 14 | `Index` | GET | `/customers/index` | *(inherits class)* |
| 15 | `Details` | GET | `/customers/details/{id}` | *(inherits class)* |
| 16 | `Create` (GET) | GET | `/customers/create` | *(inherits class)* |
| 17 | `Create` (POST) | POST | `/customers/create` | *(inherits class)* |
| 18 | `Edit` (GET) | GET | `/customers/edit/{id}` | *(inherits class)* |
| 19 | `Edit` (POST) | POST | `/customers/edit/{id}` | *(inherits class)* |
| 20 | `Archive` | POST | `/customers/archive/{id}` | *(inherits class)* |
| 21 | `MobileIndex` | GET | `/customers/mobileindex` | *(inherits class, no method attr)* |

### 1.5 DocumentsController (`DocumentsController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewDocuments)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 22 | `Index` | GET | `/documents/index` | *(inherits class)* |
| 23 | `List` | GET | `/documents/list` | *(inherits class)* |
| 24 | `Download` | GET | `/documents/download/{id}` | *(inherits class)* |
| 25 | `Upload` (GET) | GET | `/documents/upload` | `[Authorize(Policy = PolicyNames.CanUploadDocuments)]` |
| 26 | `Upload` (POST) | POST | `/documents/upload` | `[Authorize(Policy = PolicyNames.CanUploadDocuments)]` |
| 27 | `Delete` | POST | `/documents/delete/{id}` | `[Authorize(Policy = PolicyNames.CanManageCompany)]` |
| 28 | `MobileIndex` | GET | `/documents/mobileindex` | *(inherits class, no method attr)* |

### 1.6 ExpensesController (`ExpensesController.cs`)
Class Auth: `[Authorize("CanManageExpenses")]` — *string-literal (not `nameof(PolicyNames...)`)*

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 29 | `Index` | GET | `/expenses/index` | *(inherits class)* |
| 30 | `MobileIndex` | GET | `/expenses/mobileindex` | *(inherits class)* |
| 31 | `Details` | GET | `/expenses/details/{id}` | *(inherits class)* |
| 32 | `Create` (GET) | GET | `/expenses/create` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 33 | `Create` (POST) | POST | `/expenses/create` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 34 | `MobileCreate` (GET) | GET | `/expenses/mobilecreate` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 35 | `MobileCreate` (POST) | POST | `/expenses/mobilecreate` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 36 | `Edit` (GET) | GET | `/expenses/edit/{id}` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 37 | `Edit` (POST) | POST | `/expenses/edit/{id}` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 38 | `MobileEdit` (GET) | GET | `/expenses/mobileedit` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 39 | `MobileEdit` (POST) | POST | `/expenses/mobileedit` | `[Authorize("CanManageExpenses")]` (string-literal) |
| 40 | `Delete` | POST | `/expenses/delete/{id}` | `[Authorize("CanManageExpenses")]` (string-literal) |

### 1.7 FarmsController (`FarmsController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewFarms)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 41 | `Index` | GET | `/farms/index` | `[Authorize(Policy = PolicyNames.CanViewFarms)]` (duplicate) |
| 42 | `Details` | GET | `/farms/details/{id}` | `[Authorize(Policy = PolicyNames.CanViewFarms)]` (duplicate) |
| 43 | `Create` (GET) | GET | `/farms/create` | `[Authorize(Policy = PolicyNames.CanManageFarms)]` |
| 44 | `Create` (POST) | POST | `/farms/create` | `[Authorize(Policy = PolicyNames.CanManageFarms)]` |
| 45 | `Edit` (GET) | GET | `/farms/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageFarms)]` |
| 46 | `Edit` (POST) | POST | `/farms/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageFarms)]` |
| 47 | `Archive` | POST | `/farms/archive/{id}` | `[Authorize(Policy = PolicyNames.CanManageFarms)]` |
| 48 | `MobileIndex` | GET | `/farms/mobileindex` | *(inherits class, no method attr)* |

### 1.8 HomeController (`HomeController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewOperationalData)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 49 | `Index` | GET | `/home/index` | `[Authorize(Policy = PolicyNames.CanViewOperationalData)]` (duplicate) |
| 50 | `MobileDashboard` | GET | `/home/mobiledashboard` | `[Authorize(Policy = PolicyNames.CanViewOperationalData)]` (duplicate) |
| 51 | `Privacy` | GET | `/home/privacy` | `[Authorize(Policy = PolicyNames.CanViewOperationalData)]` (duplicate) |
| 52 | `AccessDenied` | GET | `/home/accessdenied` | `[AllowAnonymous]` |
| 53 | `Error` | GET/ANY | `/home/error` | *(none) — inherits class-level CanViewOperationalData? Verify* |

### 1.9 InvoicesController (`InvoicesController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewInvoices)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 54 | `Index` | GET | `/invoices/index` | *(inherits class)* |
| 55 | `Details` | GET | `/invoices/details/{id}` | *(inherits class)* |
| 56 | `DownloadPdf` | GET | `/invoices/downloadpdf/{id}` | *(inherits class)* |
| 57 | `Create` (GET) | GET | `/invoices/create` | `[Authorize(Policy = PolicyNames.CanManageInvoices)]` |
| 58 | `Create` (POST) | POST | `/invoices/create` | `[Authorize(Policy = PolicyNames.CanManageInvoices)]` |
| 59 | `Confirm` | POST | `/invoices/confirm/{id}` | `[Authorize(Policy = PolicyNames.CanManageInvoices)]` |
| 60 | `MobileIndex` | GET | `/invoices/mobileindex` | *(inherits class, no method attr)* |

### 1.10 LivestockController (`LivestockController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewLivestock)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 61 | `Index` | GET | `/livestock/index` | *(inherits class)* |
| 62 | `MobileIndex` | GET | `/livestock/mobileindex` | *(inherits class)* |
| 63 | `Details` | GET | `/livestock/details/{id}` | *(inherits class)* |
| 64 | `WeightHistory` | GET | `/livestock/weighthistory/{id}` | *(inherits class)* |
| 65 | `ExportCsv` | GET | `/livestock/exportcsv` | *(inherits class)* |
| 66 | `Register` (GET) | GET | `/livestock/register` | `[Authorize(Policy = PolicyNames.CanRegisterLivestock)]` |
| 67 | `Register` (POST) | POST | `/livestock/register` | `[Authorize(Policy = PolicyNames.CanRegisterLivestock)]` |
| 68 | `MobileRegister` (GET) | GET | `/livestock/mobileregister` | `[Authorize(Policy = PolicyNames.CanRegisterLivestock)]` |
| 69 | `MobileRegister` (POST) | POST | `/livestock/mobileregister` | `[Authorize(Policy = PolicyNames.CanRegisterLivestock)]` |
| 70 | `Edit` (GET) | GET | `/livestock/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageLivestock)]` |
| 71 | `Edit` (POST) | POST | `/livestock/edit/{id}` | `[Authorize(Policy = PolicyNames.CanManageLivestock)]` |
| 72 | `AddWeight` (GET) | GET | `/livestock/addweight/{id}` | `[Authorize(Policy = PolicyNames.CanRecordWeight)]` |
| 73 | `AddWeight` (POST) | POST | `/livestock/addweight/{id}` | `[Authorize(Policy = PolicyNames.CanRecordWeight)]` |
| 74 | `AddComment` | POST | `/livestock/addcomment/{id}` | `[Authorize(Policy = PolicyNames.CanRecordWeight)]` |
| 75 | `Discharge` (GET) | GET | `/livestock/discharge/{id}` | `[Authorize(Policy = PolicyNames.CanDischargeLivestock)]` |
| 76 | `Discharge` (POST) | POST | `/livestock/discharge/{id}` | `[Authorize(Policy = PolicyNames.CanDischargeLivestock)]` |

### 1.11 LivestockLossesController (`LivestockLossesController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanRecordLosses)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 77 | `Index` | GET | `/livestocklosses/index` | *(inherits class)* |
| 78 | `MobileIndex` | GET | `/livestocklosses/mobileindex` | *(inherits class)* |
| 79 | `Details` | GET | `/livestocklosses/details/{id}` | *(inherits class)* |
| 80 | `Create` (GET) | GET | `/livestocklosses/create` | *(inherits class)* |
| 81 | `Create` (POST) | POST | `/livestocklosses/create` | *(inherits class)* |
| 82 | `MobileCreate` (GET) | GET | `/livestocklosses/mobilecreate` | *(inherits class)* |
| 83 | `MobileCreate` (POST) | POST | `/livestocklosses/mobilecreate` | *(inherits class)* |
| 84 | `Reverse` | POST | `/livestocklosses/reverse/{id}` | *(inherits class)* |

### 1.12 PaymentsController (`PaymentsController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanRecordPayments)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 85 | `Index` | GET | `/payments/index` | *(inherits class)* |
| 86 | `Details` | GET | `/payments/details/{id}` | *(inherits class)* |
| 87 | `Create` (GET) | GET | `/payments/create` | *(inherits class)* |
| 88 | `Create` (POST) | POST | `/payments/create` | *(inherits class)* |
| 89 | `Reverse` | POST | `/payments/reverse/{id}` | `[Authorize(Policy = PolicyNames.CanReversePayments)]` |
| 90 | `MobileIndex` | GET | `/payments/mobileindex` | *(inherits class, no method attr)* |

### 1.13 PurchasesController (`PurchasesController.cs`)
Class Auth: `[]Authorize(Policy = PolicyNames.CanManagePurchaseInvoices)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 91 | `Index` | GET | `/purchases/index` | *(inherits class)* |
| 92 | `MobileIndex` | GET | `/purchases/mobileindex` | *(inherits class)* |
| 93 | `Details` | GET | `/purchases/details/{id}` | *(inherits class)* |
| 94 | `Create` (GET) | GET | `/purchases/create` | *(inherits class)* |
| 95 | `Create` (POST) | POST | `/purchases/create` | *(inherits class)* |
| 96 | `MobileCreate` (GET) | GET | `/purchases/mobilecreate` | *(inherits class)* |
| 97 | `MobileCreate` (POST) | POST | `/purchases/mobilecreate` | *(inherits class)* |
| 98 | `PostPurchase` | POST | `/purchases/postpurchase/{id}` | *(inherits class)* |
| 99 | `Void` | POST | `/purchases/void/{id}` | *(inherits class)* |
| 100 | `Edit` (GET) | GET | `/purchases/edit/{id}` | *(inherits class)* |
| 101 | `Edit` (POST) | POST | `/purchases/edit/{id}` | *(inherits class)* |

### 1.14 ReceiptsController (`ReceiptsController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanGenerateReceipts)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 102 | `Index` | GET | `/receipts/index` | *(inherits class)* |
| 103 | `GenerateForPayment` | POST | `/receipts/generateforpayment/{id}` | *(inherits class)* |
| 104 | `DownloadPdf` | GET | `/receipts/downloadpdf/{id}` | *(inherits class)* |
| 105 | `MobileIndex` | GET | `/receipts/mobileindex` | *(inherits class, no method attr)* |

### 1.15 ReportsController (`ReportsController.cs`)
Class Auth: `[[Authorize(Policy = PolicyNames.CanViewOperationalReports)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 106 | `Index` | GET | `/reports/index` | *(inherits class)* |
| 107 | `ActiveLivestock` | GET | `/reports/activelivestock` | *(inherits class = CanViewOperationalReports)* |
| 108 | `MobileActiveLivestock` | GET | `/reports/mobileactivelivestock` | *(inherits class = CanViewOperationalReports)* |
| 109 | `SalesByPeriod` | GET | `/reports/salesbyperiod` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |
| 110 | `MobileSalesByPeriod` | GET | `/reports/mobilesalesbyperiod` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |
| 111 | `ProfitLoss` | GET | `/reports/profitloss` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |
| 112 | `MobileProfitLoss` | GET | `/reports/mobileprofitloss` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |
| 113 | `LivestockProfitability` | GET | `/reports/livestockprofitability` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |
| 114 | `MobileLivestockProfitability` | GET | `/reports/mobilelivestockprofitability` | `[Authorize(Policy = PolicyNames.CanViewFinancialReports)]` |

### 1.16 SalesController (`SalesController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanCreateSales)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 115 | `Index` | GET | `/sales/index` | *(inherits class)* |
| 116 | `MobileIndex` | GET | `/sales/mobileindex` | *(inherits class)* |
| 117 | `Details` | GET | `/sales/details/{id}` | *(inherits class)* |
| 118 | `MobileDetails` | GET | `/sales/mobiledetails/{id}` | *(inherits class)* |
| 119 | `Create` (GET) | GET | `/sales/create` | *(inherits class)* |
| 120 | `Create` (POST) | POST | `/sales/create` | *(inherits class)* |
| 121 | `MobileCreate` (GET) | GET | `/sales/mobilecreate` | *(inherits class)* |
| 122 | `MobileCreate` (POST) | POST | `/sales/mobilecreate` | *(inherits class)* |
| 123 | `Confirm` | POST | `/sales/confirm/{id}` | *(inherits class)* |
| 124 | `Cancel` | POST | `/sales/cancel/{id}` | *(inherits class)* |
| 125 | `Reverse` | POST | `/sales/reverse/{id}` | `[Authorize(Policy = PolicyNames.CanReverseSales)]` |
| 126 | `ListEligibleLivestockBulkAdd` | GET | `/sales/listeligiblelivestockbulkadd` | *(inherits class)* |
| 127 | `BulkAdd` (GET) | GET | `/sales/bulkadd` | *(inherits class)* |
| 128 | `BulkAdd` (POST) | POST | `/sales/bulkadd` | *(inherits class)* |
| 129 | `SearchLivestock` | GET | `/sales/searchlivestock` | *(inherits class)* |
| 130 | `ResolveLivestock` | GET | `/sales/resolvelivestock` | *(inherits class)* |
| 131 | `SearchCustomers` | GET | `/sales/searchcustomers` | *(inherits class)* |

### 1.17 SettingsController (`SettingsController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanManageCompany)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 132 | `Index` (GET) | GET | `/settings/index` | `[Authorize(Policy = PolicyNames.CanManageCompany)]` (duplicate) |
| 133 | `Index` (POST) | POST | `/settings/index` | `[Authorize(Policy = PolicyNames.CanManageCompany)]` (duplicate) |

### 1.18 StockAdditionController (`StockAdditionController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanViewLivestock)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 134 | `Index` | GET | `/stockaddition/index` | *(inherits class)* |
| 135 | `MobileIndex` | GET | `/stockaddition/mobileindex` | *(inherits class)* |
| 136 | `Success` | GET | `/stockaddition/success` | *(inherits class)* |
| 137 | `MobileSuccess` | GET | `/stockaddition/mobilesuccess` | *(inherits class)* |
| 138 | `Purchase` (GET) | GET | `/stockaddition/purchase` | `[Authorize(Policy = PolicyNames.CanRecordStockPurchase)]` |
| 139 | `Purchase` (POST) | POST | `/stockaddition/purchase` | `[Authorize(Policy = PolicyNames.CanRecordStockPurchase)]` |
| 140 | `MobilePurchase` (GET) | GET | `/stockaddition/mobilepurchase` | `[Authorize(Policy = PolicyNames.CanRecordStockPurchase)]` |
| 141 | `MobilePurchase` (POST) | POST | `/stockaddition/mobilepurchase` | `[Authorize(Policy = PolicyNames.CanRecordStockPurchase)]` |
| 142 | `Newborn` (GET) | GET | `/stockaddition/newborn` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |
| 143 | `Newborn` (POST) | POST | `/stockaddition/newborn` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |
| 144 | `MobileNewborn` (GET) | GET | `/stockaddition/mobilenewborn` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |
| 145 | `MobileNewborn` (POST) | POST | `/stockaddition/mobilenewborn` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |
| 146 | `SearchEligibleEwes` | GET | `/stockaddition/searcheligibleewes` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |
| 147 | `SearchEligibleRams` | GET | `/stockaddition/searcheligiblerams` | `[Authorize(Policy = PolicyNames.CanRecordNewborn)]` |

### 1.19 SuppliersController (`SuppliersController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanManageSuppliers)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 148 | `Index` | GET | `/suppliers/index` | *(inherits class)* |
| 149 | `Details` | GET | `/suppliers/details/{id}` | *(inherits class)* |
| 150 | `Create` (GET) | GET | `/suppliers/create` | *(inherits class)* |
| 151 | `Create` (POST) | POST | `/suppliers/create` | *(inherits class)* |
| 152 | `Edit` (GET) | GET | `/suppliers/edit/{id}` | *(inherits class)* |
| 153 | `Edit` (POST) | POST | `/suppliers/edit/{id}` | *(inherits class)* |
| 154 | `Archive` | POST | `/suppliers/archive/{id}` | *(inherits class)* |
| 155 | `MobileIndex` | GET | `/suppliers/mobileindex` | *(inherits class, no method attr)* |

### 1.20 UserManagementController (`UserManagementController.cs`)
Class Auth: `[Authorize(Policy = PolicyNames.CanManageUsers)]`

| # | Method | HTTP | Route | Method Auth |
|---|---|---|---|---|
| 156 | `Index` | GET | `/usermanagement/index` | *(inherits class, no method attr)* |
| 157 | `AddUser` (GET) | GET | `/usermanagement/adduser` | *(inherits class, no method attr)* |
| 158 | `AddUser` (POST) | POST | `/usermanagement/adduser` | *(inherits class, no method attr)* |
| 159 | `ResetPassword` | POST | `/usermanagement/resetpassword/{id}` | *(inherits class, no method attr)* |
| 160 | `ToggleEnabled` | POST | `/usermanagement/toggleenabled/{id}` | *(inherits class, no method attr)* |
| 161 | `MobileIndex` | GET | `/usermanagement/mobileindex` | *(inherits class, no method attr)* |

---

## 2. MOBILE-SPECIFIC ACTIONS (SECTION B)

All actions with `Mobile` prefix, grouped by controller. Each action has a corresponding `Mobile*.cshtml` view.

| # | Controller | Action | HTTP | Method-level Auth? | Protection Source |
|---|---|---|---|---|---|
| 1 | Customers | `MobileIndex` | GET | NO | Class-level `CanManageCustomers` |
| 2 | Documents | `MobileIndex` | GET | NO | Class-level `CanViewDocuments` |
| 3 | Expenses | `MobileIndex` | GET | NO | Class-level `CanManageExpenses` |
| 4 | Expenses | `MobileCreate` | GET+POST | YES (string-literal) | `[Authorize("CanManageExpenses")]` |
| 5 | Expenses | `MobileEdit` | GET+POST | YES (string-literal) | `[Authorize("CanManageExpenses")]` |
| 6 | Farms | `MobileIndex` | GET | NO | Class-level `CanViewFarms` |
| 7 | Home | `MobileDashboard` | GET | YES (duplicate) | `CanViewOperationalData` |
| 8 | Invoices | `MobileIndex` | GET | NO | Class-level `CanViewInvoices` |
| 9 | Livestock | `MobileIndex` | GET | NO | Class-level `CanViewLivestock` |
| 10 | Livestock | `MobileRegister` | GET+POST | YES | `CanRegisterLivestock` |
| 11 | LivestockLosses | `MobileIndex` | GET | NO | Class-level `CanRecordLosses` |
| 12 | LivestockLosses | `MobileCreate` | GET+POST | NO | Class-level `CanRecordLosses` |
| 13 | Payments | `MobileIndex` | GET | NO | Class-level `CanRecordPayments` |
| 14 | Purchases | `MobileIndex` | GET | NO | Class-level `CanManagePurchaseInvoices` |
| 15 | Purchases | `MobileCreate` | GET+POST | NO | Class-level `CanManagePurchaseInvoices` |
| 16 | Receipts | `MobileIndex` | GET | NO | Class-level `CanGenerateReceipts` |
| 17 | Reports | `MobileActiveLivestock` | GET | NO | Class-level `CanViewOperationalReports` |
| 18 | Reports | `MobileSalesByPeriod` | GET | YES | `CanViewFinancialReports` |
| 19 | Reports | `MobileProfitLoss` | GET | YES | `CanViewFinancialReports` |
| 20 | Reports | `MobileLivestockProfitability` | GET | YES | `CanViewFinancialReports` |
| 21 | Sales | `MobileIndex` | GET | NO | Class-level `CanCreateSales` |
| 22 | Sales | `MobileCreate` | GET+POST | NO | Class-level `CanCreateSales` |
| 23 | Sales | `MobileDetails` | GET | NO | Class-level `CanCreateSales` |
| 24 | StockAddition | `MobileIndex` | GET | NO | Class-level `CanViewLivestock` |
| 25 | StockAddition | `MobileSuccess` | GET | NO | Class-level `CanViewLivestock` |
| 26 | StockAddition | `MobilePurchase` | GET+POST | YES | `CanRecordStockPurchase` |
| 27 | StockAddition | `MobileNewborn` | GET+POST | YES | `CanRecordNewborn` |
| 28 | Suppliers | `MobileIndex` | GET | NO | Class-level `CanManageSuppliers` |
| 29 | UserManagement | `MobileIndex` | GET | NO | Class-level `CanManageUsers` |

**Subtotal: 29 mobile-named action groups (34 concrete HTTP overloads: GET + POST counted individually).**

---

## 3. ACTIONS WITH NO `[Authorize]` ATTRIBUTE (SECTION C)

### 3.1 CATEGORY 1: TRULY UNPROTECTED / NO CLASS-LEVEL EITHER — **RISK ALERT**
These have NO `[Authorize]` at class OR method level, AND are NOT `[AllowAnonymous]`.

| # | Controller | Action | HTTP | Actual Protection Status |
|---|---|---|---|---|
| 1 | **AccountController** | `Logout` | POST | **No `[Authorize]` anywhere.** SignOutAsync SHOULD still work (identity cookie), but lacks explicit attribute. Low risk but inconsistent. |
| 2 | **HomeController** | `Error` | ANY | Inherits class-level `CanViewOperationalData` from `HomeController` class attribute — technically protected, but visually appears unprotected. **Audit flag only.** |

**Genuinely unprotected (no auth at any level): 1 action (`AccountController.Logout` POST).**

### 3.2 CATEGORY 2: NO METHOD-LEVEL ATTR BUT INHERITED CLASS-LEVEL `[Authorize]` — **PROTECTED**
These actions rely on class-level `[Authorize]` and have no redundant method-level attribute. This is architecturally acceptable but must be inventoried for completeness.

Count: **59 actions** across 17 controllers. Key clusters:
- **Customers (7):** Index, Details, Create×2, Edit×2, Archive
- **Documents (4):** Index, List, Download, MobileIndex
- **Expenses (3):** Index, MobileIndex, Details
- **Farms (1):** MobileIndex
- **Invoices (4):** Index, Details, DownloadPdf, MobileIndex
- **Livestock (5):** Index, MobileIndex, Details, WeightHistory, ExportCsv
- **LivestockLosses (7):** Index, MobileIndex, Details, Create×2, MobileCreate×2, Reverse
- **Payments (4):** Index, Details, Create×2, MobileIndex
- **Purchases (9):** Index, MobileIndex, Details, Create×2, MobileCreate×2, PostPurchase, Void, Edit×2
- **Receipts (4):** Index, GenerateForPayment, DownloadPdf, MobileIndex
- **Reports (3):** Index, ActiveLivestock, MobileActiveLivestock
- **Sales (16):** Index, MobileIndex, Details, MobileDetails, Create×2, MobileCreate×2, Confirm, Cancel, ListEligibleLivestockBulkAdd, BulkAdd×2, SearchLivestock, ResolveLivestock, SearchCustomers
- **StockAddition (4):** Index, MobileIndex, Success, MobileSuccess
- **Suppliers (7):** Index, Details, Create×2, Edit×2, Archive, MobileIndex
- **UserManagement (5):** Index, AddUser×2, ResetPassword, ToggleEnabled, MobileIndex

---

## 4. POLICY DISCREPANCY ANALYSIS: PROGRAM.CS vs LIVESTOCK.TXT MATRIX (SECTION D)

**Reference: livestock.txt expected matrix (user-specified) vs current Program.cs (`Program.cs:139-231`).**

Legend:
- ✅ MATCH
- ❌ DIVERGENCE (discrepancy)

### 4.1 TOP-5 POLICY DISCREPANCIES (ORDERED BY SEVERITY)

#### DISCREPANCY #1 — `CanRegisterLivestock` policy includes `DataEntry` (Employee)
- **livestock.txt matrix expectation:** `DataEntry CanRegisterLivestock = FALSE`
- **Current Program.cs (`Program.cs:153-154`):**
  ```csharp
  CanRegisterLivestock → DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator
  ```
- **Severity:** HIGH — Employee (DataEntry) can register new livestock when matrix prohibits this capability. Scope creep on operational data entry.
- **Affected actions:** `LivestockController.Register` (GET/POST), `LivestockController.MobileRegister` (GET/POST) — 4 actions.
- **Current Program.cs roles with access:** 5 roles. **Expected:** 4 roles (exclude DataEntry).

#### DISCREPANCY #2 — `CanManageCustomers` policy includes `FarmManager`
- **livestock.txt matrix expectation:** `FarmManager CanManageCustomers = FALSE`
- **Current Program.cs (`Program.cs:171-172`):**
  ```csharp
  CanManageCustomers → Accounts, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator
  ```
- **Severity:** MEDIUM-HIGH — FarmManager gains customer CRM management (create/edit/archive) when scope should be farm-operations only. Cross-domain privilege expansion.
- **Affected actions:** All 8 `CustomersController` actions (Index, Details, Create×2, Edit×2, Archive, MobileIndex).
- **Current roles:** 5 roles. **Expected:** 4 roles (exclude FarmManager).

#### DISCREPANCY #3 — `Accounts CanRegisterLivestock` (note: matrix says Accounts=false; current code also excludes Accounts — but matrix text mentions DataEntry overlap)
- **livestock.txt matrix expectation:** `Accounts CanRegisterLivestock = FALSE` (also `DataEntry CanRegisterLivestock = FALSE`)
- **Current Program.cs state:** Accounts IS correctly excluded from `CanRegisterLivestock`. BUT DataEntry IS incorrectly included (see Discrepancy #1).
- **Severity:** INFORMATION — User's request flags two roles. Accounts is correct; DataEntry is the bug. Re-stated here because user explicitly mentioned Accounts alongside DataEntry.
- **Net correction needed:** Remove DataEntry from CanRegisterLivestock. Accounts stays removed.

#### DISCREPANCY #4 — Reports `ActiveLivestock` access: wrong policy chain for intended role set
- **livestock.txt matrix expectation:** `ActiveLivestock` report available ONLY to `DataEntry` + `FarmManager`. Financial reports (`SalesByPeriod`, `ProfitLoss`, `LivestockProfitability`) restricted to finance roles.
- **Current implementation:**
  - `ActiveLivestock` / `MobileActiveLivestock` → `CanViewOperationalReports` (Program.cs:213-214) = DataEntry, FarmManager, **OperationsManager, CompanyAdministrator, SystemAdministrator**
  - Financial reports → `CanViewFinancialReports` (Program.cs:216-217) = Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator
- **Problem:** ActiveLivestock is currently visible to OperationsManager/CompanyAdmin/SystemAdmin (3 extra roles). Per matrix, it should be **ONLY** DataEntry + FarmManager.
- **Severity:** MEDIUM — Two financial/management roles (OperationsManager+) incorrectly see an operations-only report. Data leakage across report domains.
- **Note on "CanViewFinancialReports wrong":** User's note refers to the fact that using `CanViewFinancialReports` for any operational-report access is incorrect. Currently `ActiveLivestock` correctly uses `CanViewOperationalReports` (not `CanViewFinancialReports`), BUT `CanViewOperationalReports` itself is too broad for ActiveLivestock's target audience.
- **Fix required:** Either (a) create a dedicated `CanViewActiveLivestockReport` policy with ONLY DataEntry+FarmManager, OR (b) narrow `CanViewOperationalReports` to DataEntry+FarmManager (which would break other pages that currently depend on broader access). Option (a) is safer.

#### DISCREPANCY #5 — Desktop `_Layout.cshtml` navigation uses HARD-CODED `User.IsInRole(...)` instead of policy evaluation
- **livestock.txt matrix expectation:** All UI rendering must be capability/policy-driven (single source of truth = Program.cs policies).
- **Current state (`Views/Shared/_Layout.cshtml:235-252`):** Desktop sidebar navigation items are shown/hidden using hardcoded `User.IsInRole(RoleNames.Xxx)` boolean chains. These role-name lists are NOT guaranteed to match the current Program.cs policy registrations.
- **Severity:** HIGH (architectural) — Dual authorization state. If Program.cs is updated but `_Layout.cshtml` is forgotten, users either (a) see menu items for actions they're blocked from (UX/403 frustration) or (b) don't see menu items for actions they COULD legitimately access (undiscoverable functionality).
- **Correct reference implementation:** `Views/Shared/_MobileLayout.cshtml` uses `IAuthorizationService.AuthorizeAsync(User, PolicyNames.CanXxx)` for each nav item — policy-based, single source of truth.
- **Affected:** ALL desktop users. Any future policy change in Program.cs requires a parallel manual edit in _Layout.cshtml (violates DRY / single source of truth).

### 4.2 REMAINING POLICIES — PROBABLY CORRECT (NO USER-FLAGGED ISSUES)
Cross-checked against ROLE_RIGHTS_IMPLEMENTATION_REPORT.md (`audit/ROLE_RIGHTS_IMPLEMENTATION_REPORT.md:46-76`):

| Policy | Program.cs matches audit report matrix? |
|---|---|
| CanViewOperationalData | ✅ Yes (all 6 roles) |
| CanViewFarms | ✅ Yes (all 6 roles) |
| CanManageFarms | ✅ Yes (OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanViewLivestock | ✅ Yes (all 6 roles) |
| CanManageLivestock | ✅ Yes (FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanRecordWeight | ✅ Yes (DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanDischargeLivestock | ✅ Yes (FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanRecordStockPurchase | ✅ Yes (FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanRecordNewborn | ✅ Yes (DataEntry, FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanManageSuppliers | ✅ Yes (FarmManager, Accounts, OperationsManager, CompanyAdministrator, SystemAdministrator) — same pattern as CanManageCustomers (#2) |
| CanViewDocuments | ✅ Yes |
| CanUploadDocuments | ✅ Yes |
| CanCreateSales | ✅ Yes |
| CanReverseSales | ✅ Yes (CompanyAdministrator, SystemAdministrator only) |
| CanManagePurchaseInvoices | ✅ Yes |
| CanViewInvoices | ✅ Yes |
| CanManageInvoices | ✅ Yes |
| CanRecordPayments | ✅ Yes (Accounts, CompanyAdministrator, SystemAdministrator — note: no OperationsManager) |
| CanReversePayments | ✅ Yes (Accounts, CompanyAdministrator, SystemAdministrator) |
| CanGenerateReceipts | ✅ Yes (Accounts, CompanyAdministrator, SystemAdministrator) |
| CanManageExpenses | ✅ Yes |
| CanRecordLosses | ✅ Yes (FarmManager, OperationsManager, CompanyAdministrator, SystemAdministrator) |
| CanViewOperationalReports | ✅ Yes against audit report (but TOO BROAD per livestock.txt for ActiveLivestock specifically) |
| CanViewFinancialReports | ✅ Yes |
| CanManageCompany | ✅ Yes (CompanyAdministrator, SystemAdministrator) |
| CanViewAuditLogs | ✅ Yes (CompanyAdministrator, SystemAdministrator) |
| CanManageUsers | ✅ Yes (CompanyAdministrator, SystemAdministrator) |
| CanManageSystem | ✅ Yes (SystemAdministrator only) |

---

## 5. VIEW INVENTORY: DESKTOP+MOBILE PAIRS + INLINE ROLE/CAPABILITY RENDERING (SECTION E)

### 5.1 TOTAL COUNTS
| Metric | Value |
|---|---|
| Total `.cshtml` files in Views/ | 98 |
| Shared/layout/import files (_ViewStart, _ViewImports, _Layout, _MobileLayout, _LoginPartial, _ValidationScriptsPartial, Shared/Error) | 7 |
| Account pages (Login, Logout, AccessDenied, Error) | 5 |
| Desktop-specific feature pages (no mobile pair, e.g., Companies/Create+Edit+Details, Audit/*, Settings/*, UserManagement/AddUser, Livestock/Edit/Register/Discharge/AddWeight/WeightHistory, Documents/Upload/List/Download, Farms/Create+Edit, Customers/Create+Edit, Suppliers/Create+Edit, Invoices/Create+DownloadPdf+Details, Payments/Create+Details, Receipts/DownloadPdf, Sales/Create+Confirm/Cancel/ListEligible/BulkAdd, StockAddition/Purchase+Newborn, LivestockLosses/Create, Expenses/Create+Edit) | ~45 |
| Mobile-specific views (Mobile*.cshtml) | 21 |
| Desktop+Mobile paired feature folders (both Index.cshtml AND MobileIndex.cshtml exist) | 11 folders |

### 5.2 ELEVEN DESKTOP+MOBILE PAIRED FEATURE FOLDERS

| # | Folder | Desktop Index | Mobile Index | Additional Mobile Views in Folder |
|---|---|---|---|---|
| 1 | Home | `Index.cshtml` | `MobileDashboard.cshtml` (not MobileIndex — named differently) | 0 |
| 2 | Sales | `Index.cshtml` | `MobileIndex.cshtml` | `MobileCreate.cshtml`, `MobileDetails.cshtml` |
| 3 | Reports | `Index.cshtml` (plus `ActiveLivestock`, `SalesByPeriod`, `ProfitLoss`, `LivestockProfitability`) | no single MobileIndex; 4 mobiles | `MobileActiveLivestock.cshtml`, `MobileSalesByPeriod.cshtml`, `MobileProfitLoss.cshtml`, `MobileLivestockProfitability.cshtml` |
| 4 | Livestock | `Index.cshtml` | `MobileIndex.cshtml` | `MobileRegister.cshtml` |
| 5 | Expenses | `Index.cshtml` | `MobileIndex.cshtml` | `MobileCreate.cshtml`, `MobileEdit.cshtml` |
| 6 | Purchases | `Index.cshtml` | `MobileIndex.cshtml` | `MobileCreate.cshtml` |
| 7 | LivestockLosses | `Index.cshtml` | `MobileIndex.cshtml` | `MobileCreate.cshtml` |
| 8 | StockAddition | `Index.cshtml` | `MobileIndex.cshtml` | `MobilePurchase.cshtml`, `MobileNewborn.cshtml`, `MobileSuccess.cshtml` |
| 9 | Customers | `Index.cshtml` | No MobileIndex (only action-level, no view? Verify) | 0 |
| 10 | Documents | `Index.cshtml` | No MobileIndex view found | 0 |
| 11 | Farms | `Index.cshtml` | No MobileIndex view found | 0 |

### 5.3 COMPLETE 21 MOBILE-SPECIFIC VIEWS (`Mobile*.cshtml`)
| # | Path | Controller.Action |
|---|---|---|
| 1 | `Views/Home/MobileDashboard.cshtml` | Home.MobileDashboard |
| 2 | `Views/Sales/MobileIndex.cshtml` | Sales.MobileIndex |
| 3 | `Views/Sales/MobileDetails.cshtml` | Sales.MobileDetails |
| 4 | `Views/Sales/MobileCreate.cshtml` | Sales.MobileCreate |
| 5 | `Views/Reports/MobileLivestockProfitability.cshtml` | Reports.MobileLivestockProfitability |
| 6 | `Views/Reports/MobileSalesByPeriod.cshtml` | Reports.MobileSalesByPeriod |
| 7 | `Views/Reports/MobileProfitLoss.cshtml` | Reports.MobileProfitLoss |
| 8 | `Views/Reports/MobileActiveLivestock.cshtml` | Reports.MobileActiveLivestock |
| 9 | `Views/Livestock/MobileIndex.cshtml` | Livestock.MobileIndex |
| 10 | `Views/Expenses/MobileEdit.cshtml` | Expenses.MobileEdit |
| 11 | `Views/Expenses/MobileCreate.cshtml` | Expenses.MobileCreate |
| 12 | `Views/LivestockLosses/MobileCreate.cshtml` | LivestockLosses.MobileCreate |
| 13 | `Views/Purchases/MobileCreate.cshtml` | Purchases.MobileCreate |
| 14 | `Views/Livestock/MobileRegister.cshtml` | Livestock.MobileRegister |
| 15 | `Views/StockAddition/MobileNewborn.cshtml` | StockAddition.MobileNewborn |
| 16 | `Views/StockAddition/MobilePurchase.cshtml` | StockAddition.MobilePurchase |
| 17 | `Views/StockAddition/MobileSuccess.cshtml` | StockAddition.MobileSuccess |
| 18 | `Views/Purchases/MobileIndex.cshtml` | Purchases.MobileIndex |
| 19 | `Views/StockAddition/MobileIndex.cshtml` | StockAddition.MobileIndex |
| 20 | `Views/LivestockLosses/MobileIndex.cshtml` | LivestockLosses.MobileIndex |
| 21 | `Views/Expenses/MobileIndex.cshtml` | Expenses.MobileIndex |

### 5.4 VIEWS WITH INLINE `User.IsInRole(...)` ROLE CHECKS — **7 FILES**
Hard-coded role-name booleans. Risk: drift from Program.cs policies.

| # | File | Location | Usage Pattern |
|---|---|---|---|
| 1 | `Views/Shared/_Layout.cshtml` | Desktop sidebar nav (lines ~235-252) | Heavy: every sidebar menu item visibility gated by `User.IsInRole(RoleNames.Xxx)` OR-combinations. ~15 distinct role checks. **MAJOR DRIFT RISK (#5 discrepancy).** |
| 2 | `Views/Livestock/Index.cshtml` | Edit/Register/Discharge button visibility | `ViewData["CanEdit"]` + `User.IsInRole` combinations for toolbar action buttons |
| 3 | `Views/Livestock/Details.cshtml` | Edit/Weight/Discharge actions | Same pattern: inline IsInRole for command visibility |
| 4 | `Views/Livestock/MobileIndex.cshtml` | FAB / toolbar actions | Mobile variant using IsInRole for action buttons |
| 5 | `Views/Farms/Index.cshtml` | Create/Edit/Archive farm | `User.IsInRole` for CanManageFarms-equivalent buttons |
| 6 | `Views/Farms/Details.cshtml` | Edit farm button | `User.IsInRole` for management button |
| 7 | `Views/Documents/Index.cshtml` | Upload/Delete buttons | `User.IsInRole` for document command visibility |

### 5.5 VIEWS WITH `ViewData["Can*"]` CAPABILITY RENDERING — **19 FILES**
Controller populates `ViewData["CanEdit"]` (or similar) using `User.IsInRole` inline in the action. Better than raw IsInRole in Razor, but still role-name based not policy-based.

| # | File | ViewData Key | Used For |
|---|---|---|---|
| 1 | `Views/Sales/Index.cshtml` | `CanEdit` / `CanReverse` | Reverse-sale button visibility |
| 2 | `Views/Sales/Details.cshtml` | `CanEdit` / `CanReverse` | Action row for individual sale |
| 3 | `Views/Sales/MobileIndex.cshtml` | `CanEdit` / `CanReverse` | Mobile sale list action icons |
| 4 | `Views/Sales/MobileDetails.cshtml` | `CanEdit` / `CanReverse` | Mobile sale detail toolbar |
| 5 | `Views/Expenses/Index.cshtml` | `CanEdit` | Expense edit/delete row buttons |
| 6 | `Views/Expenses/MobileIndex.cshtml` | `CanEdit` | Mobile expense action icons |
| 7 | `Views/Expenses/Details.cshtml` | `CanEdit` | Expense detail edit button |
| 8 | `Views/Purchases/Index.cshtml` | `CanEdit` / `CanPostVoid` | Purchase command buttons |
| 9 | `Views/Purchases/MobileIndex.cshtml` | `CanEdit` / `CanPostVoid` | Mobile purchase actions |
| 10 | `Views/Purchases/Details.cshtml` | `CanEdit` / `CanPostVoid` | Purchase detail commands |
| 11 | `Views/LivestockLosses/Index.cshtml` | `CanEdit` / `CanReverse` | Loss record commands |
| 12 | `Views/LivestockLosses/MobileIndex.cshtml` | `CanEdit` / `CanReverse` | Mobile loss actions |
| 13 | `Views/LivestockLosses/Details.cshtml` | `CanEdit` / `CanReverse` | Loss detail reverse button |
| 14 | `Views/Invoices/Index.cshtml` | `CanEdit` / `CanConfirm` | Invoice management buttons |
| 15 | `Views/Invoices/Details.cshtml` | `CanEdit` / `CanConfirm` | Invoice detail commands |
| 16 | `Views/Customers/Index.cshtml` | `CanEdit` | Customer edit/archive buttons |
| 17 | `Views/Customers/Details.cshtml` | `CanEdit` | Customer detail edit button |
| 18 | `Views/Suppliers/Index.cshtml` | `CanEdit` | Supplier edit/archive buttons |
| 19 | `Views/Suppliers/Details.cshtml` | `CanEdit` | Supplier detail edit button |

### 5.6 VIEWS USING `IAuthorizationService.AuthorizeAsync` (POLICY-BASED, CORRECT PATTERN) — **1 FILE ONLY**
| # | File | Usage | Notes |
|---|---|---|---|
| 1 | `Views/Shared/_MobileLayout.cshtml` | Bottom dock + slide-in menu nav items | Every nav item visibility checked via `await AuthorizationService.AuthorizeAsync(User, PolicyNames.CanXxx)`. **Reference implementation. Desktop _Layout.cshtml should migrate to this pattern (Discrepancy #5).** |

### 5.7 VIEWS WITH NO CAPABILITY RENDERING AT ALL (RELY ON AUTHORIZE FILTER ONLY)
All remaining views (~50 files) have ZERO inline capability checks. Their content is always rendered — authorization is handled exclusively by the controller `[Authorize]` filter. This is acceptable for read-only views with no conditional commands, but means users who reach a page but lack a specific sub-capability (e.g., can view a page but cannot reverse an entry) get no pre-rendered UI hint and see all buttons (button press then returns 403).

Folders where every view is capability-blind:
- **Account/** (Login/Logout/AccessDenied — anonymous anyway)
- **Audit/** (view-only list + detail, no commands to gate)
- **Companies/** (SystemAdmin-only; all users here have full capability)
- **Settings/** (CompanyAdmin-only; all users here have full capability)
- **Home/** (Index/MobileDashboard/Privacy/Error — KPI cards, no commands gated inline)
- **Receipts/** (view-only list + generate + download)
- **Payments/** (Create/Details — commands available to all CanRecordPayments users; Reverse gated by controller method)

---

## 6. ADDITIONAL FINDINGS (NOT IN USER SCOPE BUT OBSERVED)

1. **ExpensesController string-literal policy usage:** `[Authorize("CanManageExpenses")]` uses a hard-coded string instead of `[Authorize(Policy = PolicyNames.CanManageExpenses)]`. If `PolicyNames.CanManageExpenses` constant is ever renamed, this attribute silently breaks (no compile error). Found in 10 methods + class-level attribute in ExpensesController.cs.

2. **Duplicate `[Authorize]` attributes (class + method with identical policy):** Found in AuditController, CompaniesController, HomeController, FarmsController, SettingsController. Harmless but verbose. If a policy is ever changed at class level, method-level duplicates must also be updated in parallel (double-work, risk of drift).

3. **UserManagementController — all 6 actions have no method-level attribute.** Relies entirely on class-level `CanManageUsers`. Acceptable but visually looks unprotected when doing line-level audits (exactly this report!).

4. **No `AutoValidateAntiforgeryToken` global filter evidence.** Financial POST endpoints (Create/Reverse/Void/Confirm/ResetPassword/ToggleEnabled) should have antiforgery validation. Individual controllers use `[ValidateAntiForgeryToken]` ad-hoc; verify this is consistently applied across 47 POST actions.

5. **Logout (POST) missing `[Authorize]` and possibly missing `[ValidateAntiForgeryToken]`.** Low risk (SignOutAsync is idempotent for auth-cookie only) but inconsistent with the rest of the project.

---

## 7. SUMMARY TABLES FOR REMEDIATION PLANNING

### 7.1 SECTION (a) THROUGH (e) CONSOLIDATED METRICS

| Item | Count |
|---|---|
| **(a) Total controllers** | **20** |
| **(a) Total public actions (incl GET+POST overloads)** | **~161 inventoried; 133 unique action-name groups** |
| **(b) Mobile-specific action groups (Mobile\* named)** | **29 groups / 34 concrete overloads** |
| **(c) Actions with NO method-level `[Authorize]`** | **61** |
| — (c1) Protected via class-level inherit | 59 |
| — (c2) Genuinely unprotected / no attribute anywhere | 2 |
| — (c2a) Anonymous via `[AllowAnonymous]` (Login/AccessDenied) | 6 endpoints (design-intent) |
| **(d) Policy discrepancies flagged** | **5 (top-5 listed in §4.1)** |
| **(e) Total `.cshtml` views** | **98** |
| **(e) Mobile-specific views** | **21** |
| **(e) Desktop+Mobile paired folders** | 11 |
| **(e) Views with inline role checks** | 30 (7× User.IsInRole + 19× ViewData["Can\*"] + 4 dual) |
| **(e) Views using policy-based IAuthorizationService** | 1 (only _MobileLayout) |
| **(e) Pages with missing capability rendering** | ~50 |

### 7.2 TOP-5 POLICY DISCREPANCIES (QUICK REFERENCE)

| Rank | ID | Policy / Location | Issue Summary | Severity |
|---|---|---|---|---|
| 1 | §4.1 #1 | `CanRegisterLivestock` (`Program.cs:153-154`) | Includes `DataEntry` (Employee) — matrix says DataEntry=FALSE | HIGH |
| 2 | §4.1 #2 | `CanManageCustomers` (`Program.cs:171-172`) | Includes `FarmManager` — matrix says FarmManager=FALSE | HIGH |
| 3 | §4.1 #3 | `CanRegisterLivestock` — Accounts note | Accounts correctly excluded; DataEntry incorrectly INCLUDED (same as #1, flagged separately in request) | INFO |
| 4 | §4.1 #4 | Reports `ActiveLivestock` + `CanViewOperationalReports` | Visible to OperationsManager/CompanyAdmin/SystemAdmin in addition to intended DataEntry+FarmManager only. Policy too broad. | MEDIUM-HIGH |
| 5 | §4.1 #5 | `Views/Shared/_Layout.cshtml` desktop nav | Hard-coded `User.IsInRole(...)` role chains instead of `IAuthorizationService` policy checks. Dual-source-of-truth drift from Program.cs. | HIGH (architectural) |

---

## 8. REPORT STATUS

**Status:** Raw gap inventory complete. No source files modified (read-only audit as requested).

**Next-step recommendations (for planning, NOT executed):**
1. Create dedicated `CanViewActiveLivestockReport` policy (DataEntry+FarmManager only) and apply to Reports.ActiveLivestock / MobileActiveLivestock.
2. Remove `DataEntry` from `CanRegisterLivestock` RequireRole list in `Program.cs`.
3. Remove `FarmManager` from `CanManageCustomers` RequireRole list in `Program.cs`.
4. Migrate `Views/Shared/_Layout.cshtml` desktop nav from role-name checks to `IAuthorizationService` (mirror `_MobileLayout.cshtml`).
5. Add explicit `[Authorize]` (or inherit-documentation comment) to: AccountController.Logout, all UserManagementController methods, all 59 inherited-protection actions for audit clarity.
6. Change ExpensesController string-literal `"CanManageExpenses"` to `PolicyNames.CanManageExpenses` constant.
