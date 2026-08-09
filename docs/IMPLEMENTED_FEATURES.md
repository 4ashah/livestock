# IMPLEMENTED FEATURES — Proven Source-Existing Features

This document lists ONLY features PROVEN to exist in source code with file:line evidence from audit/PHASE1_SOURCE_INVENTORY.md and direct source-code verification.

---

## 1. Authoritative Ovine Livestock Codes (5 values) + PurchaseAmount Rules

### Five Ovine LivestockType enum
Source: `src/LivestockManager.Domain/Enums/LivestockType.cs:1-15`

| Code (short | Enum Name              | PurchaseAmount Rule | Source line |
|---|---|---|---|
| AH = 1 | PurchasedCastratedRam  | PurchaseAmount REQUIRED (purchased) | :5-6 |
| SU = 2 | UncastratedRam (bred)   | PurchaseAmount zero or non-null (no rule |
| SA = 3 | PurchasedEwe (purchase) | PurchaseAmount REQUIRED (purchase) |
| AD = 4 | BredCastratedRam | PurchaseAmount NOT REQUIRED (bred) |
| SD = 5 | BredEwe | PurchaseAmount NOT REQUIRED (bred) |

PurchaseAmount validation is enforced conditionally based on Livestock.Purchased vs bred classification.

---

## 2. Six Roles + Authorization Policies

### Six Roles (RoleNames.cs)
Source: `src/LivestockManager.Domain/Common/RoleNames.cs:1-13`

1. `Viewer`
2. `DataEntry`
3. `FarmManager`
4. `Accounts`
5. `CompanyAdministrator` (aliased as `Administrator`)
6. `SystemAdministrator`

### Four Named Authorization Policies (Program.cs)
Source: `src/LivestockManager.Web/Program.cs:139-159`

| Policy | Roles | Line |
|---|---|---|
| `CanViewOperationalData` | Viewer, DataEntry, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator | :139-141 |
| `CanManageLivestock` | DataEntry, FarmManager, CompanyAdministrator, SystemAdministrator | :142-144 |
| `CanViewFinancialData` | Accounts, FarmManager, CompanyAdministrator, SystemAdministrator | :157-159 |
| `CanManageFarm` (as CanManageCompany | CompanyAdministrator, SystemAdministrator (via CanManageCompany policy, line 151-153) | :151-153 |

Additional policies present: CanManageSales, CanManageAccounting, CanManageSystem (Program.cs lines 145-155.

---

## 3. Year-Scoped Sequencing (Per Company)

### EfSequenceGenerator
Source: `src/LivestockManager.Infrastructure/Services/EfSequenceGenerator.cs:118-156`
- Atomic `UPDATE OUTPUT / INSERT pattern for `SequenceCounters` table.
- Composite key: `CompanyId + SequenceName (e (e` (DocumentType) + Year (YYYY scoped per company per year.

### DocumentNumberGenerator
Source: `src/LivestockManager.Infrastructure/Services/Sequencing/DocumentNumberGenerator.cs`
- Combines: `{Prefix}-{YYYY}-{NNNNNN}` format with zero-padded sequence number.
- Year from Clock/UTCnow (from IDateTime.

---

## 4. PDF Generation (Multi-Page + Unicode Noto Font Type0 Identity-H

### FormattedPdfWriter
Source: `src/LivestockManager.Infrastructure/Services/Pdf/FormattedPdfWriter.cs`
- Multi-page page-tree explicit page breaks via content-stream operators.
- NotoSans font embedded Type0 Identity-H encoding Unicode coverage.
- Implements `IPdfGenerator` interface.

### StubPdfGenerator
Source: `audit/PHASE1_SOURCE_INVENTORY.md §C.5
- Used in testing / fallback stub generator used in non-Production environments.

### DI Registration
Source: `audit/PHASE1_SOURCE_INVENTORY.md §C.2`
- Registered in `ServiceCollectionExtensions`.

---

## 5. Protected Document Storage + 9-Extension Allow-List + Magic-Byte Validation

### ProtectedDocumentStorage
Source: `src/LivestockManager.Domain/Abstractions/IProtectedDocumentStorage.cs`
Source: `src/LivestockManager.Infrastructure/Services/Storage/ProtectedDocumentStorage.cs`
- Storage outside `wwwroot` (cross-company isolation by subdirectory.

### 9-Extension Allow-List (5 independent locations)
Source: `audit/PHASE1_SOURCE_INVENTORY.md §D.1`
- 9 extensions allowed: `.pdf, .png, .jpg, .jpeg, .doc, .docx, .xls, .xlsx, .csv`
- Validated in: ProtectedFileUploadValidator + DocumentsController upload.

### Magic-Byte Validation
Source: `audit/PHASE1_SOURCE_INVENTORY.md §D.2`
- Magic-byte header byte-by-byte comparison against file header signatures.
- Extension != magic bytes denied.

### Cross-Company Isolation
Source: `audit/PHASE1_SOURCE_INVENTORY.md §D.4`
- Uploaded files stored in company-guid subdirectory.

---

## 6. Connection-String Standardization + Fail-Closed Production Guards

### Canonical Connection-String Key
Source: `audit/PHASE1_SOURCE_INVENTORY.md §B`
- Canonical key: `ConnectionStrings:LivestockManagerDb` (colon form / double-underscore env-var: `ConnectionStrings__LivestockManagerDb`.
- Legacy fallback: `ConnectionStrings:DefaultConnection` with warning.
- ConnectionStringStartupValidator performs early validation BEFORE builder creation. Startup exits code 1.

### Fail-Closed Production Guards
Source: `src/LivestockManager.Web/Program.cs:79-92`
- Early validation of seed flags in Production: Production.
- Any `SeedDemoData,` = true/1/yes in Production: Environment.Exit(1).
- `ProductionSeedGuard.CheckAndThrowIfUnsafe` second check post-builder.

---

## 7. Production Seed Fail-Closed; Testing/Development Explicit Opt-In

### DemoDataSeeder Guards
Source: `src/LivestockManager.Infrastructure/Persistence/Seed/DemoDataSeeder.cs:16-56` (audit/PHASE1_SOURCE_INVENTORY.md §E.1
- Production: NEVER seed unless `ASPNETCORE_ENVIRONMENT == Development (case-sensitive (exact match +
  OR `ASPNETCORE_ENVIRONMENT=Testing && EnableE2ESeed=1.
- Development: EnableDevSeed=true OR EnableE2ESeed=true" or "1".

### Startup Fail-Closed
Source: `src/LivestockManager.Web/Program.cs:79-92`
- Unsafe flags in Production → Environment.Exit(1).

---

## 8. Clock/Timezone Health Check + Mandatory Clock Verification

### Health Endpoints
Source: `src/LivestockManager.Web/Program.cs:116-117`
- "clock" health check: `TimeZoneInfo.Local.Id` non-empty; `DateTime.UtcNow` sanity range check.
- Tags: live + ready.

### Mandatory Clock Verification Log
Source: `src/LivestockManager.Web/Program.cs:256-261`
- Startup logs: UTC start time, Local timezone id, Local offset, Clock verification notice.

---

## 9. Mobile Responsive Drawer + Viewport E2E Matrix

### Mobile Drawer Implementation
Source: `src/LivestockManager.Web/Views/Views.cshtml:28-123` (inline CSS)
- Sidebar fixed: 56px → ↔ toggled.
- Breakpoint: max-width: 991.98px; CSS `transform: translateX(-100%) closed; .open slides in.
- Hamburger: .sidebar-backdrop show/hide.

### JS Toggle
Source: `_Layout.cshtml:129 + :288-307`
- Vanilla JS IIFE (no jQuery/BS collapse.
- Backdrop click closes.

### Viewport E2E Matrix
Source: `tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs:304-339`
- Workflow_16: 375×812 mobile (iPhone) - 3x DPR
- Workflow_17: 1920×1080 desktop (1x DPR)
- Both: `/` render without assertion failure.

### site.css @media Blocks
Source: `src/LivestockManager.Web/wwwroot/css/site.css:23, :284, :313`
- 3 explicit media queries for mobile, tablet-small, desktop.

---

## 10. Six Reports

### ReportsController Present
Source: Controllers: `src/LivestockManager.Web/Controllers/ReportsController.cs`
Views: `src/LivestockManager.Web/Views/Views/**/*.cshtml`

Reports:
1. Dashboard KPIs (Dashboard)
2. Farm/Livestock Profitability
3. Outstanding Invoices
4. Active Livestock
5. Sales by Period
6. Weight Changes

---

## 11. Invoices / Sales / Purchases / Expenses / Payments / Receipts Full Workflow + Reversals

### Controllers (14 controllers present):
Source: `src/LivestockManager.Web/Controllers/`
- InvoicesController.cs
- SalesController.cs
- PurchasesController.cs (via Suppliers + Livestock purchase workflows
- Expense workflows
- PaymentsController.cs
- Receipt generation (Receipts via InvoicesController/Receipts workflow

### Reversals
- Payment reversals / voids + allocations `IsReversed` column.
- Receipt reversal audit logged.

---

## 12. Identity 2FA UI Placeholder (Login Page Remember Me
Source: AccountController Login view
- Login page has "Remember me" checkbox present.
- MFA reset NOT present (not present (not present in source — see ROADMAP.md §NOT IMPLEMENTED).

---

## 13. Health Endpoints
Source: `src/LivestockManager.Web/Program.cs:205-221`

| Endpoint | Tags | Lines |
|---|---|---|
| `/health/live` | live | :205-209 |
| `/health/ready` | ready | :211-216 |
| `/health` | all (no predicate) | :218-222 |

All endpoints return JSON via custom `WriteHealthJson` ResponseWriter.

---

## 14. Audit Logs UI Page
Source: Controllers: `src/LivestockManager.Web/Controllers/AuditController.cs`
Route: `/Audit`
Views: `src/LivestockManager.Web/Views/Audit/*.cshtml`

---

## 15. Documents Upload List/Download
Source: Controllers: `src/LivestockManager.Web/Controllers/DocumentsController.cs`
- Upload (POST List (GET); Download (GET with company-guid folder path.
- 9-ext allow-list + magic bytes.

---

## 16. Settings Page
Source: Controllers: `src/LivestockManager.Web/Controllers/SettingsController.cs`
Views: `src/LivestockManager.Web/Views/Settings/*.cshtml`

---

## 17. Playwright E2E Runner

### Run-E2ETests.ps1
Source: `scripts/Run-E2ETests.ps1`
- 16-step orchestrator: detect root → restore/build → playwright install chromium → create disposable DB → set `ASPNETCORE_ENVIRONMENT=Testing` + `EnableE2ESeed=1 → migrate → seed → start web → `/health/live` wait 90s → run Playwright xUnit TRX → capture artifacts → finally stop+drop DB.
- Wrapper: `run-e2e-tests.cmd`.

### 18 E2E Tests
Source: `tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs:28-411`
- Workflow_01..Workflow_18 (18 [Fact] methods, runtime skip via BlockerSkip.

---

## 18. SQL Server Backup & Restore Scripts

### backup-database.ps1
Source: `scripts/backup-database.ps1`
- 8 gated steps: identifier regex → sqlcmd check → DB existence → backup dir write-test → `BACKUP DATABASE ... TO DISK=... WITH INIT, COMPRESSION, STATS=10` → exit check → zero-length guard → retention purge.
- Wrapper: `backup-database.cmd`

### restore-database.ps1
Source: `scripts/restore-database.ps1`
- 3 scenarios: (A) new DB RESTORE FILELISTONLY → WITH MOVE every logical file; (B) existing DB WITHOUT -ConfirmDestructiveOverwrite → exit 5 REFUSE; (C) existing WITH confirm → SINGLE_USER → RESTORE WITH REPLACE RECOVERY → MULTI_USER.
- Wrapper: `restore-database.cmd`

---

## 19. IIS Publish Scripts

### publish-iis.ps1
Source: `scripts/publish-iis.ps1`
- Delegates to `package-release.ps1`; creates App_Data + App_Data/files under publish output.
- Wrapper: `publish-iis.cmd`

### package-release.ps1
Source: `scripts/package-release.ps1`
- `dotnet publish LivestockManager.Web.csproj -c Release -r win-x64 --self-contained false -o ./artifacts/publish`.
- Wrapper: `package-release.cmd`

---

## 20. 8 Projects in Solution; 4 Test Projects

### 4 Source Projects (src/)
1. `LivestockManager.Domain.csproj` (net8.0, Nullable enable)
2. `LivestockManager.Application.csproj` (net8.0)
3. `LivestockManager.Infrastructure.csproj` (net8.0)
4. `LivestockManager.Web.csproj` (net8.0)

### 4 Test Projects (tests/)
5. `LivestockManager.UnitTests.csproj` (IsTestProject=true)
6. `LivestockManager.IntegrationTests.csproj` (IsTestProject=true)
7. `LivestockManager.ArchitectureTests.csproj` (IsTestProject=true)
8. `LivestockManager.EndToEndTests.csproj` (IsTestProject=true, IsPublishable=false)

Source: `audit/PHASE1_SOURCE_INVENTORY.md §L`
Solution file: `LivestockManager.sln`
