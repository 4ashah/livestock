# Project State

## Current Phase
AUDIT REMEDIATION MODE (commit ef36f34 baseline immutable, 2 Critical + 5 High release-blocking defects). Working branch: `remediation/audit-critical-fixes`.

## Phase Definition / Source
Source audit mode: `C:\Users\Administrator\Desktop\AUDIT REMEDIATION MODE.txt` (read-only compliance requirement).
Audit evidence: audit/AUDIT_SUMMARY.md, audit/DEFECTS.md, audit/SECURITY_FINDINGS.md, audit/DATABASE_FINDINGS.md, audit/TEST_RESULTS.md, audit/RELEASE_RECOMMENDATION.md.

## Remediation Order (strict)
1. DEF-002 (CRITICAL) Cross-company data access & ID tampering → AGENT PARTITION A (services layer only)
2. DEF-001 (CRITICAL) Broken Phase 2 role authorization → AGENT PARTITION B (web layer + policies)
3. DEF-004 (HIGH) Incorrect document number format → AGENT PARTITION C (EfSequenceGenerator)
4. DEF-005 (HIGH) Sequence concurrency exception handling → AGENT PARTITION C (shared with DEF-004, same file EfSequenceGenerator.cs)
5. DEF-003 (HIGH) Empty Integration/Arch/E2E test stubs → AGENT PARTITION D (tests folder only)
6. DEF-006 (HIGH) Broken database backup → AGENT PARTITION E (scripts folder only)
7. DEF-007 (HIGH) Broken database restore → AGENT PARTITION E (shared, same scripts folder)
8. DEF-010 (MEDIUM) 4 CS8629 nullable warnings in ReportService → MERGE AGENT only.

## Parallel Agent Partitioning (guaranteed no file conflicts)
### Agent A (DEF-002 Company Isolation) — Services Layer only
Files owned (EXCLUSIVE write):
- src/LivestockManager.Domain/Abstractions/* (may add IUserCompanyContext.cs interface definition; do not touch EfSequenceGenerator)
- src/LivestockManager.Application/Common/* (may add ICurrentUserAccessor or IUserCompanyContext if needed; keep IAppDbContext minimal edit if needed only)
- src/LivestockManager.Application/Services/** (EXCLUSIVE: 13 service interfaces + implementations)
  - Edit IInvoiceService.cs + InvoiceService.cs (GetByIdAsync/ConfirmAsync/CancelOrVoidAsync/RecalculateTotalsAsync/GetPdfAsync → add Guid companyId parameter, enforce entity.CompanyId == companyId)
  - Edit ISaleService.cs + SaleService.cs (same pattern for all 2+ methods)
  - Edit ILivestockService.cs + LivestockService.cs (GetByIdAsync/Discharge/AddWeight/Update/Delete → companyId)
  - Edit IPaymentService.cs + PaymentService.cs (Create/GetById/Reverse)
  - Edit IReceiptService.cs + ReceiptService.cs
  - Edit IPurchaseService.cs + PurchaseService.cs
  - Edit IExpenseService.cs + ExpenseService.cs
  - Edit ICustomerService.cs + CustomerService.cs
  - Edit ISupplierService.cs + SupplierService.cs
  - Edit IFarmService.cs + FarmService.cs
  - Edit ICompanyService.cs + CompanyService.cs (settings isolation)
  - Edit ICustomerBalanceService.cs + CustomerBalanceService.cs (company scope)
  - Edit IReportService.cs + ReportService.cs (company scope)
  - NOTE: In all methods, NEVER trust CompanyId from payload. Always use companyId parameter.
  - NotFound result for cross-company (do not reveal existence).

EXPLICITLY FORBIDDEN for Agent A:
- Do NOT edit Controllers
- Do NOT edit EfSequenceGenerator, DocumentNumberGenerator
- Do NOT edit Views, Program.cs, RoleNames.cs, appsettings
- Do NOT create migrations
- Do NOT touch scripts/
- Do NOT touch tests/ outside tests/LivestockManager.UnitTests/Services/ company scoping unit tests (12 tests DEF002 A-L scenarios, InMemory OK for predicate tests)

### Agent B (DEF-001 Role Authorization) — Web Layer + Policies only
Files owned (EXCLUSIVE write):
- src/LivestockManager.Web/Program.cs → AddAuthorization policies:
  - CanViewOperationalData = Viewer, DataEntry, FarmManager, Accounts, CompanyAdministrator, SystemAdministrator
  - CanManageLivestock = DataEntry, FarmManager, CompanyAdministrator, SystemAdministrator
  - CanManageSales = FarmManager, Accounts, CompanyAdministrator, SystemAdministrator
  - CanManageAccounting = Accounts, CompanyAdministrator, SystemAdministrator
  - CanManageCompany = CompanyAdministrator, SystemAdministrator
  - CanManageSystem = SystemAdministrator only
  - CanViewFinancialData = Accounts, FarmManager (readonly), CompanyAdministrator, SystemAdministrator
- src/LivestockManager.Web/Controllers/*.cs (ALL controllers; rewrite [Authorize(Roles="Administrator,Manager")] with policies or explicit RoleNames comma-joined constants. Keep Viewer role for read-only GET methods where appropriate. Add per-action role gates on ALL POSTs, not only class-level [Authorize].)
- src/LivestockManager.Web/Views/Shared/_Layout.cshtml (replace isInRole("Administrator")/("Manager") with RoleNames checks; match controller policies)
- src/LivestockManager.Web/Views/Shared/_LoginPartial.cshtml (if any role refs)
- src/LivestockManager.Domain/Common/RoleNames.cs (may add helper if needed, keep existing 6 consts + aliases intact)
- tests/LivestockManager.UnitTests/RoleAuthorizationMatrixTests.cs (update to verify policies / role consts used; 15 endpoint role tests)

EXPLICITLY FORBIDDEN for Agent B:
- Do NOT touch any Application services
- Do NOT touch EfSequenceGenerator, migrations
- Do NOT edit appsettings.json
- Do NOT touch scripts/

### Agent C (DEF-004 + DEF-005) — Sequencing only
Files owned (EXCLUSIVE write):
- src/LivestockManager.Infrastructure/Services/EfSequenceGenerator.cs (BOTH fixes: format + concurrency retry)
  - DEF-004: Year scoping — combine prefix with year UTC e.g. counter key = "PUR:2026" / "INV:2026" / "PAY:2026" / "RCP:2026" / "LIVESTOCK-Ah" (livestock does not need year scoping, keep original prefix-only). Format: {prefix}-{year}-{D5} e.g. INV-2026-00001, RCP-2026-00001, PAY-2026-00001, PUR-2026-00001. Default receipt prefix must be "RCP" (not "RCT") unless company.ReceiptPrefix overrides it. Existing invoice prefix default "INV" correct. Existing livestock format Ah00001 unchanged. Do NOT auto-re-number already issued documents (keep existing numbers if non-empty).
  - DEF-005: Concurrency — catch Microsoft.Data.SqlClient.SqlException ex when (ex.Number is 2601 or 2627 or 1205); bounded retry maxRetries=5 with random 10-60ms delays; also catch existing DbUpdateConcurrencyException and DbUpdateException paths; CancellationToken propagation; never infinite loops. Keep ROWLOCK/UPDLOCK/HOLDLOCK transaction design.
- src/LivestockManager.Infrastructure/Services/Sequencing/DocumentNumberGenerator.cs (if existing, match year scoped counter keys + format)
- Migration file NEW additive (only if Prefix column on SequenceCounters needs wider width to hold "PUR:2026" format; current NVarChar(50) is probably enough. If <= 50 chars SKIP migration, else create 20260807140000_SequencePrefixYearWidth.cs under Migrations/ folder with DI design-time factory correct connection string Server=.)
- tests/LivestockManager.UnitTests/Purchases + Finance/Services (10 tests; format: first 2026 invoice = INV-2026-00001, receipt prefix default RCP, company independent, year independent; for concurrency tests use SQL Server DB not InMemory (create helper connection to Test_LivestockManager_Concurrency DB)).

EXPLICITLY FORBIDDEN for Agent C:
- Do NOT edit controllers, services except sequencing class, Views, RoleNames.cs, scripts, backup/restore

### Agent D (DEF-003 Replace Empty Stubs) — Test Projects only
Files owned (EXCLUSIVE write, 3 test projects + any NuGet packages):
- DELETE tests/LivestockManager.IntegrationTests/UnitTest1.cs
- CREATE in tests/LivestockManager.IntegrationTests/:
  - LivestockManagerWebFactory.cs (extends WebApplicationFactory<Program>, ConfigureTestServices: UseSqlServer Test_LivestockManager_Integration DB or UseInMemory if SQL Server not strictly needed but company isolation MUST use real EF migrations)
  - AuthAndCompanyIsolationTests.cs (10+ tests: Unauthenticated redirect; Viewer can GET index; Accounts cannot edit farms; cross-company A cannot access company B's invoice via endpoint → NotFound/403; antiforgery on POST; sequence no duplicates 2 attempts)
  - InvoiceAndPaymentWorkflowTests.cs (5+ tests: Boot → Migrate → create draft → confirm → partial pay → final pay → get PDF → receipt creation → aging 5 buckets)
  - ProtectedDocumentTests.cs (2+ tests: cross-company download throws; invalid extension rejected)
- tests/LivestockManager.IntegrationTests/LivestockManager.IntegrationTests.csproj: add <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" /> and <InternalsVisibleTo if needed; reference Web project
- DELETE tests/LivestockManager.ArchitectureTests/UnitTest1.cs
- CREATE in tests/LivestockManager.ArchitectureTests/:
  - LayerReferenceTests.cs (add NetArchTest.Rules NuGet 1.3.2+: Domain NOT ref Infrastructure/Web; Application NOT ref Web; production projects never reference test projects)
  - ControllerAuthorizationTests.cs (controllers: no hardcoded legacy strings "Administrator,Manager"; all POST actions have explicit role/policy auth check)
  - ServiceArchitectureTests.cs (service methods: GetById signature includes companyId param for company-owned services; public method count reasonable)
- DELETE tests/LivestockManager.EndToEndTests/UnitTest1.cs
- CREATE tests/LivestockManager.EndToEndTests/E2eEnvironmentChecks.cs:
  - [Fact(Skip = "Playwright/browser dependencies not installed; marked blocked_external per remediation spec DEF-003 browser policy. Launch with .\\run-dev.cmd then verify manually.")] public void BrowserE2e_MarkedBlockedExternal(){ Assert.True(true, "blocked_external per audit remediation spec §6 browsers-unavailable clause"); }
  - Document all 18 workflows from §6 audit spec as [Fact(Skip="blocked_external")] methods (names only, no empty body).
- csproj fixes: ensure no Test1 placeholder methods anywhere, all tests have assertions.

EXPLICITLY FORBIDDEN for Agent D:
- Do NOT modify any production .cs files (src/**). Only test projects (tests/**).
- Do not create migrations or touch scripts.

### Agent E (DEF-006 Backup + DEF-007 Restore) — Scripts only
Files owned (EXCLUSIVE write):
- CREATE scripts/Backup-Database.ps1 (source of truth). Params:
  - [Parameter(Mandatory)] [string]$ServerInstance, [Parameter(Mandatory)] [string]$DatabaseName, [Parameter(Mandatory)] [string]$BackupDirectory, [int]$RetentionDays = -1, [string]$LogDirectory = (Join-Path (Resolve-Path .) "artifacts\logs")
  - Validations: DatabaseName NOT IN ('master','model','msdb','tempdb'); BackupDirectory (create if missing, test write access by making temp file). sqlcmd on PATH. Test SQL connectivity: SELECT name FROM sys.databases WHERE name = @DatabaseName.
  - Timestamp: $stamp = Get-Date -Format "yyyyMMdd_HHmmss"; $BackupFile = Join-Path $BackupDirectory ("{0}_{1}.bak" -f $DatabaseName, $stamp)
  - Execute: sqlcmd -S $ServerInstance -E -b -Q "BACKUP DATABASE [$DatabaseName] TO DISK=N'$BackupFile' WITH INIT, COMPRESSION, STATS=10;"
  - Verify: Test-Path $BackupFile AND (Get-Item $BackupFile).Length -gt 0
  - RETENTION ONLY AFTER SUCCESS: if ($RetentionDays -gt 0) { Get-ChildItem $BackupDirectory -Filter ($DatabaseName + "_*.bak") | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } | Remove-Item -Force }
  - Logs: write to $LogDirectory\Backup_$stamp.log. Exit code nonzero on any failure.
  - Never embed SQL credentials in scripts. Use -E trusted connection.
  - Avoid command injection in sqlcmd: use SqlParameter or [] quoting for identifiers; validate DatabaseName regex ^[A-Za-z0-9_@#$-]+$
- CREATE scripts/Restore-Database.ps1:
  - Params: [string]$ServerInstance, [Parameter(Mandatory)] [string]$BackupFile, [Parameter(Mandatory)] [string]$TargetDatabase, [string]$DataFileDirectory = "", [switch]$ConfirmDestructiveOverwrite
  - Validation: BackupFile exists and ends with .bak and (Get-Item $BackupFile).Length -gt 0. TargetDatabase not system databases. Missing backup → exit 3. Invalid .bak header → exit 4. ConfirmDestructiveOverwrite required IF target already exists.
  - Step 1: RESTORE FILELISTONLY to enumerate logical file names + physical names ($fileList = sqlcmd -S $ServerInstance -E -b -W -h -1 -Q "RESTORE FILELISTONLY FROM DISK=N'$BackupFile';")
  - Step 2: Build WITH MOVE for each logical → map to $DataFileDirectory if provided, else server default. Typical: data file .mdf → TargetDatabase.mdf, log .ldf → TargetDatabase_log.ldf
  - Step 3: If target EXISTS and NOT $ConfirmDestructiveOverwrite → write error + exit 5 (refuse by default).
  - Step 4: If target EXISTS and ConfirmDestructiveOverwrite TRUE: ALTER DATABASE [Target] SET SINGLE_USER WITH ROLLBACK IMMEDIATE only then RESTORE DATABASE... WITH REPLACE, RECOVERY plus each MOVE clause. Set back MULTI_USER.
  - Step 5: If target NOT EXISTS: RESTORE DATABASE with MOVE and RECOVERY (no ALTER DATABASE needed).
  - Step 6: Verify: sqlcmd -S $ServerInstance -E -b -Q "IF DB_ID(N'$TargetDatabase') IS NOT NULL SELECT 1 ELSE SELECT 0" — must return 1
  - Nonzero exit on failure. Logs to artifacts/logs/Restore_yyyyMMdd_HHmmss.log.
- EDIT backup-database.cmd: rewrite to call powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Backup-Database.ps1" -ServerInstance "%SQL_SERVER%" -DatabaseName "%SQL_DATABASE%" -BackupDirectory "%BACKUP_DIR%" (-RetentionDays if RETENTION_DAYS defined)
- EDIT restore-database.cmd: rewrite to call powershell ... scripts\Restore-Database.ps1 -ServerInstance "%SQL_SERVER%" -BackupFile "%BACKUP_FILE_PATH%" -TargetDatabase "%SQL_DATABASE%" -ConfirmDestructiveOverwrite:$([bool]::Parse("%CONFIRM_RESTORE_OVERWRITE_BOOL%")) — set env var default SQL_SERVER=. (not .\SQLEXPRESS, we use Server=. default instance MSSQLSERVER already)
- Create new scripts folder files: artifacts/logs directory is auto-created by PowerShell scripts using New-Item -ItemType Directory -Force
- VERIFY on actual SQL Server default instance (Server=.): create DB LivestockManager_Audit → BACKUP produces valid .bak file with timestamp name and non-zero size → RESTORE to NEW name LivestockManager_Audit_Restored_NEW as nonexistent target → verify DB usable.

EXPLICITLY FORBIDDEN for Agent E:
- No C# code changes at all. No migrations. No controllers/services edits.

## Merging
Merge order after all 5 agents finish:
1. Merge Agents A + B + C first (no overlapping files expected, so direct merge).
2. Merge Agent D (tests only) then Agent E (scripts only).
3. Merge Agent runs: DEF-010 CS8629 fix, integrate IUserCompanyContext into Web layer (if Agent A added interface), final additive DB migrations, run all tests.
4. If any merge conflict: Merge Agent edits via 3-way manual resolution in exact audit remediation priority order DEF-002 > DEF-001 > others.

## Last Completed Task
A0: Audited evidence read; 7 defects classified; branch remediation/audit-critical-fixes created from ef36f34 baseline immutable. 5 parallel agents ready to start.

## Current Task
A1/B1/C1/D1/E1 running in parallel.

## Build Result
Baseline (pre-remediation): Release Build SUCCEEDED — 8 projects, 0 errors, 4 CS8629 warnings (DEF-010). Target: 0/0 after MERGE AGENT.

## Test Result
Baseline: 175/175 unit (Release), 1/1 Integration empty, 1/1 Arch empty, 1/1 E2E empty = 178/178 inflated by stubs. After DEF-003 replaced: ≥200 tests all with real assertions.

## Migration Status
InitialMvp: APPLIED (21 tables). Phase2Entities: APPLIED (28 tables total). SequencePrefixYearWidth migration: PENDING only if Agent C confirms SequenceCounters.Prefix < required width.

## Resume Instructions (if context limit)
```
cd C:\Projects\livestock
git branch --show-current           # expect: remediation/audit-critical-fixes
git status --short                  # expect: agents A-E files dirty; committed per checkpoint commits
dotnet restore LivestockManager.sln -v minimal
dotnet build LivestockManager.sln -c Release -v minimal  # expect 0/0 after fixes
dotnet test LivestockManager.sln -c Release -v minimal   # expect all non-empty tests PASS
# Next if resume mid-defect: continue current defect from last checkpoint commit.
```

## Auto-resume Next Step after Context Limit
Current checkpoint hash after A0: none yet. After Agent A commits DEF-002: commit hash fix(security): enforce company isolation across business operations.
