# ROUND2_TREE_CLASSIFICATION.md — File Classification (2026-08-09)

Base commit: `4ea7b78` (Phase 1-14 final hardening; pending gate execution)
Classified by: ROUND2 Phase A inspection
Classification categories:
- **Valid source change** — tracked .cs/.cshtml/.csproj/.json app code relevant to build/runtime
- **Valid test change** — tracked test source in tests/**
- **Valid script change** — tracked .ps1/.cmd scripts in scripts/** or repo root
- **Valid textual audit evidence** — audit/*.md markdown evidence (NEVER deleted)
- **Generated artifact** — outputs of build/test/publish (should be .gitignore'd)
- **Temporary file** — tooling caches, editor state, transient
- **Unwanted binary** — tracked binary that should not be committed (audit.zip, fake font placeholders)

---

## Files Modified (Tracked Working-Tree Changes, git status " M")

| Relative path | Classification | Notes |
|:--------------|:---------------|:------|
| `.agent/DEFECTS.md` | Valid textual audit evidence | Agent defect log; historical + current defects; retained |
| `.agent/LAST_RUN.md` | Valid textual audit evidence | Agent last-run timestamped audit record; retained |
| `.agent/STATE.md` | Valid textual audit evidence | Agent pipeline state; retained |
| `.agent/TASKS.json` | Valid textual audit evidence | Agent task plan; retained |
| `BUILD_STATUS.md` | Valid textual audit evidence | Top-level release gate status table; retained |
| `audit/FINAL_DEPENDENCY_SCAN.md` | Valid textual audit evidence | Appended G2 0W/0E build confirmation; retained |
| `tests/LivestockManager.IntegrationTests/LivestockManagerWebFactory.cs` | Valid test change | Static ctor fix (D-010) for pattern #6 Startup-Fatal crash; critical; PRESERVED |

---

## Files Untracked (git status "??")

| Relative path | Classification | Notes |
|:--------------|:---------------|:------|
| `.vscode/` | Temporary file | Editor workspace state (launch.json, tasks.json); NOT staged; covered by .vs/ pattern already; keep .gitignore'd |
| `audit/FINAL_DR_RESULTS.md` | Valid textual audit evidence | G8/G9 backup+restore gate report; STAGED |
| `audit/FINAL_HARDENING_REPORT.md` | Valid textual audit evidence | Phase 1-15 70-file inventory hardening report; STAGED |
| `audit/FINAL_MOBILE_RESULTS.md` | Valid textual audit evidence | G6 7-viewport mobile matrix report; STAGED |
| `audit/FINAL_PDF_RESULTS.md` | Valid textual audit evidence | G7 6-sample PDF report (Unicode + multi-page); STAGED |
| `audit/FINAL_PRODUCTION_CONFIG_RESULTS.md` | Valid textual audit evidence | G10 production config validation; STAGED |
| `audit/FINAL_RELEASE_CANDIDATE.md` | Valid textual audit evidence | Release candidate = PENDING INDEPENDENT AUDIT; NOT self-approved; STAGED |
| `audit/FINAL_TEST_RESULTS.md` | Valid textual audit evidence | G3/G4/G5/G6 414/414 test totals; STAGED |
| `scripts/Generate-PdfSamples.ps1` | Valid script change | G7 PDF sample throwaway-console generator; PRESERVED |

---

## Tracked Binaries / Other Known Assets (on disk or in index)

| Relative path | Classification | Notes |
|:--------------|:---------------|:------|
| `audit.zip` (tracked in index) | **Unwanted binary** | 330,737,449 bytes (~315 MB) compressed binary; must be `git rm --cached` + deleted from worktree (see Phase C); NO historical audit evidence content is inside — audit evidence lives in audit/*.md |
| `src/LivestockManager.Infrastructure/Resources/Fonts/NotoSans-Regular.ttf` (tracked) | **Unwanted binary** (placeholder) | 169 bytes only — NOT a genuine font; must be deleted + replaced with validator + fallback |
| `src/LivestockManager.Infrastructure/Resources/Fonts/NotoSans-Bold.ttf` (tracked) | **Unwanted binary** (placeholder) | 169 bytes only — NOT a genuine font; same action |
| `src/LivestockManager.Infrastructure/Resources/Fonts/OFL.txt` (if tracked) | Valid textual audit evidence | SIL Open Font License; if it is an empty placeholder, delete; retain if genuine text |

---

## Artifact Directories (All Generated / .gitignore'd — NOT staged)

- `artifacts/bin/`, `artifacts/obj/` → Generated artifact (covered by **/bin/ **/obj/)
- `artifacts/production-publish/` → Generated artifact (publish output)
- `artifacts/release/LivestockManager-Release-v1.0.0-rc.zip` → Generated artifact
- `artifacts/backups/*.bak` → Generated artifact (SQL backup)
- `artifacts/e2e/results/*.trx`, `screenshots/`, `traces/`, `videos/`, `.playwright-browsers/` → Generated artifact
- `TestResults/`, `testresults/` → Generated artifact
- `bin/`, `obj/` under any project → Generated artifact
- `.vs/` (IDE state) → Temporary file

---

## Disposition Summary for Phase I Commit

**Stage (valid changes only — PRESERVED):**
1. All 7 " M" tracked modifications above (source/test/audit-text)
2. All 7 "??" audit/FINAL_* markdown reports (valid textual audit evidence)
3. scripts/Generate-PdfSamples.ps1 (valid script)
4. Phase C/D/E/F/G new source: TrueTypeFontValidator.cs + RepositoryHygieneTests.cs + TrueTypeFontValidatorTests.cs + updated LivestockDocConsistencyTests.cs + .gitignore updates + updated backup-database.ps1 + updated FormattedPdfWriter.cs + updated .agent/* files + updated THIRD_PARTY_NOTICES.md + ROUND2_* markdown evidence created in Phases A/C/H

**DO NOT stage:**
- audit.zip (removed from index + worktree in Phase C)
- The two 169-byte fake NotoSans-*.ttf placeholders (deleted in Phase E)
- .vscode/ (temp editor state; already .gitignore'd via .vs/ family)
- artifacts/** (all generated; already .gitignore'd)
- bin/ obj/ .vs/ TestResults/ App_Data/ (all .gitignore'd)
