# DEFECTS FINDINGS & AUDIT LOG

This document lists all actionable defects identified during the independent audit of commit `ef36f34`.

---

## Summary of Audit Defects

| Defect ID | Severity | Module | Title | Release Blocking |
|---|---|---|---|---|
| DEF-001 | Critical | Auth & Authorization | Controller Action Authorization Uses Stale Phase-1 Role Strings (`"Administrator,Manager"`) | **Yes** |
| DEF-002 | Critical | Authorization / Multi-Tenancy | `InvoiceService.GetByIdAsync` & Related Endpoints Omit `CompanyId` Ownership Validation | **Yes** |
| DEF-003 | High | Test Infrastructure | Integration, Architecture, and E2E Test Projects Contain Empty 0-Assertion Stubs | **Yes** |
| DEF-004 | High | Sequence Generator | `EfSequenceGenerator` Fails Year-Scoped Formatting (`INV-YYYY-NNNNN`, `RCP-YYYY-NNNNN`) | **Yes** |
| DEF-005 | High | Database / Concurrency | `EfSequenceGenerator` Does Not Catch `SqlException` Under Concurrent Sequence Creation | **Yes** |
| DEF-006 | High | Backup & Recovery | `backup-database.cmd` Fails Timestamp Generation Due to Deprecated `wmic` Utility | **Yes** |
| DEF-007 | High | Backup & Recovery | `restore-database.cmd` Fails When Restoring Backup to New Target Database | **Yes** |
| DEF-008 | Medium | PDF Generation | Custom PDF 1.4 Writer (`FormattedPdfWriter`) Truncates Long Invoices Without Page Overflow | No |
| DEF-009 | Medium | PDF Generation | `FormattedPdfWriter` Lacks Unicode Font Encoding for Non-ASCII Text & Currency Symbols | No |
| DEF-010 | Medium | Build & Code Quality | Release Build Emits 4 CS8629 Nullable Value Type Warnings in `ReportService.cs` | No |
| DEF-011 | Low | Settings UI | `SettingsController` Index GET Does Not Include Anti-Forgery Token Validation Guidance | No |

---

## Detailed Defect Records

### DEF-001: Controller Action Authorization Uses Stale Phase-1 Role Strings
- **Severity**: Critical
- **Module**: Authorization / Controllers (`SalesController`, `PaymentsController`, `InvoicesController`, `FarmsController`, `CustomersController`, `LivestockController`)
- **Title**: Action attributes reference `"Administrator,Manager"` instead of official Phase-2 roles (`FarmManager`, `Accounts`, `CompanyAdministrator`, `SystemAdministrator`)
- **Evidence**:
  - `SalesController.cs` L97: `[Authorize(Roles = "Administrator,Manager")]`
  - `InvoicesController.cs` L95, L104: `[Authorize(Roles = "Administrator,Manager")]`
  - `PaymentsController.cs` L81, L114: `[Authorize(Roles = "Administrator,Manager")]`
  - `LivestockController.cs` L256, L279: `[Authorize(Roles = "Administrator,Manager")]`
  - `RoleNames.cs`: Defines `Viewer`, `DataEntry`, `FarmManager`, `Accounts`, `CompanyAdministrator`, `SystemAdministrator`. `"Manager"` does not exist.
- **Reproduction Steps**:
  1. Log in as `accounts@livestock.dev` or `farmmanager@livestock.dev`.
  2. Navigate to POST `/sales/create` or `/invoices/confirm/{id}` or `/invoices/downloadpdf/{id}`.
  3. ASP.NET Core Identity returns `403 Access Denied`.
- **Expected Result**: Authorization matrix allows `Accounts` to manage sales/invoices/payments, `FarmManager` to manage livestock/farms, and `SystemAdministrator` full access.
- **Actual Result**: Access denied for `FarmManager`, `Accounts`, and `SystemAdministrator`.
- **Likely Root Cause**: Legacy Phase-1 role strings were left unupdated when Phase-2 6-role matrix was implemented.
- **Recommended Correction**: Replace hardcoded role strings with constants from `RoleNames.cs` (e.g. `[Authorize(Roles = $"{RoleNames.CompanyAdministrator},{RoleNames.Accounts},{RoleNames.SystemAdministrator}")]`).
- **Required Regression Test**: `RoleAuthorizationMatrixTests.VerifyAllControllerActions_UsePhase2RoleConstants`.
- **Release Blocking**: **YES**.

---

### DEF-002: Cross-Company Invoice & Document Access via ID Manipulation
- **Severity**: Critical
- **Module**: Application Services / Invoices (`InvoiceService`, `InvoicesController`, `SalesController`)
- **Title**: Entity retrieval by GUID in `InvoiceService.GetByIdAsync` does not verify `CompanyId` ownership
- **Evidence**:
  - `InvoiceService.cs` L32: `_db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct)`
  - `InvoicesController.cs` L74: `_invoiceService.GetByIdAsync(id, ct)`
- **Reproduction Steps**:
  1. User A (Company 1) logs in.
  2. User A sends GET request to `/invoices/details/{Invoice_Guid_Belonging_To_Company_2}`.
  3. `InvoiceService.GetByIdAsync` executes query without checking `invoice.CompanyId == userCompanyId`.
- **Expected Result**: System returns `404 Not Found` or `403 Forbidden` when attempting to access another company's records.
- **Actual Result**: Invoice details and line items of Company 2 are returned to User A.
- **Likely Root Cause**: Lack of `CompanyId` tenant scoping in domain service methods.
- **Recommended Correction**: Pass `companyId` into `GetByIdAsync`, `ConfirmAsync`, and `CancelOrVoidAsync`, enforcing `.Where(i => i.Id == id && i.CompanyId == companyId)`.
- **Required Regression Test**: `InvoiceServiceTests.GetByIdAsync_CrossCompanyId_ThrowsNotFound`.
- **Release Blocking**: **YES**.

---

### DEF-003: Integration, Architecture, and E2E Test Projects Contain Empty 0-Assertion Stubs
- **Severity**: High
- **Module**: Automated Test Suites (`tests/LivestockManager.IntegrationTests`, `tests/LivestockManager.ArchitectureTests`, `tests/LivestockManager.EndToEndTests`)
- **Title**: Reported 1 integration test, 1 architecture test, and 1 E2E test are empty methods with 0 assertions
- **Evidence**:
  - `tests/LivestockManager.IntegrationTests/UnitTest1.cs` L5-L9: `[Fact] public void Test1() { }`
  - `tests/LivestockManager.ArchitectureTests/UnitTest1.cs` L5-L9: `[Fact] public void Test1() { }`
  - `tests/LivestockManager.EndToEndTests/UnitTest1.cs` L5-L9: `[Fact] public void Test1() { }`
- **Reproduction Steps**:
  1. Run `dotnet test tests/LivestockManager.IntegrationTests/LivestockManager.IntegrationTests.csproj`.
  2. Inspect output: 1 passed in 1ms.
  3. Inspect file source code: empty method block.
- **Expected Result**: Integration tests execute real DB workflows; Architecture tests assert layer reference rules; E2E tests assert HTTP/UI flows.
- **Actual Result**: 3 empty stub methods pass without executing any logic or assertions.
- **Likely Root Cause**: Placeholder test classes generated during initial solution setup were never populated.
- **Recommended Correction**: Implement real integration tests using `WebApplicationFactory<Program>`, NetArchTest for architecture tests, and Playwright/Puppeteer for E2E tests.
- **Required Regression Test**: Assert test suite executes non-empty test cases with real assertions.
- **Release Blocking**: **YES**.

---

### DEF-004: Document Number Generator Fails Year-Scoped Format Requirement
- **Severity**: High
- **Module**: Infrastructure / Sequence Generator (`EfSequenceGenerator`)
- **Title**: Document sequence generator produces `INV00001` / `RCT00001` instead of year-scoped `INV-YYYY-NNNNN` / `RCP-YYYY-NNNNN`
- **Evidence**:
  - `EfSequenceGenerator.cs` L51: `return prefix + lastValue.ToString("D5");`
  - `InvoiceService.cs` L56: `_sequenceGenerator.GenerateInvoiceNumberAsync(invoice.CompanyId)` -> returns `INV00001`.
  - `ReceiptService.cs` L50: `_sequenceGenerator.GenerateReceiptNumberAsync(companyId)` -> returns `RCT00001`.
  - Specification in `docs/DATABASE.md` & Audit requirements: Requires `INV-YYYY-NNNNN`, `RCP-YYYY-NNNNN`, `PUR-YYYY-NNNNN`, `PAY-YYYY-NNNNN`.
- **Reproduction Steps**:
  1. Confirm a draft invoice.
  2. Inspect generated `InvoiceNumber` column: value is `INV00001` (missing year scoping and hyphen formatting).
- **Expected Result**: Invoice number generated as `INV-2026-00001`.
- **Actual Result**: Invoice number generated as `INV00001`.
- **Likely Root Cause**: `EfSequenceGenerator` prefix handling does not combine company prefix with current UTC year.
- **Recommended Correction**: Update `GenerateInvoiceNumberAsync` and `GenerateReceiptNumberAsync` to combine prefix and year (e.g. `$"{prefix}:{year}"`), returning formatted string `$"{prefix}-{year}-{lastValue:D5}"`.
- **Required Regression Test**: `SequenceGeneratorTests.GenerateInvoiceNumber_IncludesCurrentYearAndHyphens`.
- **Release Blocking**: **YES**.

---

### DEF-005: `EfSequenceGenerator` Concurrency Retry Does Not Catch `SqlException`
- **Severity**: High
- **Module**: Infrastructure / Sequence Generator (`EfSequenceGenerator.cs`)
- **Title**: Concurrency retry block catches `DbUpdateConcurrencyException` and `DbUpdateException`, but raw ADO.NET SQL commands throw `SqlException`
- **Evidence**:
  - `EfSequenceGenerator.cs` L139-L150: Only catches `DbUpdateConcurrencyException` and `DbUpdateException`.
  - Lines 86-132 use raw `DbCommand` (`updateCmd.ExecuteScalarAsync()` and `insertCmd.ExecuteScalarAsync()`), which throws `Microsoft.Data.SqlClient.SqlException` on primary key or lock conflicts.
- **Reproduction Steps**:
  1. Trigger concurrent sequence generation requests for a new prefix simultaneously.
  2. SQL Server returns error 2627 (Violation of Primary Key) on initial `INSERT INTO SequenceCounters`.
  3. `EfSequenceGenerator` fails to catch `SqlException`, bypassing retry loop and crashing caller with unhandled exception.
- **Expected Result**: Sequence generator catches database lock/duplicate key conflicts and retries cleanly up to `maxRetries`.
- **Actual Result**: System throws unhandled `SqlException`.
- **Likely Root Cause**: `DbCommand` execution bypasses EF Core exception wrapping.
- **Recommended Correction**: Add `catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 1205)` to retry loop.
- **Required Regression Test**: `EfSequenceGeneratorTests.ConcurrentGeneration_RetriesAndSucceeds`.
- **Release Blocking**: **YES**.

---

### DEF-006: Database Backup Script Fails Timestamp Generation on Modern OS
- **Severity**: High
- **Module**: DevOps / Deployment Scripts (`backup-database.cmd`)
- **Title**: Deprecated `wmic` command produces invalid backup filename without extension
- **Evidence**:
  - `backup-database.cmd` L10: `for /f "tokens=2 delims==" %%a in ('wmic os get localdatetime /value ^| findstr "="') do set "DT=%%a"`
  - Console output on Windows 11 / Server 2025: `'wmic' is not recognized as an internal or external command`.
  - Output filename: `LivestockManager_Audit-~0,8DT:~8,6.bak`.
- **Reproduction Steps**:
  1. Execute `.\backup-database.cmd` on standard modern Windows OS.
  2. Observe console warning and inspect `artifacts/backups/`.
- **Expected Result**: Backup created as `LivestockManager_Audit-20260807-153422.bak`.
- **Actual Result**: Backup created with broken malformed filename `LivestockManager_Audit-~0,8DT`.
- **Likely Root Cause**: Windows 11 / Server 2025 removed `wmic.exe`.
- **Recommended Correction**: Replace `wmic` in `backup-database.cmd` with PowerShell ISO timestamp formatting (e.g. `for /f %%a in ('powershell -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set STAMP=%%a`).
- **Required Regression Test**: Execute `backup-database.cmd` and verify `.bak` extension and valid date string in filename.
- **Release Blocking**: **YES**.

---

### DEF-007: Database Restore Script Fails When Restoring to New Target Database
- **Severity**: High
- **Module**: DevOps / Deployment Scripts (`restore-database.cmd`)
- **Title**: Script executes `ALTER DATABASE` prior to checking DB existence and lacks `MOVE` clauses for target database paths
- **Evidence**:
  - `restore-database.cmd` L15: `sqlcmd -S "%SQL_SERVER%" -E -Q "ALTER DATABASE [%SQL_DATABASE%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [%SQL_DATABASE%] FROM DISK='%BACKUP_FILE_PATH%' WITH REPLACE, RECOVERY, STATS=10; ALTER DATABASE [%SQL_DATABASE%] SET MULTI_USER;"`
  - Output when restoring to `LivestockManager_Audit_Restored`: `Msg 5011 ... User does not have permission to alter database 'LivestockManager_Audit_Restored', the database does not exist...`
- **Reproduction Steps**:
  1. Set `SQL_DATABASE=LivestockManager_Audit_Restored`.
  2. Run `.\restore-database.cmd`.
- **Expected Result**: Database restored successfully to target database name.
- **Actual Result**: `sqlcmd` fails with error 5011 and terminates abnormally.
- **Likely Root Cause**: `ALTER DATABASE` statement assumes target database already exists; lacks conditional check `IF EXISTS (SELECT 1 FROM sys.databases WHERE name = ...)` and `WITH MOVE` parameters.
- **Recommended Correction**: Wrap `ALTER DATABASE` in `IF DB_ID('%SQL_DATABASE%') IS NOT NULL` check, and support dynamic file relocation.
- **Required Regression Test**: Execute `restore-database.cmd` to a fresh non-existent database name and verify successful recovery.
- **Release Blocking**: **YES**.

---

### DEF-008: Custom PDF Generator Truncates Long Invoices Without Page Overflow
- **Severity**: Medium
- **Module**: Infrastructure / PDF Generator (`FormattedPdfWriter.cs`)
- **Title**: Multi-item invoices exceeding single-page height drop line items off the bottom of the page
- **Evidence**:
  - `FormattedPdfWriter.cs` L509: `if (y < MarginBottom + 12) break;`
  - Single fixed page stream object (Object 3 `MediaBox [0 0 612 792]`, Object 4 single `/Contents 4 0 R`).
- **Reproduction Steps**:
  1. Create a sale with 25 line items.
  2. Generate PDF.
  3. Open generated PDF: line items past item 18 are missing; no page 2 is created.
- **Expected Result**: Multi-page PDF generated with header/footer and page numbers on page 2.
- **Actual Result**: Line items truncated; totals box collides with table footer or breaks layout.
- **Likely Root Cause**: `FormattedPdfWriter` is hardcoded to a 1-page PDF stream structure.
- **Recommended Correction**: Implement multi-page catalog and page tree array in `AssemblePdf`.
- **Required Regression Test**: `FormattedPdfWriterTests.GenerateInvoicePdf_25Items_ProducesTwoPagePdf`.
- **Release Blocking**: No (Can be constrained by limiting line item counts in UI until fixed).

---

### DEF-009: `FormattedPdfWriter` Lacks Unicode Encoding Support
- **Severity**: Medium
- **Module**: Infrastructure / PDF Generator (`FormattedPdfWriter.cs`)
- **Title**: Non-ASCII customer names, accents, and special characters render incorrectly in PDF output
- **Evidence**:
  - `FormattedPdfWriter.cs` L304-L305: Font defined as standard `/Helvetica` Type1 font.
  - `EscapePdfString` (L536) converts non-ASCII characters (`c > 126`) to octal byte strings without WinAnsiEncoding or CIDFont dictionary.
- **Reproduction Steps**:
  1. Create customer with name `"José María Gómez"`.
  2. Generate invoice PDF.
  3. Open PDF in Adobe Reader or browser PDF viewer: accent characters render as missing glyphs or raw octal sequences.
- **Expected Result**: Special characters render cleanly.
- **Actual Result**: Glyphs garbled or missing.
- **Likely Root Cause**: Standard Type1 Helvetica font without encoding map does not support extended Unicode.
- **Recommended Correction**: Add WinAnsiEncoding font dictionary or integrate true type font embedding.
- **Required Regression Test**: `FormattedPdfWriterTests.GeneratePdf_UnicodeName_RendersWithoutError`.
- **Release Blocking**: No.

---

### DEF-010: Release Build Emits CS8629 Nullable Value Type Warnings
- **Severity**: Medium
- **Module**: Application Services / Reports (`ReportService.cs`)
- **Title**: Compiler emits 4 CS8629 warnings during Release build
- **Evidence**:
  - Compiler output during `dotnet build -c Release`:
    `ReportService.cs(196,26): warning CS8629: Nullable value type may be null.`
    `ReportService.cs(211,50): warning CS8629: Nullable value type may be null.`
    `ReportService.cs(213,34): warning CS8629: Nullable value type may be null.`
    `ReportService.cs(250,27): warning CS8629: Nullable value type may be null.`
- **Reproduction Steps**:
  1. Execute `dotnet build LivestockManager.sln -c Release`.
  2. Inspect output line: `4 Warning(s), 0 Error(s)`.
- **Expected Result**: Clean compilation with 0 warnings.
- **Actual Result**: 4 compiler warnings emitted.
- **Likely Root Cause**: Dereferencing nullable `.Value` properties on nullable `DateTimeOffset?` or `decimal?` in LINQ queries without explicit `.HasValue` check.
- **Recommended Correction**: Add null checks or coalescing operators in `ReportService.cs` lines 196, 211, 213, 250.
- **Required Regression Test**: `dotnet build LivestockManager.sln -c Release` returns 0 warnings.
- **Release Blocking**: No.

---

## REMEDIATION PHASE RESOLUTION — BRANCH `remediation/audit-critical-fixes`

_The baseline findings above are preserved as the immutable audit record against commit `ef36f34`. This section records the resolution status of each release-blocking (and Medium follow-up) defects on the remediation branch.

| Defect ID | Severity | Status | Resolution Commit(s) | Notes |
|---|---|---|---|---|
| DEF-001 | Critical | **Closed** | db28126 | 7 named authorization policies registered with `RoleNames.*` constants; 14 controllers re-authorized; `_Layout.cshtml` menu gates aligned with policies; architecture test `Controllers_NoHardcodedLegacyRoleStrings` enforces no literal `"Administrator,Manager"` substring in controller assembly attributes; reflection enumerates all protected POSTs have explicit Authorize. |
| DEF-002 | Critical | **Closed** | 2867b94 | 23 service files: all Get/List/StateChange/Download methods gained mandatory `Guid companyId` param; WHERE requires `CompanyId == companyId`; cross-company misses → generic `DomainException("X not found.")` → `NotFound()` controller response (no existence enumeration); 12 regression tests `CompanyIsolationTests` A..L all pass; trusted `companyId = (await UserManager.GetUserAsync(User)).CompanyId` at controller call sites per DEF-001 merge. |
| DEF-003 | High | **Closed** | 2f9fc22 + merge A7 | 3 placeholder `UnitTest1.cs` deleted; replaced with 15 real Integration assertions (15/15 pass: boot 3 + auth 8 + workflow 4), 10 Architecture tests (10/10 pass: layer refs + role literal guard + Test1 method count=0 reflection assert), 18 E2E `[Fact(Skip="blocked_external: ...")]` with documented 1..18 checklist items and non-empty `Assert.True(true)` bodies; grep + reflection both report `Test1` count=0. |
| DEF-004 | High | **Closed** | b7857fb | Counter key = `"{prefix}:{yyyy}"` (UTC calendar year); generated value = `"{prefix}-{yyyy}-{D5}"` e.g. `INV-2026-00001`, `RCP-2026-00001`; `ReceiptPrefix ??= "RCP"` default corrects legacy `"RCT"` typo; additive migration `SequencePrefixYearWidth` widens `SequenceCounters.Prefix` NVARCHAR(20) → 100; historical non-empty numbers preserved via `if (string.IsNullOrWhiteSpace(...))` overwrite guard. Livestock ID format `Ah00001` unchanged (no year). |
| DEF-005 | High | **Closed** | b7857fb | Max retries=5 (6 total attempts); 10–60 ms jitter `Random.Shared`; catch block covers: `DbUpdateConcurrencyException`, `DbUpdateException`, and **raw** `Microsoft.Data.SqlClient.SqlException when Number is 2601 (dup key) / 2627 (UK) / 1205 (deadlock)`; repeatable-read `ROWLOCK,UPDLOCK,HOLDLOCK` tx with atomic UPDATE OUTPUT else INSERT; CancellationToken propagated through all GenerateXAsync public/private; 3 real SQL `Test_LivestockManager_SeqConcurrency` concurrency tests verify 20 parallel → 20 distinct contiguous invoice numbers w/ 0 unhandled exceptions. |
| DEF-006 | High | **Closed** | 0a831ed | Deprecated `wmic` call entirely removed; timestamp now `Get-Date -Format "yyyyMMdd_HHmmss"` (native PS); `scripts/Backup-Database.ps1` + root `backup-database.cmd` wrapper with `SQL_SERVER=.`, `BACKUP_DIR`, `LOG_DIR`, `SQL_DATABASE` defaults; 8 exit-code gates (identifier regex, system DBs reject, sqlcmd missing, connection absent, backup dir write-test, BACKUP DATABASE exit non-zero, zero-length, retention only after success). VERIFY step produced `LivestockManager_AuditTempSrc_20260808_060955.bak` 485,376 bytes > 0 confirmed. |
| DEF-007 | High | **Closed** | 0a831ed | `restore-database.cmd` wraps `scripts/Restore-Database.ps1`; existence check `ISNULL(DB_ID(@target),0)` FIRST → if non-existent → zero SQL ALTERs, proceed `RESTORE FILELISTONLY + WITH MOVE` each logical file to server-default data/log dirs; target exists WITHOUT `-ConfirmDestructiveOverwrite` → exit code 5 (refuse, zero SQL run); target exists + confirm → `SINGLE_USER ROLLBACK IMMEDIATE` → `RESTORE ... REPLACE` → `MULTI_USER`. VERIFY: restore to NEW target name exit 0, overwrite-attempt exit=5 REFUSE, sys.tables query integer OK, cleanup DROP both temp DBs clean pollution-free. |
| DEF-010 | Medium (non-blocking) | **Closed** | merge-A7 | 4× CS8629 nullable `.Value` warnings in `ReportService.CompleteLivestockProfitabilityAsync` L196/211/213/250 fixed by `.Where(x => x.X.HasValue)` guard + `.GetValueOrDefault()` accessor. 1× CS8602 `PaymentService.PostAsync` L63 null-forgiving `!` replaced with explicit `?? throw new DomainException(...)`. Release `dotnet build LivestockManager.sln -c Release` → **0 Warnings / 0 Errors** confirmed. |
| DEF-008/009/011 | Medium/Low | **Deferred** | — | Marked non-blocking by audit; follow-up PDF enhancement / PDF i18n / Settings UI token guidance respectively; ValidateAntiForgeryToken on POSTs already present; out of scope for remediation patch. |

---

## REMEDIATION VALIDATION MATRIX — FULL TALLY

| Validation Gate | Expected | Actual | Result |
|---|---|---|---|
| Baseline commit `ef36f34` preserved untouched | Yes | Verified via `git log` — baseline remains parent; remediation on branch `remediation/audit-critical-fixes` | PASS |
| Branch name | `remediation/audit-critical-fixes` | `git branch --show-current` matches | PASS |
| Release build warnings | 0 | 0 Warnings / 0 Errors | PASS |
| Release build errors | 0 | 0 Errors | PASS |
| Unit tests pass | — | 199 / 199 (incl. 12 CompanyIsolation A..L; 10 SequenceRemediation) | PASS |
| Integration tests pass | — | 15 / 15 (boot 3 + auth 8 + workflow 4) | PASS |
| Architecture tests pass | — | 10 / 10 (incl. Test1 count=0; No legacy role strings; GetById requires companyId) | PASS |
| E2E blocked_external format | 18 documented, non-empty bodies | 18 `[Fact(Skip="blocked_external:...")]` methods 1..18, each body contains `Assert.True(true)`; 0 empty bodies | PASS |
| `Test1` method across test assemblies | 0 occurrences | Grep 0 matches + reflection assert Expected=0 Actual=0 | PASS |
| Additive migration | 1 new, never edits applied ones | `20260807140000_SequencePrefixYearWidth` ALTER COLUMN only (no drop/create), InitialMvp/Phase2Entities untouched | PASS |
| Additive migration applied to SQL | Success | `dotnet ef database update` succeeded, `__EFMigrationsHistory` contains new row | PASS |
| Backup VERIFY a-g (actual SQL default instance) | PASS | a) Create src OK b) backup exit 0 c) .bak regex-match 485,376 bytes d) restore NEW target exit 0 e) sys.tables count integer f) overwrite-refuse exit=5 g) cleanup drops both OK | PASS |
| Concurrency test: 20 parallel invoice numbers | 20 distinct + contiguous | NoDuplicates=20 distinct; SequentialRange=00001..00020; NoUnhandledExceptions=0 | PASS |
| PDF binary header | `%PDF-1.` magic bytes (7) | Integration `InvoicePdf_Download_ReturnsPdfHeader7Bytes`: 7-byte header match | PASS |
| Receipt default prefix | `"RCP"` | Company default `ReceiptPrefix ??= "RCP"`; generated number format includes `RCP-YYYY-NNNNN` | PASS |
| Company isolation architecture rule | All 10 company-owned `GetByIdAsync` require `Guid companyId` | Architecture reflection test asserts param exists on all interfaces | PASS |
| Cross-company controller test: Invoice Details + Livestock Edit POST | NotFound / Forbidden (no 200) | 404 / 403 both accepted → actual = 404 NotFound (fail-closed, existence not leaked) | PASS |
| Publish file count ≥ 366 | Release 0/0 | `Get-ChildItem artifacts\publish -Recurse` → 469 files | PASS |
| Audit scope: new feature additions | None allowed | Diff inspection = only security/convention/hardening changes; 0 routes added; 0 new domain entities | PASS |
| Release recommendation by automated script | NOT APPROVED (require independent re-audit) | `audit/REMEDIATION_REPORT.md §12` explicitly states release not auto-approved; state file `LAST_RUN.md` recommendation = Pending Independent Re-Audit | PASS |
