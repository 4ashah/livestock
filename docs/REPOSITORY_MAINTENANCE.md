# REPOSITORY MAINTENANCE

Phase 11 (2026-08-09): Tracked vs Generated, Clean, Rebuild, Package.

---

## 1. What is TRACKED (source code, committed) vs GENERATED (ephemeral, deletable)

### Tracked (Never delete these without a commit)

| Category | Examples | Where |
|---|---|---|
| Solution file | `LivestockManager.sln` | Root |
| Source code: `src/**/*.cs`, `src/**/*.csproj`, `src/**/*.cshtml`, `src/**/*.css`, `src/**/*.js`, `src/**/*.json` (except real Production secrets) | `src/LivestockManager.Domain`, `.Application`, `.Infrastructure`, `.Web` | `src/` |
| Test code: `tests/**/*.cs`, `tests/**/*.csproj` | UnitTests, IntegrationTests, ArchitectureTests, EndToEndTests | `tests/` |
| Authoritative docs (15+ files) | IMPLEMENTED_FEATURES, ROADMAP, ARCHITECTURE, SECURITY, DATABASE, DEPLOYMENT, E2E_TESTING, TEST_PLAN, USER_GUIDE, ADMIN_GUIDE, TRACEABILITY_MATRIX, MASTER_PLAN, THREAT_MODEL, REQUIREMENTS, CLOCK_VERIFICATION, MOBILE_UAT, REPOSITORY_MAINTENANCE | `docs/` |
| Audit evidence (14 .md files + CLEANUP_INVENTORY) | PHASE1_SOURCE_INVENTORY, DEFECTS, SECURITY/DATABASE/UI_FINDINGS, AUDIT_SUMMARY, REAUDIT_*, RELEASE_RECOMMENDATION, REMEDIATION_REPORT, TEST_RESULTS, CLEANUP_INVENTORY | `audit/` |
| Scripts (13 .ps1) | Run-E2ETests, build, clean, test, package-release, publish-iis, database-update, backup-database, restore-database, seed-demo-data, run-dev, run-local, smoke-test | `scripts/` |
| CMD wrappers (13 files) | build.cmd, clean.cmd, test.cmd, run-e2e-tests.cmd, etc. | Root |
| .gitignore, BUILD_STATUS.md, DEPLOY_CHECKLIST.md, CHANGELOG.md, THIRD_PARTY_NOTICES.md, appsettings.Production.example.json | Configuration & docs | Root |
| Vendor library static assets | `src/Lib/LivestockManager.Web/wwwroot/lib/{bootstrap,jquery,jquery-validation,...}` | `src/**/wwwroot/lib/` (if LibMan static) |

### Generated / Ephemeral (100% regeneratable — safe to delete locally)

| Category | Patterns | Why regeneratable |
|---|---|---|
| Build output | `**/bin/**`, `**/obj/**` | `dotnet build` regenerates |
| NuGet restore cache | `**/obj/{project.assets.json,*.cache,*.dgspec.json}` | `dotnet restore` |
| Publish output | `artifacts/publish/**`, `artifacts/production-publish/**` | `scripts/package-release.ps1` / `publish-iis.ps1` |
| Playwright browser binaries | `artifacts/e2e/.playwright-browsers/**` | `playwright.ps1 install chromium` (runs in Run-E2ETests.ps1 step 4) |
| E2E test artifacts | `artifacts/e2e/{results,screenshots,traces,videos,logs}/*` (but keep `.gitkeep`) | Run Run-E2ETests.ps1 again |
| Unit/Int/Arch test results | `TestResults/`, `artifacts/testresults/**`, `*.trx` | `dotnet test` |
| Code coverage | `coverage/`, `coverage.*` | Coverage tool re-run |
| Application logs | `artifacts/logs/`, `artifacts/e2e/logs/` | Rerun any script/app |
| Database backups (stale generated) | `artifacts/backups/*.bak` | Run `scripts/backup-database.ps1` |
| VS/Rider caches | `.vs/**`, `.idea/**`, `.freebuff/**`, `.trae/**` | IDE recreates on open |
| Runtime uploads | `App_Data/**` (outside repo; but if in Web project, delete locally — empty on fresh clone) | Web app creates at runtime |
| User-specific project settings | `*.user`, `*.suo` | IDE regenerates per user |
| Stale zips (**except `audit.zip`**) | `*.zip` (with `!audit.zip` exception in .gitignore) | Recreated via `git archive` or package-release.ps1 |
| Production secrets file (must NOT be in repo) | `appsettings.Production.json` | Stored externally in KeyVault / env vars |

---

## 2. How to CLEAN locally (reclaim disk)

### Step 1: SAFE dry-run — see what git would delete

```powershell
# DRY RUN (no actual deletion yet)
git clean -xdn
```

- `-x` = also delete files ignored by `.gitignore`
- `-d` = recurse into untracked directories
- `-n` = dry run, prints what it WOULD do

### Step 2: Targeted light clean (safe every-day)

```powershell
# Delete all bin/obj recursively
Get-ChildItem -Recurse -Directory | Where-Object { $_.Name -in 'bin','obj' } | Remove-Item -Recurse -Force

# Also delete TestResults, .vs, coverage
Get-ChildItem -Recurse -Directory | Where-Object { $_.Name -in 'TestResults','.vs','coverage' } | Remove-Item -Recurse -Force
```

Or use the clean script (also deletes ./artifacts):
```powershell
scripts\clean.ps1
# or
clean.cmd
```

### Step 3: AGGRESSIVE full clean (warning: deletes ALL untracked/ignored)

```powershell
# FIRST: Copy irreproducible audit/artifact evidence outside the repo (see §4 below)
# THEN:
git clean -xdf
```

- ⚠ **WARNING:** `-f` = force. **This deletes EVERYTHING not in HEAD** including:
  - audit.zip (copy it out first if you want it)
  - All contents of artifacts/ except any `.gitkeep`
  - Any uncommitted markdown in audit/ or docs/ — commit first!
  - Any local-only appsettings.*.json with real secrets

**Never run `git clean -xdf` without:**
1. Committing or stashing every uncommitted file you value; AND
2. Copying `audit/` + `artifacts/[latest-timestamped]` to an external drive (§4)

---

## 3. How to REBUILD from clean state

```powershell
# (1) Restore (downloads / unpacks NuGet packages into ~/.nuget + project obj/)
dotnet restore LivestockManager.sln

# (2) Build Release (compile C# → creates bin/ and obj/ for each project)
dotnet build LivestockManager.sln -c Release --no-restore

# (3) Unit + Integration + Architecture tests (no DB, no browser needed usually)
dotnet test LivestockManager.sln -c Release --no-restore

# (4) E2E tests (needs SQL Server + Playwright Chromium)
#     See docs/E2E_TESTING.md. Canonical env var: ConnectionStrings__LivestockManagerDb
run-e2e-tests.cmd
# or: scripts\Run-E2ETests.ps1
```

Or use the convenience wrappers:
```powershell
build.cmd          # step 1+2 combined (no-restore if already restored)
test.cmd           # step 3 (runs UnitTests only; see scripts\test.ps1)
run-e2e-tests.cmd  # step 4 (full E2E with Playwright + disposable DB)
smoke-test.cmd     # unit tests + Production smoke health check on port 5199
```

Expected exit code for each successful command: **0**.

---

## 4. How to PRESERVE audit artifacts before any clean

**BEFORE running `git clean -xdf` or any aggressive clean**, copy irreproducible artifacts outside the repository (e.g. USB drive, network share, backup folder `D:\audit-archive\YYYY-MM-DD\`):

```powershell
$archiveRoot = "D:\audit-archive\$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Force -Path $archiveRoot

# (1) Audit reports (authoritative markdown)
Copy-Item -Recurse -Path audit -Destination (Join-Path $archiveRoot 'audit')

# (2) Latest E2E traces / videos / screenshots (if any exist — latest timestamped)
Get-ChildItem artifacts\e2e -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -in 'traces','videos','screenshots' } | ForEach-Object {
    $latest = Get-ChildItem $_.FullName -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 5
    if ($latest) {
        $dest = Join-Path $archiveRoot "e2e\$($_.Name)"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        $latest | Copy-Item -Destination $dest
    }
}

# (3) Latest backup .bak (newest only, not all history)
if (Test-Path artifacts\backups) {
    $bak = Get-ChildItem artifacts\backups\*.bak -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($bak) {
        $dest = Join-Path $archiveRoot 'backups'
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item $bak.FullName -Destination $dest
    }
}

# (4) audit.zip packaged evidence
if (Test-Path audit.zip) { Copy-Item audit.zip -Destination $archiveRoot }

Write-Host "Preserved to: $archiveRoot"
```

Retain at minimum: `audit/*.md` (tracked but nice to have alongside generated artifacts) + any newly generated E2E evidence + the single most recent .bak backup.

---

## 5. How to create a SOURCE-ONLY review ZIP

A "source-only" ZIP is for code review and has ZERO generated files. It contains everything tracked in `HEAD` but no `bin/`, `obj/`, `artifacts/`, `.vs/`, etc.

### Option A (best: via git archive — guaranteed track-only)

```powershell
git archive -o review.zip HEAD
# or with explicit format + prefix
git archive --format zip --output review.zip --prefix livestock-source/ HEAD
```

SHA-256 of the ZIP (for audit log / chain of custody):
```powershell
Get-FileHash review.zip -Algorithm SHA256 | Format-List
```

### Option B (manual zip without bin/obj/artifacts)

If git isn't available, zip the whole repo **with explicit exclusions**:
```powershell
$exclude = @('bin','obj','.vs','.git','artifacts','TestResults','coverage','logs','App_Data','.trae','.freebuff','.idea','node_modules')
Compress-Archive -Path (Get-ChildItem -Exclude $exclude) -DestinationPath review-source.zip
```

Option A is preferred for audits because git archive only packages tracked, committed content (no local-only surprises).

---

## 6. How to create a PRODUCTION RELEASE package ZIP with SHA-256

This produces an IIS-deployable build output plus a sidecar SHA256 manifest.

```powershell
# (1) Produce self-contained or framework-dependent publish under artifacts/publish
scripts\package-release.cmd
# or: scripts\package-release.ps1 -Configuration Release -Runtime win-x64 -SelfContained $false

# (2) Also run IIS post-publish prep (App_Data/files dir)
scripts\publish-iis.cmd

# (3) Create release ZIP from artifacts/publish
$releaseTag = "release-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$releaseZip = "artifacts\$releaseTag.zip"
Compress-Archive -Path artifacts\publish\* -DestinationPath $releaseZip -CompressionLevel Optimal

# (4) Produce SHA256 sidecar file (standard format: hash<TAB>filename)
$hash = (Get-FileHash $releaseZip -Algorithm SHA256).Hash
"$hash`t$releaseTag.zip" | Set-Content -Path "artifacts\$releaseTag.zip.sha256" -Encoding ASCII

Write-Host "Release package: $releaseZip"
Write-Host "SHA256: $hash"
```

**Production deployment (high-level):** copy `artifacts\$releaseTag.zip` to IIS server → unzip into `C:\inetpub\livestock\` → set SQL-connection env var `ConnectionStrings__LivestockManagerDb` (double underscore) in `web.config` ASP.NET Core `environmentVariables` section or in machine-level env vars → recycle app pool → hit `/health/live`.

For detailed checklists, see `DEPLOY_CHECKLIST.md` at repo root and `docs/DEPLOYMENT.md`.
