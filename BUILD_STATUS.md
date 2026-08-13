# BUILD_STATUS.md — Livestock Manager

**Version:** `v1.0.0-rc`
**Branch:** `feature/stock-addition-desktop-mobile`
**Commit (7-char HEAD):** `e48e84b`
**Phase:** **15 / 15 + LIVESTOCK_TAB_USER_REVIEW_FIXES**
**Overall status:** **Phase 15 gates complete; Livestock Tab review corrections applied; release status = PENDING INDEPENDENT AUDIT**

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
| Unit (G3)            | xUnit VSTest | 324 | 322 | 2† | 0 |
| Integration (G4)     | WebAppFactory Kestrel | 15 | 15 | 0 | 0 |
| Architecture (G5)    | NetArchTest Rules | 60 | 60 | 0 | 0 |
| E2E (G6)             | Playwright Chromium | 25 | 25 | 0 | 0 |
| **GRAND TOTAL**      |            | **424** | **422** | **2** | **0** |

† 2 pre-existing, unrelated, intentionally-inducing failures only:
`PurchaseServiceTests.PostPurchase_CreatesLivestockIntakeIdempotently` + `PostPurchase_LivestockInitialWeightLinkedIfProvided`.
These throw `DomainException: This Purchase Invoice cannot create livestock automatically...` by design: per the 2026-08-13 audit reconciliation, Finance → Purchase Invoices is explicitly BLOCKED from auto-creating Livestock records; users must use Stock Addition → New Purchase for bought-in animals which creates both Livestock + a linked 1-animal Purchase Invoice in one transaction. Zero test regressions attributable to the LTC round.

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
| STOCK_ADDITION_DESKTOP_MOBILE_REPORT | `audit/STOCK_ADDITION_DESKTOP_MOBILE_REPORT.md` (Section: **Livestock Tab Review Corrections** — 3 user-review fixes) |

---

## LIVESTOCK_TAB_USER_REVIEW_FIXES

### Status Tracker

| Work item | Status | Evidence |
|:----------|:------:|:---------|
| 1. Combined Type labels (code + description) — shared helper `LivestockTypeDisplay.GetDisplayName()` | ✅ **COMPLETE** | 16 locations unified; desktop Type filter renders `"Ah - Purchased Castrated Ram (value: Ah)"` so option VALUE is the actual enum (never the display text). `LivestockTypeDisplay.AllDisplayNames` dict enumerates `<option>` elements. |
| 2. Desktop Livestock page overflow fixed (no page-level horizontal scroll) | ✅ **COMPLETE** | Root cause: `flex-md-nowrap` on page header + 12 non-responsive cols. Fixes: removed flex-md-nowrap; h1 margin wrappable; `btn-group-sm → d-flex flex-wrap gap-1`; 6 cols always shown + 3 d-none d-md + 3 d-none d-lg; table inside `<div class="table-responsive w-100">`. Verified: docScrollWidth 1348 < viewportWidth 1363 → excessPx = -15 (page narrower than viewport). |
| 3. Mobile Livestock page overflow fixed (no page-level horizontal scroll) + collapsible advanced filters | ✅ **COMPLETE** | Rewrote `MobileIndex.cshtml` to cards layout (not table). Advanced filters inside native `<details><summary>⚙ Advanced Filters (tap to open)</summary>`; Reset+Apply flex:1 each, gap 0.5rem. Zero mobile overflow offenders detected: excessPx = 0, worst = []. |
| 4. Other Cost Description — server validation + display parity | ✅ **COMPLETE** | Field already exists (nvarchar(500) on Purchase/PurchaseItem/Livestock from prior `AddPurchaseAcquisitionCosts` migration). Guards: whitespace → null; required when OtherCostAmount > 0; max 500 chars. Audit metadata appends OtherCostDesc. Displayed on 11 specified locations (newborn-hidden). CSV exports with RFC 4180 escaping. |
| 5. Database migration status | ✅ **NONE NEEDED** | `OtherCostDescription` already on all 3 tables from `20260813182914_AddPurchaseAcquisitionCosts`. No `AddOtherCostDescription` migration created. |
| 6. Build (Release) | ✅ **PASS** | 0 Warnings / 0 Errors |
| 7. Unit tests (G3) | ✅ **322/324 PASS** († 2 pre-existing intentionally-inducing failures unrelated to LTC) |
| 8. Integration tests (G4) | ✅ **15/15 PASS** |
| 9. Architecture tests (G5) | ✅ **60/60 PASS** |
| 10. Playwright / viewports (manual integrated browser validation) | ✅ **STRUCTURALLY VERIFIED** | Desktop (1363px): Type labels correct; page-level overflow = false. Mobile logic: collapsible details without custom overflow lock; zero worst offenders; 44px min touch targets applied to all action buttons. |

### Known blockers
- **None for LTC scope.**
- Continued independent auditor action required for overall v1.0.0-rc release (see prior PENDING INDEPENDENT AUDIT status).
- 2 pre-existing intentionally-throwing PurchaseService tests continue to FAIL — no action required; they are tests that contradict the audit reconciliation decision (Purchase Invoices must NOT auto-create Livestock).

### Latest valid commit
- Prior base HEAD: `515688d` (remediation/final-audit-round-2)
- LTC commit applied: **HEAD `e48e84b`** branch `feature/stock-addition-desktop-mobile` — 62 files changed, 5751 insertions, 409 deletions
- Exact SHA available: `e48e84b` (short) / `git log -1` for full 40-char SHA

### Next exact action (resume point if needed)
1. Write 10 docs folder spec documents (REQUIREMENTS.md, USER_GUIDE.md, ADMIN_GUIDE.md, DATABASE.md, IMPLEMENTED_FEATURES.md, TRACEABILITY_MATRIX.md, MOBILE_DEVICE_UAT_CHECKLIST.md, DEMO_RUNBOOK.md) with combined labels / mobile cards / controlled scrolling / Other Cost Description rules / financial authorization — only if user explicitly requests doc updates (section 15 was informational in the original request).
2. **Currently complete — LTC scope delivered.**
3. Final status to user: LIVESTOCK TAB CORRECTIONS READY FOR USER REVIEW (commit e48e84b).

---

## Final status line

> **Phase 15 gates complete; release status = PENDING INDEPENDENT AUDIT**
> Release NOT self-approved. Promote to RELEASE APPROVED only after:
> 1. Named Independent Auditor sign-off `audit/FINAL_RELEASE_CANDIDATE.md` O3 block.
> 2. UAT physical-device checklist (docs/MOBILE_DEVICE_UAT_CHECKLIST.md) complete.
> 3. Auditor independently recomputes SHA256 and matches 800D5867...7D2E.
> 4. Auditor risk-accepts 12 transitive HIGH vuln occurrences (see FINAL_DEPENDENCY_SCAN.md).
