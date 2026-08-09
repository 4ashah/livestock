# FINAL_RELEASE_CANDIDATE — Livestock Manager v1.0.0-rc

**Status:** **PENDING INDEPENDENT AUDIT** (NOT RELEASE APPROVED)
**Report date:** 2026-08-09
**Version tag candidate:** `v1.0.0-rc`
**Head commit:** `4ea7b78` (7 chars) on branch `remediation/final-production-hardening`
**Starting commit (per spec lines 1073-1104):** `ef1fac7`

---

## ⚠️ EXPLICIT RELEASE STATUS

**THIS RELEASE CANDIDATE IS: `PENDING INDEPENDENT AUDIT`**

- ❌ **RELEASE APPROVED** — NOT SET. Do NOT deploy to production.
- ❌ Self-approval applied? — **No.** Release approval strictly requires an independent third-party audit sign-off (human, external to dev/build pipeline).
- ✅ **Phase 15 GATES G1..G11 PASS** — All 11 gates PASS (see BUILD_STATUS.md).
- ✅ FINAL_RELEASE_CANDIDATE artifact written — yes, this file.

---

## Final release artifacts

| Artifact | Path / value |
|:---------|:-------------|
| Release ZIP           | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` |
| SHA256 sidecar        | `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip.sha256` |
| SHA256 hex (64 chars) | `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E` |
| Publish drop          | `artifacts/production-publish/` (IIS-ready, framework-dependent win-x64) |
| SQL backup (.bak)     | `artifacts/backups/LivestockManager_E2E_Gate_20260809_135627.bak` |
| PDF samples           | `artifacts/pdf-samples/` (6 files: 1/18/25/50/unicode/receipt) |
| E2E TRX + traces      | `artifacts/e2e/results/` |

**ZIP contents (release package, NOT source-only):**
- Root = IIS publish output (`LivestockManager.Web.dll 1.33 MB`, `web.config`, `appsettings.*.json`, 93 DLLs, App_Data/files empty dir)
- `docs/` — all markdown docs including MOBILE_DEVICE_UAT_CHECKLIST.md, SECURITY.md, DEPLOYMENT.md, OPERATIONS.md
- `audit/` — all FINAL_* reports R1-R10, FINAL_SECRETS_SCAN.md, FINAL_DEPENDENCY_SCAN.md
- `scripts/` — PowerShell deployment: publish-iis.ps1, backup-database.ps1, restore-database.ps1, Run-E2ETests.ps1, Generate-PdfSamples.ps1, Validate-ProductionConfig.ps1, Check-Prerequisites.ps1, package-release.ps1

## Open items (MUST resolve before status = RELEASE APPROVED)

### O1. UAT physical-device checklist — REQUIRES HUMAN

Mobile viewport matrix in G6 passed 7/7 in Chromium headless CI. Independent UAT on **physical hardware** is still required per `docs/MOBILE_DEVICE_UAT_CHECKLIST.md`. Items:

- [ ] iPhone 12/13/14 Pro (Safari, Chrome iOS 17+) — touch, scroll, safe-area, 4G latency
- [ ] Samsung Galaxy S23 / Pixel 8 (Android 14 Chrome) — pinch, split-screen, dark mode, font-size scale
- [ ] iPad Air 5 (Safari + Chrome iPadOS 17+) — split-view 1/3, 1/2, 2/3 ratios, Apple Pencil scrolling
- [ ] Pixel Fold / Galaxy Z Fold / Surface Duo — fold-unfold transitions, inner vs outer viewport swap
- [ ] Desktop Safari 17.x (MacBook M2) + Firefox 128 ESR (Windows) — non-Chromium parity QA

### O2. Transitive vulnerable package occurrences — 12 occurrences, 3 HIGH unique

From `audit/FINAL_DEPENDENCY_SCAN.md` (Phase 13):

| # | Package ID | Vulnerable versions | Severity | Transitive via | Occurrences |
|:-:|:-----------|:--------------------|:---------|:---------------|:-----------:|
| 1 | `Microsoft.Extensions.Caching.Memory` | 8.0.0  | HIGH | Identity UI / EF Core cache | 4/12 |
| 2 | `Microsoft.Build`  | 17.8.3 | HIGH | dotnet-aspnet-codegenerator-design (scaffolding tools) | 3/12 |
| 3 | `System.Text.Json` | 7.0.3 / 8.0.4 | HIGH | Azure SDK / CodeAnalysis / REST API SDKs | 5/12 |
| | **Totals** | | | | **12 occurrences** |

**Risk posture:** All 3 are HIGH severity *transitive* dependencies (not direct project PackageReferences).
Mitigations already present in Phase 1-14 work:
- `Microsoft.Extensions.Caching.Memory`: used only for in-proc tag-helper / identity UI cache; no untrusted external cache entry poisoning in scope
- `Microsoft.Build`: used only by `dotnet-aspnet-codegenerator-design.dll` — never loaded at runtime in IIS publish; scaffolding is build-time only (already verified by G5 architecture test)
- `System.Text.Json`: 8.0.4 already in use; advisory scoped to crafted depth + max-depth bypass — all LivestockManager controllers explicitly set `JsonSerializerOptions.MaxDepth = 32` AND request size limits via `[RequestSizeLimit]`

**Action required by Independent Auditor:** Sign off on risk acceptance or mandate 8.0.100+ SDK rebuild to pull transitive `System.Text.Json 8.0.12+`.

### O3. Independent audit sign-off — REQUIRED HUMAN

Release candidate **cannot** be promoted from `PENDING INDEPENDENT AUDIT` → `RELEASE APPROVED` without:
1. A named Independent Auditor (not the dev team, not the build author)
2. Auditor's sign-off reviewing:
   - This file + all FINAL_* reports (R1-R10)
   - `audit/FINAL_SECRETS_SCAN.md` (428 files, 0 PRODUCTION_RISK)
   - `audit/FINAL_DEPENDENCY_SCAN.md` (12 transitive HIGH occurrences — acceptance required)
   - Physical mobile UAT checklist completion (O1 above)
   - ZIP SHA256 manual recomputation and match against `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E`
3. Auditor writes:
   - Name, date, signature/PKI, explicit text: *"I approve promotion of commit 4ea7b78 (v1.0.0-rc) to RELEASE APPROVED for go-live."*

Audit sign-off location (to be completed by Auditor):
```
Auditor name: ______________________________________________
Auditor role: ______________________________________________
Date (YYYY-MM-DD): _________________________________________
Signature / PKI: ___________________________________________
Decision: ▢ RELEASE APPROVED   ▢ REJECT (see notes below)
Notes / conditions:
_____________________________________________________________
_____________________________________________________________
```

---

## Final references (convenience links within ZIP)

- `docs/DEPLOYMENT.md` — IIS deploy, AppPool ACL, appsettings.Production.json secrets setup
- `docs/OPERATIONS.md` — backup/restore runbooks, DR, SQL maintenance
- `docs/MOBILE_DEVICE_UAT_CHECKLIST.md` — O1 physical device checklist
- `docs/SECURITY.md` — vuln disclosure policy + responsible-contact
- `audit/FINAL_HARDENING_REPORT.md` — full Phase 1-15 hardening summary
- `audit/FINAL_SECRETS_SCAN.md` — secrets scan (428 files, 0 PRODUCTION_RISK)
- `audit/FINAL_DEPENDENCY_SCAN.md` — dependency scan (see O2)
- `audit/FINAL_DR_RESULTS.md` — G8 backup + G9 restore verification
- `audit/FINAL_TEST_RESULTS.md` — 414/414 PASS (314+15+60+25)
- `audit/FINAL_MOBILE_RESULTS.md` — 7/7 G6 mobile viewport PASS
- `audit/FINAL_PDF_RESULTS.md` — 6/6 PDF files (multi-page + Unicode)
- `audit/FINAL_PRODUCTION_CONFIG_RESULTS.md` — validator 12/12 PASS
- `audit/FINAL_RELEASE_CANDIDATE.md` — (this file)
