# BUILD_STATUS.md — Livestock Manager

**Version:** `v1.0.0-rc`
**Branch:** `remediation/final-production-hardening`
**Commit (7-char HEAD):** `4ea7b78`
**Phase:** **15 / 15**
**Overall status:** **Phase 15 gates complete; release status = PENDING INDEPENDENT AUDIT**

> ⚠️ **IMPORTANT:** Release is **NOT APPROVED** — the pipeline does **NOT** self-approve. Release status is explicitly `PENDING INDEPENDENT AUDIT`. The Independent Auditor must manually sign off at `audit/FINAL_RELEASE_CANDIDATE.md` (signature block page 3, item O3) before status may be changed to `RELEASE APPROVED`.

---

## Phase 15 Gate Table — G1 through G11 (all gates executed 2026-08-09)

| Gate | ID | Description | Pass / Fail | Evidence |
|-----:|:--:|:------------|:-----------:|:---------|
|  1 | G1 | Git clean → commit with required message + `dotnet restore LivestockManager.sln` (exit 0) | ✅ **PASS** | HEAD `4ea7b78` message exactly `Phase 1-14 final hardening; pending gate execution`. Restore exit 0. |
|  2 | G2 | `dotnet build -c Release --no-restore` — 0 Warnings, 0 Errors | ✅ **PASS** | 0 W / 0 E, exit 0. |
|  3 | G3 | Unit tests `tests/LivestockManager.UnitTests` (Release nobuild norest) | ✅ **PASS** | 314 Discovered, 314 Passed, 0 Failed, 0 Skipped. |
|  4 | G4 | Integration tests `tests/LivestockManager.IntegrationTests` | ✅ **PASS** | 15 Discovered, 15 Passed, 0 Failed, 0 Skipped (static ctor fixes pattern #6 startup-fatal). |
|  5 | G5 | Architecture tests `tests/LivestockManager.ArchitectureTests` | ✅ **PASS** | 60 Discovered, 60 Passed, 0 Failed, 0 Skipped. |
|  6 | G6 | E2E `scripts/Run-E2ETests.ps1 -ServerInstance "."` (incl. 7 mobile viewports) | ✅ **PASS** | 25 Discovered, 25 Passed, 0 Failed, 0 Skipped. Wall ≈ 2 min 18 s. Exit 0. DB kept for DR. |
|  7 | G7 | PDF generation gate (6 PDFs: 1/18/25/50/unicode/receipt) | ✅ **PASS** | All ≥ 1000 bytes; headers valid; 25=3 pg, 50=5 pg (>1 req met); Unicode names/currency chars embedded. |
|  8 | G8 | SQL backup `scripts/backup-database.ps1` (LivestockManager_E2E_Gate → .bak) | ✅ **PASS** | Exit 0; 697 pages; .bak = 576,000 bytes (> 0; compressed). |
|  9 | G9 | SQL restore to `LivestockManager_E2E_Gate_Restored` | ✅ **PASS** | Exit 0; 3-table row counts = AspNetUsers 6/6, AspNetRoles 6/6, Livestock 0/0 all equal. |
| 10 | G10 | Publish `scripts/publish-iis.ps1 → artifacts/production-publish` | ✅ **PASS** | web.config exists. LivestockManager.Web.dll = 1,394,176 bytes (1.33 MB, non-empty). 93 DLLs. |
| 11 | G11 | Release ZIP `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` + SHA256 sidecar | ✅ **PASS** | ZIP = 23.4 MB (publish+docs+audit+scripts). SHA256 = 800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E. |

**GATES TOTAL:** 11 / 11 PASS. **Overall: 100% pass rate.**

---

## Test totals summary

| Suite (gate) | Framework | Discovered | Passed | Failed | Skipped |
|:-------------|:----------|:----------:|:------:|:------:|:-------:|
| Unit (G3)            | xUnit VSTest | 314 | 314 | 0 | 0 |
| Integration (G4)     | WebAppFactory Kestrel | 15 | 15 | 0 | 0 |
| Architecture (G5)    | NetArchTest Rules | 60 | 60 | 0 | 0 |
| E2E (G6)             | Playwright Chromium | 25 | 25 | 0 | 0 |
| **GRAND TOTAL**      |            | **414** | **414** | **0** | **0** |

New final test classes (G3):
- LivestockCodeAuthoritativeTests 3 / 3
- LivestockDocConsistencyTests 1 / 1
- PdfMultiPageTests 10 / 10
- PdfUnicodeTests 10 / 10
- ConnectionStringStandardizationTests 26 / 26
- ProtectedFileUploadValidationTests 31 / 31
- ProductionSeedHardeningTests 28 / 28
- DateTimeClockTests 4 / 4
- ProductionValidationScriptTests 5 / 5
- MobileViewport (G6) 7 / 7
**= 125 / 125 PASS final new-class tests**

---

## Key artifact paths

| Artifact | Absolute or relative path |
|:---------|:--------------------------|
| Publish IIS drop         | `artifacts/production-publish/` |
| Release ZIP              | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` (23.4 MB) |
| SHA256 sidecar           | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip.sha256` |
| **SHA256 hex (64)**      | `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E` |
| SQL backup .bak          | `artifacts/backups/LivestockManager_E2E_Gate_20260809_135627.bak` |
| PDF samples dir          | `artifacts/pdf-samples/` (6 files) |
| G6 E2E TRX + traces     | `artifacts/e2e/results/` |
| G3/G4/G5 TRX            | `artifacts/testresults/` |
| FINAL_HARDENING_REPORT  | `audit/FINAL_HARDENING_REPORT.md` |
| FINAL_RELEASE_CANDIDATE | `audit/FINAL_RELEASE_CANDIDATE.md` (status: PENDING INDEPENDENT AUDIT) |

---

## Final status line

> **Phase 15 gates complete; release status = PENDING INDEPENDENT AUDIT**
> Release NOT self-approved. Promote to RELEASE APPROVED only after:
> 1. Named Independent Auditor sign-off `audit/FINAL_RELEASE_CANDIDATE.md` O3 block.
> 2. UAT physical-device checklist (docs/MOBILE_DEVICE_UAT_CHECKLIST.md) complete.
> 3. Auditor independently recomputes SHA256 and matches 800D5867...7D2E.
> 4. Auditor risk-accepts 12 transitive HIGH vuln occurrences (see FINAL_DEPENDENCY_SCAN.md).
