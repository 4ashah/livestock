# REPOSITORY CLEANUP INVENTORY

Categorization of every file and directory in the working tree. 2026-08-09 (Phase 11).

Legend:
- **Tracked?** Y = `git ls-files` reports it; N = untracked per `git status --short`
- **Needed for source?** Y = source code / authoritative content
- **Needed for build?** Y = build input required at compile time
- **Needed for audit evidence?** Y = audit trail / human review artifact (not reproducible)
- **Safe to delete?** Y = can be regenerated / downloaded / recreated from scratch
- **Size** (KB) approximate for files; **Count** of nested files for directories

---

| # | Path (relative to repo) | Type | Size/Count | Tracked? | Source needed? | Build needed? | Audit needed? | Safe delete? | Reason |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `.agent/` | dir | 3 | N | N | N | Y (agent log only) | N | Internal agent bookkeeping; LAST_RUN.md tracked by convention; keep |
| 2 | `.agent/LAST_RUN.md` | file | 1 | N | N | N | Y | N | Phase completion record; unique content |
| 3 | `.freebuff/` | dir | 2 | N | N | N | N | Y | IDE transient cache; fully regeneratable |
| 4 | `.git/` | dir | many | N/A (git dir) | N | N | N | N **NEVER DELETE** | Repository history database; catastrophic loss if removed |
| 5 | `.gitignore` | file | 2 | Y | Y | N | Y | N | Tracked source-ignore rules; authoritative |
| 6 | `.vscode/` | dir | 3 | N | N | N | N | Y | User-specific VS Code settings; regeneratable per-developer |
| 7 | `artifacts/` | dir | 1200+ | N | N | N | N (but keep *.md if any) | Y for generated subdirs; NOT Y for root — has subdirs with audit retention |
| 8 | `artifacts/backups/` | dir | 0 | N | N | N | Y if backup files present | Y | Database backup outputs; regeneratable by running backup-database.ps1 again |
| 9 | `artifacts/e2e/` | dir | 5+ | N | N | N | Partial | See rows 10-15 |
| 10 | `artifacts/e2e/.playwright-browsers/` | dir | 800MB+ | N | N | N | N | Y | Playwright Chromium browser binary; reinstallable via `playwright.ps1 install chromium` |
| 11 | `artifacts/e2e/results/` | dir | 5-20 .trx | N | N | N | Partial (keep .gitkeep) | Y for .trx only | Test result TRX files; rerun `Run-E2ETests.ps1` to regenerate. Keep placeholder `.gitkeep` |
| 12 | `artifacts/e2e/screenshots/` | dir | 0-50 png | N | N | N | Partial (keep .gitkeep) | Y for png/jpg only | Failure screenshots; rerun E2E regenerates. Keep placeholder `.gitkeep` |
| 13 | `artifacts/e2e/traces/` | dir | 0-20 zip | N | N | N | Partial (keep .gitkeep) | Y for trace zip only | Playwright traces; rerun E2E regenerates. Keep placeholder `.gitkeep` |
| 14 | `artifacts/e2e/videos/` | dir | 0-20 webm | N | N | N | Partial (keep .gitkeep) | Y for videos only | Opt-in test videos; rerun regenerates. Keep placeholder `.gitkeep` |
| 15 | `artifacts/e2e/logs/` | dir | 0-10 log | N | N | N | Partial (keep .gitkeep) | Y for log files only | Transient app logs; rerun regenerates. Keep placeholder `.gitkeep` |
| 16 | `artifacts/logs/` | dir | 0-50 log | N | N | N | N | Y | Scripted operation logs; rerun any script to regenerate |
| 17 | `artifacts/production-publish/` | dir | 0+ | N | N | N | N | Y | Release publish output; rerun `scripts/publish-iis.ps1` or `package-release.ps1` to regenerate |
| 18 | `artifacts/publish/` | dir | 500+ files (DLLs, wwwroot, runtimes) | N | N | N | N | Y | Same as 17 — publish/build output; regeneratable |
| 19 | `artifacts/testresults/` | dir | 0-20 trx | N | N | N | N | Y | Test output; rerun `dotnet test` regenerates |
| 20 | `artifacts/_probe/` | dir | 20+ | N | N | N | N | Y | Transient build probe artifact; delete bin/obj + probe dir |
| 21 | `audit/` | dir | 14 md files | Y (14 md) | Y | N | Y | N | Audit evidence directory. All .md files are authoritative source-correlated audit. DO NOT DELETE. |
| 22 | `audit/AUDIT_SUMMARY.md` | file | 7 | Y | Y | N | Y | N | Tracked audit summary |
| 23 | `audit/DATABASE_FINDINGS.md` | file | 5 | Y | Y | N | Y | N | Tracked findings |
| 24 | `audit/DEFECTS.md` | file | 24 | Y | Y | N | Y | N | Tracked defect register |
| 25 | `audit/PHASE1_SOURCE_INVENTORY.md` | file | 53 | Y | Y | N | Y | N | Tracked source inventory (Section H proof for 22 NOT-EXISTS) |
| 26 | `audit/REAUDIT_DR_RESULTS.md` | file | 5 | Y | Y | N | Y | N | Tracked re-audit |
| 27 | `audit/REAUDIT_RELEASE_DECISION.md` | file | 4 | Y | Y | N | Y | N | Tracked |
| 28 | `audit/REAUDIT_SECURITY_RESULTS.md` | file | 5 | Y | Y | N | Y | N | Tracked |
| 29 | `audit/REAUDIT_SUMMARY.md` | file | 5 | Y | Y | N | Y | N | Tracked |
| 30 | `audit/RELEASE_RECOMMENDATION.md` | file | 5 | Y | Y | N | Y | N | Tracked |
| 31 | `audit/REMEDIATION_REPORT.md` | file | 12 | Y | Y | N | Y | N | Tracked |
| 32 | `audit/SECURITY_FINDINGS.md` | file | 7 | Y | Y | N | Y | N | Tracked |
| 33 | `audit/TEST_RESULTS.md` | file | 7 | Y | Y | N | Y | N | Tracked |
| 34 | `audit/UI_FINDINGS.md` | file | 5 | Y | Y | N | Y | N | Tracked |
| 35 | `audit/CLEANUP_INVENTORY.md` | file | 15+ | N → Y (after creation) | Y | N | Y | N | This file, being created now |
| 36 | `audit.zip` | file | variable | N | N | N | Y | N (pending review) | Unique packaged evidence pending review. If content identical to audit/*.md dir, safe delete later. For now: LEAVE ALONE per Phase 11 rules. |
| 37 | `src/` | dir | 4 source projects | Y (all .cs/.csproj/.cshtml/.css/.js/.json) | Y | Y | N | N | Authoritative source code directory |
| 38 | `src/LivestockManager.Domain/` | dir | ~50 files | Y | Y | Y | N | N | Domain project; source |
| 39 | `src/LivestockManager.Domain/bin/` | dir | 50+ DLLs | N | N | N | N | Y | Build output; regeneratable via `dotnet build` |
| 40 | `src/LivestockManager.Domain/obj/` | dir | 20+ cache | N | N | N | N | Y | Build intermediate cache; regeneratable |
| 41 | `src/LivestockManager.Application/` | dir | ~100 files | Y | Y | Y | N | N | Application project; source |
| 42 | `src/LivestockManager.Application/bin/` | dir | 80+ | N | N | N | N | Y | Build output |
| 43 | `src/LivestockManager.Application/obj/` | dir | 30+ | N | N | N | N | Y | Build cache |
| 44 | `src/LivestockManager.Infrastructure/` | dir | ~150 files | Y | Y | Y | N | N | Infrastructure project; source |
| 45 | `src/LivestockManager.Infrastructure/bin/` | dir | 100+ | N | N | N | N | Y | Build output |
| 46 | `src/LivestockManager.Infrastructure/obj/` | dir | 40+ | N | N | N | N | Y | Build cache |
| 47 | `src/LivestockManager.Web/` | dir | ~400 files | Y | Y | Y | N | N | Web project; views, controllers, wwwroot source |
| 48 | `src/LivestockManager.Web/bin/` | dir | 300+ | N | N | N | N | Y | Build output |
| 49 | `src/LivestockManager.Web/obj/` | dir | 60+ | N | N | N | N | Y | Build cache |
| 50 | `src/LivestockManager.Web/App_Data/` | dir | 0+ | N | N | N | N | Y | Runtime uploads/proteced storage; NOT tracked; generated at runtime |
| 51 | `src/LivestockManager.Web/wwwroot/lib/` | dir | ~200 files (bootstrap, jquery, validation) | Y | Y | Y | N | N | Vendor JS/CSS libraries; checked in via LibMan or static. Keep unless regeneratable via manifest |
| 52 | `src/LivestockManager.Web/appsettings.json` | file | 2 | Y | Y | Y | N | N | Tracked base appsettings (Development defaults) |
| 53 | `src/LivestockManager.Web/appsettings.Development.json` | file | 2 | Y | Y | Y | N | N | Tracked dev config |
| 54 | `src/LivestockManager.Web/appsettings.Testing.json` | file | 1 | Y | Y | Y | N | N | Tracked test config |
| 55 | `appsettings.Production.example.json` | file | 2 | Y | Y | N | Y | N | Tracked example template for production values |
| 56 | `tests/` | dir | 4 test projects | Y (all .cs/.csproj) | Y | Y | N | N | Test projects |
| 57 | `tests/LivestockManager.UnitTests/` | dir | ~30 files | Y | Y | Y | N | N | Unit test project |
| 58 | `tests/LivestockManager.UnitTests/bin/` | dir | 100+ | N | N | N | N | Y | Build output |
| 59 | `tests/LivestockManager.UnitTests/obj/` | dir | 30+ | N | N | N | N | Y | Build cache |
| 60 | `tests/LivestockManager.IntegrationTests/` | dir | ~20 files | Y | Y | Y | N | N | Integration test project |
| 61 | `tests/LivestockManager.IntegrationTests/bin/` | dir | 100+ | N | N | N | N | Y | Build output |
| 62 | `tests/LivestockManager.IntegrationTests/obj/` | dir | 30+ | N | N | N | N | Y | Build cache |
| 63 | `tests/LivestockManager.ArchitectureTests/` | dir | ~15 files | Y | Y | Y | N | N | Architecture test project |
| 64 | `tests/LivestockManager.ArchitectureTests/bin/` | dir | 100+ | N | N | N | N | Y | Build output |
| 65 | `tests/LivestockManager.ArchitectureTests/obj/` | dir | 30+ | N | N | N | N | Y | Build cache |
| 66 | `tests/LivestockManager.EndToEndTests/` | dir | ~20 files | Y | Y | Y | N | N | E2E test project (Playwright) |
| 67 | `tests/LivestockManager.EndToEndTests/bin/` | dir | 200+ | N | N | N | N | Y | Build output + Playwright .playwright/ dir |
| 68 | `tests/LivestockManager.EndToEndTests/obj/` | dir | 50+ | N | N | N | N | Y | Build cache |
| 69 | `scripts/` | dir | 13 .ps1 files | Y | Y | N | N | N | Build/backup/restore/seed/test/e2e scripts |
| 70 | `scripts/*.ps1` (collective 13 files) | file | 40-60KB total | Y | Y | N | N | N | Tracked scripts |
| 71 | `docs/` | dir | 15+ .md files | Y | Y | N | Y | N | Authoritative documentation |
| 72 | `docs/IMPLEMENTED_FEATURES.md` | file | 15 | N → Y (after Phase 10a) | Y | N | Y | N | Created Phase 10a |
| 73 | `docs/ROADMAP.md` | file | 15 | N → Y (after Phase 10b) | Y | N | Y | N | Created Phase 10b |
| 74 | `docs/REPOSITORY_MAINTENANCE.md` | file | 10 | N → Y (after Phase 11d) | Y | N | Y | N | Created Phase 11d |
| 75 | `docs/ARCHITECTURE.md` | file | 28 | Y | Y | N | Y | N | Corrected Phase 10c |
| 76 | `docs/SECURITY.md` | file | 21 | Y | Y | N | Y | N | Corrected Phase 10c |
| 77 | `docs/DATABASE.md` | file | 37 | Y | Y | N | Y | N | Corrected Phase 10c |
| 78 | `docs/DEPLOYMENT.md` | file | 18 | Y | Y | N | Y | N | Corrected Phase 10c |
| 79 | `docs/E2E_TESTING.md` | file | 9 | Y | Y | N | Y | N | Corrected Phase 10d (ConnectionStrings__LivestockManagerDb) |
| 80 | `docs/TEST_PLAN.md` | file | 17 | Y | Y | N | Y | N | Corrected Phase 10c |
| 81 | `docs/USER_GUIDE.md` | file | 22 | Y | Y | N | Y | N | Corrected Phase 10c |
| 82 | `docs/ADMIN_GUIDE.md` | file | 21 | Y | Y | N | Y | N | Corrected Phase 10c |
| 83 | `docs/TRACEABILITY_MATRIX.md` | file | 19 | Y | Y | N | Y | N | Corrected Phase 10c (impl/not-impl columns) |
| 84 | `docs/MASTER_PLAN.md` | file | 12 | Y | Y | N | Y | N | Corrected Phase 10c |
| 85 | `docs/THREAT_MODEL.md` | file | 23 | Y | Y | N | Y | N | Corrected Phase 10c |
| 86 | `docs/ASSUMPTIONS.md` | file | 13 | Y | Y | N | Y | N | Corrected Phase 10c |
| 87 | `docs/REQUIREMENTS.md` | file | 21 | Y | Y | N | Y | N | Requirements document |
| 88 | `docs/CLOCK_VERIFICATION.md` | file | 1 | Y | Y | N | Y | N | Clock verification note |
| 89 | `docs/MOBILE_DEVICE_UAT_CHECKLIST.md` | file | 5 | Y | Y | N | Y | N | Mobile UAT checklist |
| 90 | `BUILD_STATUS.md` | file | 5 | Y | Y | N | Y | N | Root build status doc; corrected Phase 10c |
| 91 | `DEPLOY_CHECKLIST.md` | file | 5 | Y | Y | N | Y | N | Root deploy checklist; corrected Phase 10c |
| 92 | `CHANGELOG.md` | file | 3 | N → Y (after Phase 10b) | Y | N | Y | N | Created Phase 10b |
| 93 | `THIRD_PARTY_NOTICES.md` | file | variable | Y | Y | N | Y | N | OSS notices; keep |
| 94 | `LivestockManager.sln` | file | 5 | Y | Y | Y | N | N | Solution file; tracked; authoritative |
| 95 | `backup-database.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 96 | `build.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 97 | `clean.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 98 | `database-update.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 99 | `package-release.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 100 | `publish-iis.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 101 | `restore-database.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 102 | `run-dev.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 103 | `run-e2e-tests.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 104 | `run-local.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 105 | `seed-demo-data.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 106 | `smoke-test.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 107 | `test.cmd` | file | 1 | Y | Y | N | N | N | Thin wrapper; tracked |
| 108 | `TestResults/` (at repo root, inside tests, or elsewhere) | dir | 0+ | N | N | N | N | Y | VSTest/TRX output; regeneratable |
| 109 | `.vs/` | dir | 100+ files | N | N | N | N | Y | Visual Studio / Rider user cache; 100% regeneratable |
| 110 | `coverage/` (any depth) | dir | 0+ | N | N | N | N | Y | Code coverage report output; regeneratable |
| 111 | `coverage.*` files (coverage.xml, coverage.json, etc.) | file | 0+ | N | N | N | N | Y | Coverage tooling output; regeneratable |
| 112 | `*.user` files anywhere | file | 0+ | N | N | N | N | Y | User-specific project settings; regeneratable |
| 113 | `*.suo` files anywhere | file | 0+ | N | N | N | N | Y | Legacy VS user options; regeneratable |
| 114 | `*.bak` files anywhere | file | 0+ | N | N | N | N | Y (unless unique backup) | Stale DB backups unless recently produced intentionally. Rerun backup-database.ps1 to regenerate. Phase 11b: temporary zips inside repo |
| 115 | `*.trx` files anywhere (inside tests, artifacts, TestResults) | file | 0+ | N | N | N | N | Y (except .gitkeep) | Test run results; rerun tests to regenerate |
| 116 | `*.pfx` and `*.snk` files anywhere | file | 0+ | N | N | N | Y | Y (unless production signing) | Signing keys should NEVER be in repo; if present, delete immediately. Production keys stored externally |
| 117 | `appsettings.Production.json` (if present anywhere) | file | 0+ | N | N | Y (secrets) | N | Y (from tracking, keep out of git) | Real production secrets file; MUST NOT be tracked; delete from repo tree, store externally |

---

## Summary counts

- **Total rows:** 117
- **Safe delete = Y (generated output, deletable now):** ~60 entries
- **Safe delete = N (source, audit, tracked docs):** ~50 entries
- **Never delete:** `.git/`, audit/*.md tracked, src/**/*.cs, src/**/*.csproj, LivestockManager.sln, tests/**/*.cs, tests/**/*.csproj, scripts/*.ps1, docs/*.md, *.cmd wrappers, appsettings.*.json (except real Production with secrets), .gitignore
