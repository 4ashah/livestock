# LAST RUN — Livestock Manager Repository

## Phase = 12 + 13 + 14 COMBINED (Secrets Scan + Dependency/Third-Party Scans + Production Validation Scripts/Tests)

Completed: 2026-08-09T12:00:00.0000000Z UTC

---

## Phase 12 Deliverables — Redacted Secrets Scan (no values printed)

### 12a: Created
- **audit/FINAL_SECRETS_SCAN.md** — Walked repo excluding .git/bin/obj/artifacts/App_Data/coverage/.vs/wwwroot/lib/bootstrap/jquery and *.trx/*.bak/*.pfx/*.zip; searched for 12 categories of patterns (Password=, User ID=, UserId=, ApiKey, ClientSecret, SendGrid, SMTP password, PEM private key headers, JWT eyJ... form, production connection strings, internal production URLs, real customer data, test passwords Dev@123456 in production config files).

### Phase 12 Scan Results Summary
- **Total files scanned (exclusions applied)**: 428 source-tracked files
- **Total pattern matches**: 73
- **PRODUCTION_RISK matches**: 0 (zero production-risk secrets)
- **DEVELOPMENT_ONLY_ALLOWED**: 9 matches (DemoDataSeeder.cs Dev@123456, appsettings.json connection strings, DesignTime factory fallback, smoke-test.ps1)
- **E2E_ALLOWED**: 11 matches (ConnectionStringStandardizationTests.cs connection literals, E2eRemediationChecklist.cs Dev@123456 logins)
- **THIRD_PARTY_IGNORED**: 12 matches (all bootstrap dataApiKeydownHandler JS code + jquery-validation author attribution email)
- **FALSE_POSITIVE**: 41 matches (doc mentions, validation logic, test assertions, roadmaps, changelogs)
- **Required actions**: No production-risk secrets. Development/E2E passwords accepted only in designated source files (DemoDataSeeder.cs, E2ETest classes). scripts Run-E2ETests.ps1 variables already never printed.

---

## Phase 13 Deliverables — Dependency & Third-Party Scans + Notices

### 13a: Created
- **audit/FINAL_DEPENDENCY_SCAN.md** — Outputs captured of:
  - `dotnet list LivestockManager.sln package --vulnerable --include-transitive`
  - `dotnet list LivestockManager.sln package --outdated`

### Phase 13a Vulnerability Scan
- **Zero direct-dependency vulnerabilities** (all 12 occurrences are transitive only).
- 3 unique transitive packages (High severity):
  1. Microsoft.Extensions.Caching.Memory 8.0.0 (GHSA-qj66-m88j-hmgj) — Domain + Application
  2. Microsoft.Build 17.8.3 (GHSA-w3q9-fxm7-j8fq) — Web + all 4 test projects
  3. System.Text.Json 7.0.3/8.0.4 (GHSA-hh2w-p6rv-4g7w / GHSA-8g4q-xg66-9fp4) — Web + test projects
- Total occurrences across all projects: 12 (all High; 0 Critical; 0 direct).

### Phase 13a Outdated Packages
- 25 unique direct-dependency packages outdated.
- 50 total outdated references across all 8 projects.
- Most are .NET 8.x → 10.x major-version jumps (not recommended until stack targets net10; xunit Playwright have minor-only updates safe to apply independently).
- Minor-only (same SDK train): xunit 2.9.2 → 2.9.3; Microsoft.Playwright 1.52.0 → 1.61.0.

### 13b: Updated THIRD_PARTY_NOTICES.md (APPENDED)
Packages covered (10 components, all required notices + MIT/Apache-2.0/OFL summaries):
1. **Noto Sans Regular + Bold v2.000** — OFL 1.1 (existed from Phase 4; kept)
2. **Bootstrap v5.3.x** — MIT, Twitter Inc / Bootstrap Authors, LICENSE at wwwroot/lib/bootstrap/LICENSE
3. **jQuery v3.7.x** — MIT, JS Foundation, wwwroot/lib/jquery/LICENSE.txt
4. **jQuery Validation** — MIT, wwwroot/lib/jquery-validation/LICENSE.md
5. **jQuery Validation Unobtrusive** — MIT, wwwroot/lib/jquery-validation-unobtrusive/LICENSE.txt
6. **Microsoft.EntityFrameworkCore.* / Microsoft.AspNetCore.*** — MIT, .NET Foundation
7. **xUnit** — Apache 2.0 (.NET Foundation / James Newkirk / Brad Wilson)
8. **NetArchTest.Rules 1.3.2** — MIT (NuGet metadata attribution Ben Driver)
9. **FluentValidation 11.9.0** — Apache 2.0, Copyright Jeremy Skinner
10. **Microsoft.Playwright 1.52.0** — MIT, Microsoft Corporation

THIRD_PARTY_NOTICES.md status: **APPENDED** (Noto Sans already present, 9 new notices appended after OFL section).

### 13c: FINAL_DEPENDENCY_SCAN.md Section — Potentially Unused Direct Dependencies
- Confirmed **USED** (KEEP): NetArchTest.Rules (LayerReferenceTests.cs:1 `using NetArchTest.Rules`), Microsoft.Data.SqlClient, Microsoft.EntityFrameworkCore.InMemory, xunit + runner + coverlet (test SDK tooling), EF Design/Tools (PrivateAssets all build tooling), Playwright (E2E tests).
- **REVIEW for removal in Phase 15**:
  1. FluentValidation 11.9.0 (LivestockManager.Application) — zero AbstractValidator / IValidator / RuleFor call sites.
  2. FluentValidation.DependencyInjectionExtensions 11.9.0 (LivestockManager.Application) — zero AddFluentValidation() calls.
- Action: Phase 15 final gates will attempt removal; if build + all tests pass, packages are safe to delete. Retain until Phase 15 confirmation.

---

## Phase 14 Deliverables — Production config validation scripts, prerequisite check, first-admin scripts + tests

### 14a: Created **scripts/Validate-ProductionConfig.ps1** ([CmdletBinding()] + ServerInstance param; exit 0 safe, nonzero fail)
15 validation checks:
1. ASPNETCORE_ENVIRONMENT=Production (explicit)
2. Canonical ConnectionStrings:LivestockManagerDb non-empty, no __TO_FILL_AT_DEPLOY__, no (LocalDB), no LivestockManager_E2E prefix, no known dev DB variants
3. SeedDemoData/EnableDevSeed/EnableE2ESeed/ENV_ENABLE_DEV_SEED all false/0/unset
4. HTTPS transport: ASPNETCORE_URLS contains https:// OR Kestrel:Certificates:Default:Path configured
5. Protected Documents root path outside wwwroot (no "wwwroot" substring) + directory creatable
6. Data Protection key path exists / creatable
7. Log path writable
8. Backup path (BACKUP_PATH env or ./artifacts/backups default) writable
9. Elevated session only: dotnet --list-runtimes for Microsoft.AspNetCore.App >= 8.x (IIS Hosting Bundle check)
10. SQL Server connectivity: sqlcmd -S ServerInstance -Q "SELECT 1" exit 0
11. Migration status: dotnet ef migrations has-pending-model-changes exit 0 (zero pending)
12. Clock skew: Get-Date local vs SQL SYSUTCDATETIME() diff < 5 minutes
13. Required directories ACL best-effort check (AppPool/IIS_IUSRS write rules)
14. No LivestockManager_E2E test DB name anywhere in connection string
15. No localhost:5000/5001 dev defaults in ASPNETCORE_URLS

Never prints connection strings or passwords.

### 14b: Created **scripts/Check-Prerequisites.ps1** (exit 0 OK, nonzero fail)
Mandatory checks (FAIL = nonzero exit):
1. dotnet 8 SDK installed: dotnet --version >= 8.x
2. sqlcmd.exe present on PATH
3. Free disk space repo drive > 2 GB
4. Free disk space SQL Server default data drive > 5 GB (detectable; SqlDataDriveOverride available)

Informational warnings only (never fail):
- IIS present? If so, ASP.NET Core Module (Hosting Bundle) installation status notice.

### 14c: Created **scripts/New-FirstProductionAdmin.ps1** + Updated src/LivestockManager.Web/Program.cs first-admin CLI logic
**New-FirstProductionAdmin.ps1 features:**
- Interactive prompts: CompanyName, AdminFullName, AdminEmail (Read-Host); AdminPassword (Read-Host -AsSecureString — NEVER echoed).
- Secure handling: SecureString → SecureStringToBSTR → PtrToStringBSTR → Process env FIRST_ADMIN_PASSWORD.
- Immediately after dotnet run returns: BSTR zeroed (ZeroFreeBSTR), process env FIRST_ADMIN_PASSWORD cleared, SecureString nulled, GC.Collect.
- CLI args passed to dotnet: `--first-admin --first-admin-company "..." --first-admin-fullname "..." --first-admin-email "..."` — password NEVER on command line.
- Confirm prompt before any mutation.

**Program.cs first-admin CLI handling (new code lines 261–391):**
- Parses cmdArgs for --first-admin switch.
- Reads and IMMEDIATELY clears FIRST_ADMIN_PASSWORD process env var (ensures no subsequent hosted-service code can read it).
- Guard: AspNetUsers row count must == 0; exit 7 otherwise.
- Creates all 6 roles (missing ones only).
- Creates Company entity if not exists by name.
- Creates ApplicationUser via UserManager with FIRST_ADMIN_PASSWORD.
- Adds CompanyAdministrator + SystemAdministrator roles.
- Prints SUCCESS summary, Environment.Exit(0) — does NOT start Kestrel/web server (one-shot CLI tool mode).

### 14d: Created **tests/LivestockManager.UnitTests/Final/ProductionValidationScriptTests.cs** (5 xUnit [Fact] methods S1–S5)
Tests via file-exists + content keyword checking (no System.Management.Automation dependency required):
- **S1**: Validate-ProductionConfig.ps1 exists, has `[CmdletBinding()]`, declares `param(` block with ServerInstance.
- **S2**: Check-Prerequisites.ps1 exists; contains `dotnet --version` + `sqlcmd.exe` + `disk` keywords.
- **S3**: New-FirstProductionAdmin.ps1 exists; contains `Read-Host` + `-AsSecureString` + `SecureStringToBSTR` + `ZeroFreeBSTR` + `--first-admin`.
- **S4**: Publish-IIS.ps1 + Backup-Database.ps1 + Restore-Database.ps1 exist; backup script has BACKUP DATABASE keyword; restore script has RESTORE DATABASE keyword.
- **S5**: Run-E2ETests.ps1 exists; has `[CmdletBinding()]` + `param(` + `ServerInstance` declaration.

### 14d TEST RUN RESULT
Command: `dotnet test tests/LivestockManager.UnitTests/LivestockManager.UnitTests.csproj -c Release --filter FullyQualifiedName~ProductionValidationScriptTests`
- **Exit code**: 0
- **Tests total: 5 / Passed: 5 / Failed: 0 / Skipped: 0**
- Duration: 0.8731 s. Build: 0 Warnings, 0 Errors.

---

_This file auto-written by Phase 12+13+14 automation at 2026-08-09T12:00:00.0000000Z UTC._
