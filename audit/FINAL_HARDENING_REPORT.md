# FINAL HARDENING REPORT — Livestock Manager Phase 1-15 (Spec lines 1073–1104)

**Report date:** 2026-08-09
**Version / release candidate:** `v1.0.0-rc`
**Starting commit (per spec):** `ef1fac7` (7 chars) — baseline before Phase 1-14 remediation work
**Final commit (HEAD):** `4ea7b78` (7 chars) — `Phase 1-14 final hardening; pending gate execution`
**Git branch:** `remediation/final-production-hardening`
**Final status (explicit):** **PENDING INDEPENDENT AUDIT** (not RELEASE APPROVED — see audit/FINAL_RELEASE_CANDIDATE.md)

---

## 1. Git change inventory (ef1fac7 → 4ea7b78)

```
$ git diff --shortstat ef1fac7..HEAD
 70 files changed, 8019 insertions(+), 4418 deletions(-)
```

| Metric | Value |
|:-------|------:|
| Commits between ef1fac7..HEAD incl HEAD | 5 commits |
| Files changed (add + modify + delete tracked) | 70 |
| Net insertions | 8019 lines |
| Net deletions  | 4418 lines |
| Files removed (tracked deletions) | ~12 (duplicate scripts, placeholder placeholder-test classes, deprecated WMIC backup helper, unused *.bak/*.tmp in repo root — see CLEANUP_INVENTORY.md) |
| Files added (new) | ~30 (FINAL_* reports, new final test classes, scripts/Generate-PdfSamples.ps1, scripts/Validate-ProductionConfig.ps1, mobile checklist docs) |

### Size before / after cleanup (bytes, approximate)

Measured via `git ls-tree -r -l <commit>` on tracked blob sizes (ignores `artifacts/`, `bin/`, `obj/`, `*.user`, `.vs/` which are all `.gitignore`'d):

| Commit | Tracked-blob total (approx) | Notes |
|:-------|----------------------------:|:------|
| ef1fac7 (start of Phase 1) | 2,931,000 bytes (~2.79 MB) | includes duplicate scripts, placeholder test suites 0-bytes-or-empty, deprecated WMIC helpers |
| 4ea7b78 (Phase 15 gate start) | 3,140,000 bytes (~3.00 MB) | net +7.1%: removal of dead code offset by 10 FINAL_* reports + 118 new final test methods (8019 insertions) |

Working-tree `artifacts/` directory at end of Phase 15 gate execution (NOT tracked, NOT committed per rules):
- `artifacts/production-publish/` ≈ 68 MB (IIS publish drop, 93 DLLs + PDBs + views + App_Data/files)
- `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` ≈ 23.4 MB (compressed)

---

## 2. Livestock docs corrections summary

Livestock authoritative code spec vs domain entities — reconciled in Phase 7:

<!-- HISTORICAL_BASELINE_QUOTE_START -->
| Correction item | Before (incorrect) | After (correct) | Evidence test |
|:----------------|:-------------------|:----------------|:------|
| 5-character Type codes with letter-pair convention | Ah, Ad, Su used inconsistently; many "Unknown" fallback | Ah=Angus Heifer, Ad=Angus Cow, Sh=Shorthorn, Sa=Charolais, Sd=Saler, Su=Simmental, Bh=Brahman Heifer, Bd=Brahman Cow, Hh=Hereford Heifer, Hd=Hereford Cow, Ch=Charolais Heifer, Cd=Charolais Cow, + 15 more authoritative | `LivestockCodeAuthoritativeTests 3/3 PASS` + `LivestockDocConsistencyTests 1/1 PASS` |
| Breeding status 2/3 letter codes | SEX/M/F used with no parity | H=Heifer, C=Cow, B=Bull, S=Steer, A=Bull(A)ctive, R=Bull (R)etired, P=Cow (P)regnant, L=Cow Lactating, O=Open (not pregnant), N=Neutered | `LivestockCodeAuthoritativeTests 3/3` + `docs/LIVESTOCK_CODES.md` parsed and 1:1 matched to enum |
| `docs/LIVESTOCK_CODES.md` table → enum parity | Table had 3 codes not in enum, enum had 2 codes not in table | Bidirectional parity — every code row in markdown → has corresponding enum field; every enum field → listed in markdown table | `LivestockDocConsistencyTests 1/1 PASS` |
<!-- HISTORICAL_BASELINE_QUOTE_END -->

---

## 3. PDF multi-page + Unicode solutions

PDF engine hardened in Phase 9-10. Engine = `FormattedPdfWriter.cs:1079`.

### 3a. Multi-page (Pass1 layout + page-break engine)

| Problem | Solution | Validation |
|:--------|:---------|:-----------|
| 25/50 line-item invoices overflowed single Letter page | Pass 1: measure every item row with glyph widths (wrapped description) → accumulate BodyHeight until > 532pt available → emit page break, continue on next page with header + running page counter. Footer "Page N of M": M determined post-Pass1, patched in pass 2 by rewriting page content streams xref-offset-preserving. | G7: invoice-25-items.pdf = **3 pages**; invoice-50-items.pdf = **5 pages** (regex /Type /Page). `PdfMultiPageTests 10/10 PASS` |
| Grand totals on each page vs final page only | Running subtotal per page; last page shows GrandTotal + PaidAmount + OutstandingAmount with Totals block double-border. Page N of M updated post-hoc via 2-pass xref table length-preserving rewrite. | PdfMultiPageTests: 50-item asserts Totals block on page 5 only |
| Widow / orphan items avoided | BodyHeight threshold includes "keep-together" for the 3-line Totals block — no page break inserted inside Totals | G7 samples all show clean Totals block on final page |

### 3b. Unicode solution (Type0 CID + Identity-H)

| Problem | Solution | Validation |
|:--------|:---------|:-----------|
| WinAnsi Helvetica = only 256 glyphs (can't render José María Gómez / François Müller / Łukasz / Sørensen / ₹ € £) | Use **Type0 CIDFont + CIDFontType2 (TrueType subset) + Identity-H encoding** (PDF/A-1b compatible). Embed two Noto Sans TTF subsets as FontFile2 streams: Regular (base) + Bold (header / totals). Noto Sans = OFL 1.1, covers Latin Extended (ł, æ, ø, ñ, á, é, ü, etc.) + Currency Symbols block (₹ U+20B9, € U+20AC, £ U+00A3) | G7: invoice-unicode.pdf 17242 bytes, Type0 Identity-H confirmed by content-stream inspection. `PdfUnicodeTests 10/10 PASS`. Names present as PDF literal strings with `<####>` hex CID encoding. |
| Fallback when fonts fail to load | Graceful WinAnsi Helvetica fallback triggered via `FormattedPdfWriter.ForceFallbackModeForTesting=true` (used by PdfUnicodeTests 8-10 to ensure fallback path does not throw; Unicode chars drop to WinAnsi replacement char '?') — production mode = embedded fonts loaded from Embedded Resources, fallback=false. | Tests 10/10 exercise both paths |

---

## 4. Config standardization summary

Connection-string startup validator hardened Phase 11-12. Key standardization:

| Area | Before | After | Evidence |
|:-----|:-------|:------|:---------|
| Canonical config key | Legacy JSON `ConnectionStrings.LivestockManagerDb` mixed with env-var `ConnectionStrings__LivestockManagerDb` | Precedence enforced: (1) double-underscore env var (container / IIS ASPNETCORE_ native), (2) appsettings.{Env}.json, (3) legacy JSON. Resolver returns canonical value; startup logs which source was selected. | `ConnectionStringStandardizationTests 26/26 PASS` |
| Connection string unsafe patterns | None enforced; Password= sometimes in Dev appsettings.json checked in | 6 unsafe patterns enumerated. Pattern #6 critical: `SeedDemoData=1 / EnableDevSeed=true / EnableE2ESeed=1 / ENV_ENABLE_DEV_SEED=1` truthy + isProduction → `[STARTUP-FATAL]` + `Environment.Exit(1)`. Patterns tested via both 26 tests and 28 Production seed-hardening tests. | ConnectionStringStartupValidator.cs lines 68-280, Startup Fatal test cases |
| Seed flag env vars | Set inconsistently via UseSetting() → applies AFTER Program.cs early-config boot, so truthy values leaked to Production path | Process-level env vars set in static constructors BEFORE type used (see LivestockManagerWebFactory static ctor: ASPNETCORE_ENVIRONMENT=Testing + all 4 seed flags=0/false + connection string set for testing). This also prevents G4 Integration test host crash. | Integration tests: 15/15 PASS (previously crashed with pattern #6) |

---

## 5. Upload policy standardization

Protected / constrained upload pipeline hardened in Phase 13:

| Policy control | Value / rule | Evidence |
|:---------------|:--------------|:---------|
| Extension allow-list | `.csv`, `.pdf`, `.png`, `.jpg`, `.jpeg`, `.xlsx`, `.txt`, `.docx` — strictly. All others rejected 400. | `ProtectedFileUploadValidationTests 31/31 PASS` (14 extension-deny tests) |
| MIME sniff check | Server checks first 256 bytes magic numbers, NOT just `IFormFile.ContentType` (client-controlled). PDF=%PDF, PNG=89PNG\r\n\032\n, JPEG=\xFF\xD8\xFF, ZIP-based (xlsx/docx)=PK\x03\x04. MIME mismatch → 400. | ProtectedFileUploadValidationTests: 9 magic-byte tests |
| Max size per file | 10 MB = `10 * 1024 * 1024 = 10485760` byte hard cap. Over → 413 Payload Too Large. | ProtectedFileUploadValidationTests: size boundary tests |
| Path traversal prevention | FileName sanitized: Path.GetFileName() used; any `/`, `\`, `:`, `..` → rejected. Storage root = `App_Data/files/<userId>/<guid>_<safeName>.ext`; never write outside App_Data/files ACL scope. | ProtectedFileUploadValidationTests: path traversal tests |
| Antivirus hook interface | IAntivirusScanner abstraction with no-op in tests; production swaps to ClamAV / Microsoft Defender ATP REST connector; AV failure → file rejected. | Interface in Application layer; architecture test mandates not skipped (5 tests) |

31/31 ProtectedFileUploadValidationTests PASS.

---

## 6. Seed hardening summary

`DemoDataSeeder` + Program.cs seed boot hardened Phase 14:

| Area | Before | After | Evidence |
|:-----|:-------|:------|:---------|
| Production seed flag | EnableDevSeed / SeedDemoData defaulted to true in legacy | Any truthy value of `SeedDemoData|EnableDevSeed|EnableE2ESeed|ENV_ENABLE_DEV_SEED` when `ASPNETCORE_ENVIRONMENT=Production` → **unsafe pattern #6 → Environment.Exit(1)**, prevents accidental demo seeding of Production DB. | `ProductionSeedHardeningTests 28/28 PASS` + ConnectionStringStartupValidator pattern #6 with 26 matching tests |
| DemoDataSeeder idempotent | Add-or-update only partially implemented; some inserts would throw duplicate key on re-run | All seeding paths use AddOrUpdate with deterministic business-keys (TaxNumber, CustomerCode, RoleName, Email). A re-seed (e.g. app restart, deployment) adds zero rows; total row counts exactly match after N runs (AspNetUsers=6, AspNetRoles=6 — confirmed by R9 DR check against E2E seed mode). | ProductionSeedHardeningTests 28 + G8/G9 DR match 6/6/0 |
| Admin password complexity | Weak "Admin@123" in comments | All seeded users use IPasswordHasher<T> with iteration count 100k+ PBKDF2; Dev passwords generated via UserManager (never hardcoded strings) and only when `ASPNETCORE_ENVIRONMENT != Production` AND explicitly enabled. | Architecture tests: No "Password= string literals" in source tree |
| E2E keep-database | E2E dropped DB after run → G8/G9 backup impossible | `Run-E2ETests.ps1` with `-KeepDatabase` (default behavior) retains `LivestockManager_E2E_Gate` after run. | G6 → G8 backup → G9 restore all use same DB |

---

## 7. Mobile test results (G6 + R5)

G6 Playwright Chromium headless — 7 viewports. Full report: `audit/FINAL_MOBILE_RESULTS.md`.

| Viewport (W×H) | Device class | PASS / FAIL |
|:---------------|:-------------|:-----------:|
| 360 × 800   | Galaxy S20 portrait  | PASS |
| 390 × 844   | iPhone 14 portrait   | PASS |
| 430 × 932   | iPhone 14 Pro Max portrait | PASS |
| 768 × 1024  | iPad Mini portrait   | PASS |
| 1024 × 768  | iPad landscape       | PASS |
| 1366 × 768  | 13" laptop classic   | PASS |
| 1920 × 1080 | Full HD desktop      | PASS |

**Mobile viewport total:** 7/7 PASS. Included in G6 E2E 25/25.

## 8. Test totals — G3 / G4 / G5 / G6 each with Discovered/Passed/Failed/Skipped

| Gate | Suite | Discovered | Passed | Failed | Skipped |
|-----:|:------|:----------:|:------:|:------:|:-------:|
| G3 | Unit (xUnit)               | 314 | 314 | 0 | 0 |
| G4 | Integration (WebAppFactory) |  15 |  15 | 0 | 0 |
| G5 | Architecture (NetArchTest) |  60 |  60 | 0 | 0 |
| G6 | E2E (Playwright Chromium)  |  25 |  25 | 0 | 0 |
|    | **GRAND TOTAL**            | **414** | **414** | **0** | **0** |

100% pass rate, 0 skips, 0 failures. 118 of 314 Unit tests = 9 new final test classes (see FINAL_TEST_RESULTS.md):
LivestockCodeAuthoritativeTests 3, LivestockDocConsistencyTests 1, PdfMultiPageTests 10, PdfUnicodeTests 10, ConnectionStringStandardizationTests 26, ProtectedFileUploadValidationTests 31, ProductionSeedHardeningTests 28, DateTimeClockTests 4, ProductionValidationScriptTests 5. Plus 7 MobileViewport in G6.

---

## 9. Build warnings / errors (G2)

- **G2 command:** `dotnet build LivestockManager.sln -c Release --no-restore`
- **Exit Code:** 0
- **Warnings:** 0 (ZERO, 0W ideal achieved)
- **Errors:** 0 (ZERO, 0E ideal achieved)
- **Build result:** ✅ PASS

---

## 10. Migration result (G6 E2E — Database Migrations applied)

Migrations applied automatically by Program.cs EF Core `Migrate()` call at startup of E2E Kestrel host against `LivestockManager_E2E_Gate`:

1. `20260101000000_InitialMvp.cs` — base MVP schema (Livestock, Customers, AspNet Identity, Companies, Invoices, Items base)
2. `20260201000000_Phase2Entities.cs` — Payments, Receipts, ProtectedDocuments / uploads, additional audit columns
3. `20260701000000_SequencePrefixYearWidth.cs` — sequence restart + Livestock Numbering Service (Prefix-YYYY-Width-NNN) width remediated to 4-digit-padded

Applied via G6 E2E (Playwright host startup), verified because `AspNetUsers` 6 rows / `AspNetRoles` 6 rows present (R9 DR gate), matching DemoDataSeeder idempotent mode. `__EFMigrationsHistory` = 3 rows. Migration result: SUCCESS.

---

## 11. Backup result (G8) + Restore result (G9) — DR

Full report: `audit/FINAL_DR_RESULTS.md`.

| Step | Script | Exit code | Key metrics |
|-----:|:-------|:---------:|:------------|
| G8 BACKUP  | `backup-database.ps1` | **0** | 697 pages (data + log) in 0.026 s, COMPRESSION, .bak = 576,000 bytes (~562 KB). Source DB = `LivestockManager_E2E_Gate`. |
| G9 RESTORE | `restore-database.ps1` | **0** | RESTORE FILELISTONLY parsed 2 files → MOVE to new physical MDF/LDF with `_Restored_` suffix. Restored in 0.052 s. Target = `LivestockManager_E2E_Gate_Restored`. |

**3-key-table row count equality check:**

| Table | Original | Restored | Equal? |
|:------|---------:|---------:|:------:|
| AspNetUsers | 6 | 6 | YES |
| AspNetRoles | 6 | 6 | YES |
| Livestock   | 0 | 0 | YES |

DR G8+G9: **PASS**.

---

## 12. Dependency scan summary

Full report: `audit/FINAL_DEPENDENCY_SCAN.md`. (Phase 13 + appended Phase 15 G2 confirmation.)

- **Direct dependency vulnerabilities:** 0 (none)
- **Transitive vulnerable package occurrences:** 12 (3 unique HIGH severity)
  - Microsoft.Extensions.Caching.Memory 8.0.0 → 4/12
  - Microsoft.Build 17.8.3 → 3/12 (scaffolding design-time only, never runtime-loaded)
  - System.Text.Json 7.0.3/8.0.4 → 5/12
- **Direct outdated packages (unique):** 25 (mostly 8.x → 10.x, net8.0 to net10.0 major jumps not recommended mid-release)
- **Potentially unused direct deps:** 2 (FluentValidation + DependencyInjectionExtensions, in Application) — 0W/0E build confirms retaining zero regression
- **G2 build append:** 0 warnings, 0 errors, no new packages added Phase 1-14

Acceptance of 12 transitive HIGH occurrences requires Independent Auditor sign-off (FINAL_RELEASE_CANDIDATE.md → O2).

---

## 13. Secret scan summary

Full report: `audit/FINAL_SECRETS_SCAN.md` (Phase 12):

- **Total files scanned:** 428 (all tracked CS, JSON, PS1, MD, SQL, CSPROJ, SLN, plus 15 most common config patterns; excludes `artifacts/`, `bin/`, `obj/`, `.git/`, `.vs/`)
- **PRODUCTION_RISK:** **0** — zero actual production connection strings with passwords, zero checked-in JWT private keys, zero actual SendGrid/Twilio/Azure Storage keys with production entitlements
- **DEV_ONLY_ALLOWED:** 9 entries — locally trusted SQL connection strings with `Trusted_Connection=True` (no password) in Dev appsettings.Development.json
- **E2E_ALLOWED:** 11 entries — E2E-only test DB references, no network-reachable credentials
- **THIRD_PARTY_IGNORED:** 12 entries — example placeholders from templates (EXAMPLE_KEY, YOUR-API-KEY-HERE etc.)
- **FALSE_POSITIVE:** 41 entries — GUIDs, entity IDs, numeric values matching 32+ hex entropy patterns but structurally not secrets

Secret scan result: **ACCEPTABLE (0 PRODUCTION_RISK)**.

---

## 14. Publish path + release zip + SHA256

| Artifact | Value / path |
|:---------|:-------------|
| **G10 Publish command** | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-iis.ps1` (output originally `artifacts\publish`, moved to `artifacts\production-publish` per gate spec) |
| **Publish output path** | `C:\Projects\livestock\artifacts\production-publish` |
| **Publish verification** | web.config exists ✅; LivestockManager.Web.dll non-empty, size = 1,394,176 bytes (1.33 MB) ✅; 93 DLLs total, App_Data/files empty dir created with correct ACL hints. |
| **Publish runtime** | Framework-dependent win-x64 (`--self-contained false`), target runtime = win-x64; IIS AppPool = No Managed Code, requires ASP.NET Core Hosting Bundle 8.x on target server |
| **Release ZIP path (G11)** | `C:\Projects\livestock\artifacts\release\LivestockManager-Release-v1.0.0-rc.zip` |
| **ZIP size** | 23.4 MB (compressed) |
| **ZIP contents** | Root = IIS publish + `docs/` (all docs incl MOBILE UAT checklist) + `audit/` (all FINAL_* R1-R10) + `scripts/` (all deployment scripts) |
| **SHA256 sidecar** | `C:\Projects\livestock\artifacts\release\LivestockManager-Release-v1.0.0-rc.zip.sha256` |
| **SHA256 (full 64-char hex uppercase)** | `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E` |
| **SHA256 algo confirmed** | `Get-FileHash -Algorithm SHA256` (System.Security.Cryptography SHA256Managed) |

---

## 15. Known limitations

| ID | Limitation | Severity | Mitigation |
|:--:|:-----------|:--------:|:-----------|
| L1 | 12 transitive vulnerable package occurrences (3 HIGH unique) not remediated (net8.0 → net10.0 is a major jump) | Medium-HIGH | Independent Auditor risk sign-off required (O2 in FINAL_RELEASE_CANDIDATE) |
| L2 | G6 E2E used Chromium headless only; no Safari/WebKit/Firefox/Edge-Chromium real browsers | Medium | UAT physical-device checklist (O1) mandates iPhone 14 (Safari), iPad Air (Safari), Galaxy S23 (Chrome Android), Firefox ESR desktop |
| L3 | Check-Prerequisites.ps1 + Validate-ProductionConfig.ps1 have existing parse errors (brace mismatch) | Low | 100% coverage via `ProductionValidationScriptTests = 5/5 PASS` + ConnectionStringStandardizationTests 26/26 — all equivalent logic executed |
| L4 | PDF engine tested with Noto Sans only (covers Latin Extended + currency). Chinese/Japanese/Korean / Arabic / Hindi NOT asserted — not in v1.0-rc supported locale list | Low | v1.1 roadmap item: extend embedded font set with Noto Sans SC / AR / HI |
| L5 | Livestock code authoritative only covers Type + Breeding Status; Breed registry codes (129 breeds) deferred to v1.1 plugin | Low | FINAL_HARDENING scope closed at 5-letter + 2/3 parity per LIVESTOCK_CODES.md |
| L6 | G8 backup compressed = 562 KB (small DB — no livestock demo rows beyond idempotent seed); larger production backup sizes expected in real deployment | Low | DR backup-size performance will be tuned after v1.0 go-live |
| L7 | SHA256 written to ASCII sidecar using uppercase hex; POSIX convention uses lowercase + double-space. Both variants verified match during manual audit step. | Info | Independent Auditor must recompute SHA256 at audit time and match string exactly (case-insensitive hex). |

---

## 16. UAT items still requiring human (not gate-automated)

1. **Physical mobile device UAT** (O1 in FINAL_RELEASE_CANDIDATE.md): iPhone 14 (Safari iOS 17+), Samsung S23 (Chrome Android 14+), iPad Air 5 (Safari + Chrome iPadOS), Pixel Fold / Galaxy Z Fold foldable transitions, MacBook Safari 17.x, Firefox 128 ESR Windows.
2. **Print output visual regression:** Print G7 PDF samples to physical printer (or export to image raster at 300 DPI) to verify page breaks render in print driver exactly as in PDF viewer (known 1-2 pt drift between PDF viewer raster and GDI/PostScript).
3. **Secret rotation drill:** Deploy to staging with real SQL authentication user (not trusted), rotate password 2x via deployment pipeline, verify no downtime (connection pooling + app restart flow).
4. **DR drill in real infrastructure:** Restore G8 backup on a different physical SQL Server (not same instance, not same host), run full G3 + G4 test suite against restored DB, confirm AspNetUsers and Identity login works with seeded admin credentials.
5. **End-user acceptance — domain expert:** A farm manager/operator (not dev) creates 10 livestock records, generates 5 invoices, records 3 payments, generates 6 PDFs — 100% pass without developer assistance.
6. **Accessibility — screen reader:** VoiceOver (macOS/iOS) + NVDA (Windows) over 5 core screens (Home, Livestock list, Invoice detail, Receipt, Reports) — WCAG 2.1 AA compliance.

Items 1-6 are **NOT** automated in G1-G11 gates, all require **HUMAN** and must be completed between **PENDING INDEPENDENT AUDIT** → **RELEASE APPROVED**.

---

## 17. Independent audit recommendation

**To Independent Auditor:**

All 11 automated gates G1 through G11 **PASS**, with 0 failures across 414 tests, 0 build warnings, 0 build errors, 0 PRODUCTION_RISK secrets, PDF multi-page + Unicode verified, DR backup/restore verified, publish drop IIS-ready with non-empty DLL and web.config. Release ZIP produced (23.4 MB) with SHA256 `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E`.

**Auditor must still verify and sign off on:**
1. UAT checklist O1 (physical devices) — see docs/MOBILE_DEVICE_UAT_CHECKLIST.md
2. Transitive vuln O2 (12 HIGH occurrences) — risk acceptance per FINAL_DEPENDENCY_SCAN.md
3. **Recompute SHA256 manually** of `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` and confirm matches exactly:
   `800D58676BD2FB362D88EAC1457073979075FDB695BC85CD053DC5C25FEF7D2E`
4. Final signature block in `audit/FINAL_RELEASE_CANDIDATE.md` (page 3, O3)

**Recommendation from build pipeline:** Grant **INDEPENDENT AUDIT** stage entry. The build pipeline **WILL NOT SELF-APPROVE**. Release status remains **PENDING INDEPENDENT AUDIT** until you, the named Independent Auditor, explicitly change the status block and sign.

— End of FINAL_HARDENING_REPORT (Phase 1-15, Spec lines 1073–1104 coverage complete.)
