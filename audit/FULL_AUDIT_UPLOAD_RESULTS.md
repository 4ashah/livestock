# FULL AUDIT — Upload + Storage Security Audit (Section 17)

**Audit Section:** 17  
**Status:** COMPLETED — Static code audit of ProtectedFileUploadValidator + ProtectedDocumentStorage + DocumentsController authorization triad  
**Auditor:** Independent Auditor

---

## 17.A Upload Configuration Canonical Values (from source code, not spec)

Code values read from `ProtectedFileUploadValidator.cs` L19-20:

| Config Item | Canonical Code Value | Spec Template Placeholder | Mismatch? |
|---|---|---|---|
| Allowed Extensions (9) | `.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.csv` | Same 9 | ✅ Match |
| Extension Case Sensitivity Mode | `StringComparer.OrdinalIgnoreCase` (case-insensitive) | Case-insensitive expected | ✅ Match |
| **Max File Size (bytes)** | `MaxFileSize = 26214400` (= **25.0 MB** exactly: 25 × 1024 × 1024) | Placeholder in spec template header: 10,485,760 (= 10 MB) | ⚠️ **GAP** — Code allows 25 MB; spec template says 10 MB. Auditor recommends 25 MB acceptable for scanned 50-page PDFs; change spec limit to 25 MB or reduce config to 10 MB (LOW mismatch only; security boundary still reasonable) |

---

## 17.B Authorization: DocumentsController Actions — Policy Audit

| Endpoint | HTTP | Actual Policy Attribute on Action | REQUIRED Policy per Financial Classification | Status |
|---|---|---|---|---|
| UP-CTRL-1 Upload (POST form file) | POST | `[Authorize(Policy = CanViewFinancialData)]` | CanViewFinancialData (Financial docs: Invoices/Receipts attachments) | ✅ **PASS** — Correctly finance-gated |
| UP-CTRL-2 List documents | GET | `[Authorize(Policy = CanViewOperationalData)]` | ❌ SHOULD BE CanViewFinancialData (same financial docs) | ❌ **FAUD-0010 HIGH** — Financial document listings viewable by Viewer/DataEntry/FarmManager who don't have finance role. |
| UP-CTRL-3 Download document (GET id) | GET FileResult | `[Authorize(Policy = CanViewOperationalData)]` | ❌ SHOULD BE CanViewFinancialData | ❌ **FAUD-0010 HIGH** — Same root. Can download Invoice.PDF / Receipt.PDF files without financial-data authorization. |
| UP-CTRL-4 Delete document | POST/Delete | CompanyAdministrator or SysAdmin policy | Correct for destructive | ✅ PASS |
| UP-CTRL-5 Storage Save — ProtectedDocumentStorage.SaveAsync(file, companyId) | Internal Storage Layer | Takes `companyId` parameter, stores under company GUID subfolder | N/A internal | ✅ Company-scoped folders used |
| UP-CTRL-6 Storage Download — 2-stage isolation check (see 17.E) | Internal | 2 checks both pass | N/A internal | ✅ PASS |

---

## 17.C Positive Tests — Allowlist Valid Uploads

| File # | File Description | Extension | Magic Bytes Present in Validator? | Validator Outcome (per code-path trace) | Status |
|---|---|---|---|---|---|
| UP-1 Valid Invoice PDF header `%PDF-1.4` first 4 bytes = `25 50 44 46` | .pdf | ✅ PDF magic `25504446` bytes check implemented L242 → `fileBytes[0..3] == [0x25,0x50,0x44,0x46]` → PASS | ✅ Allowed | ✅ PASS |
| UP-2 Valid JPEG photo of cattle SOI `FF D8 FF` (marker 0xFFD8FFE0 etc.) | .jpg | ✅ JPEG check L256: `[0] == 0xFF && [1] == 0xD8 && [2] == 0xFF` → valid start; trailer check FFD9 (FAUD-0028 issue) | ✅ Allowed — SOI marker pass | ✅ PASS (trailer FAUD-0028) |
| UP-3 Valid PNG 8-byte header `89 50 4E 47 0D 0A 1A 0A` | .png | ✅ PNG check L265: exact 8-byte compare to canonical PNG signature | ✅ Allowed | ✅ PASS |
| UP-4 Valid DOC (Old Word) Compound Binary File header CFBF 0xD0CF11E0A1B11AE1 | .doc | ✅ OLE/CFBF header 8 bytes match L280: first 8 bytes == [0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1] → OLE valid | ✅ Allowed | ✅ PASS |
| UP-5 Valid DOCX (OOXML ZIP): `PK 03 04` (50 4B 03 04) header + ZipArchive open → MUST contain entry `[Content_Types].xml` (not empty / not fake ZIP) | .docx | ✅ OOXML check L326-371: opens ZipArchive read-safe; checks `.Entries.Any(e => e.FullName == "[Content_Types].xml")`; Entries count < 256; max single entry size < 1 MB (zip bomb guard); fails if PK\003\004 present but inner structure invalid | ✅ Allowed (genuine docx) | ✅ PASS (deep OOXML structure validation) |
| UP-6 Valid XLS (Old Excel) same CFBF header | .xls | ✅ Same OLE validator reused → pass | ✅ Allowed | ✅ PASS |
| UP-7 Valid XLSX (OOXML ZIP) same DOCX pattern | .xlsx | ✅ Same OOXML ZipArchive check → Entries contains [Content_Types].xml → pass | ✅ Allowed | ✅ PASS |
| UP-8 Valid CSV UTF-8 text no NUL bytes, comma separated | .csv | ✅ CSV specific checks L373-429: no 0x00 NUL bytes anywhere; UTF-8 without BOM accepted; printable ASCII + Unicode allowed | ✅ Allowed (text CSV) | ✅ PASS |
| UP-9 Uppercase `.PDF` extension test case-insensitive | .PDF | ✅ Extension comparer `OrdinalIgnoreCase` → treated same as .pdf → PDF magic check applied → pass | ✅ Allowed case-insensitive | ✅ PASS |
| UP-10 Unicode original filename "José factura №1.PDF" stored as GUID.pdf; original filename preserved only in DB metadata | .pdf (Unicode display name) | ✅ Storage.SaveAsync: `InternalFileName = Guid.NewGuid("N") + ".pdf"`; `OriginalDisplayName = SanitizeDisplayName(originalName)`; no user control over internal file on disk path | ✅ Allowed; display name stored sanitized | ✅ PASS |

**Positive Tests: 10/10 PASS ✅**

---

## 17.D Negative Rejection Tests — MUST FAIL CLOSED

| File # | Attack Scenario | Extension | Expected | Validator Code Trace → Outcome | Status |
|---|---|---|---|---|---|
| UP-11 Executable `invoice.exe` direct | .exe | ✅ REJECT hard | ✅ Extension not in 9-item allowlist → early short-circuit return false L52 | ❌✅ Correctly rejected | ✅ PASS |
| UP-12 Double extension masquerade `invoice.pdf.exe` | .exe final | ✅ REJECT final extension is .exe not allowlisted | ✅ Path.GetExtension returns last segment = ".exe" — not in allowlist → rejected | ✅ PASS |
| UP-13 Double extension `invoice.pdf.cmd` | .cmd final (batch script) | ✅ REJECT | ✅ Same GetExtension final → ".cmd" not allowlisted | ✅ PASS |
| UP-14 XSS upload `steal-cookies.html` JS payload | .html | ✅ REJECT not in list | ✅ ".html" not in 9-item list | ✅ PASS |
| UP-15 **MZ EXECUTABLE HEADER MAGIC BYTES MISMATCH** → actual DOS/Windows executable with bytes `4D 5A` (MZ) renamed to `.pdf` to try bypass extension allowlist | .pdf (renamed) | ✅ MUST REJECT even though extension is .pdf, magic bytes are EXE not PDF | ✅ Lines 225-232: **MZ executable guard** — Before checking magic per extension, runs `if (firstTwo == 0x4D5A /* MZ */ && extensionNotDocxNorXls) { reject }`. This catches renamed .exe → .pdf before it even gets to PDF-specific magic. 4D 5A MZ header correctly rejected. | ✅ PASS (MZ guard works) |
| UP-16 Fake DOCX ZIP without `[Content_Types].xml` inside. Actually plain ZIP renamed to invoice-attach.docx (could contain script) | .docx (renamed fake OOXML) | ✅ REJECT inner OOXML-structure-invalid | ✅ Lines 326-371 ZipArchive opened; `if (!entries.Any(e => e.FullName == "[Content_Types].xml")) return false`. → fake OOXML correctly rejected. Additional guard: entries ≤ 256, single-entry ≤ 1 MB to prevent zip-bomb. ALL FAKEs caught. | ✅ PASS (deep inner structure check excellent) |
| UP-17 NUL bytes inside CSV — malicious binary `payload.dat` renamed to data.csv containing 0x00 0x00 0x00 early | .csv (renamed) | ✅ REJECT because CSV must be plain text — no NULs | ✅ Lines 373-429 CSV-specific scan: walks full file; `if (bytes.Any(b => b == 0x00)) { // NUL in CSV — reject }` — NUL-CSV protection actively engaged. Correctly rejected. | ✅ PASS |
| UP-18 Zero-byte empty file `empty.pdf` 0 bytes | .pdf | ✅ REJECT or warn — cannot be valid PDF | ✅ First guard in validator: `if (bytes.Length < 4) return false;` (length check L48). 0 bytes triggers short-circuit false. Correctly rejected. | ✅ PASS |
| UP-19 > Max size file 26,214,401 bytes (25 MB +1 byte) oversized invoice-50mb-scan.pdf | .pdf | ✅ REJECT oversize | ✅ L50: `if (file.Length > MaxFileSize) return false;` → correctly rejected for > 25,000,000 boundary. | ✅ PASS |
| UP-20 Path traversal filename `../../../../Windows/System32/calc.exe.pdf` — attempt to escape storage root via relative parts | .pdf (display name) | ✅ REJECT traversal OR internal filename GUID so path parts ignored (both acceptable) | ✅ Lines 151-165: ContainsPathTraversal() checks literal `../` or `..\` → if found → SanitizeDisplayName() replaces; THEN applies `Path.GetFileName(userSupplied)` LAST to strip any directory components even after sanitization. Layered defense. Internal filename always GUID-based and never uses user input. | ✅ PASS (no traversal possible; internal names random GUIDs anyway) |
| UP-21 Invalid characters in display filename `Bad<Name>?|*`:smile.pdf | .pdf | ✅ Invalid chars replaced not filename crash | ✅ Lines 121-149: Invalid regex `[<>:"/\\|?*\0-\31]` → each match replaced with `-` character → final display name sanitized printable only. | ✅ PASS |
| UP-22 Leading dot-space filename trick `.hidden.pdf ` → attempts to make Unix dotfile hidden or NTFS alternate stream | .pdf | ✅ Trimmed; never allowed as internal storage name | ✅ Lines 167-196 SanitizeDisplayName: .TrimStart('.', ' ') → no leading dots; Trim() both ends whitespace → cleans before DB write. Internal storage name always GUID so this is defense-in-depth display name clean only. | ✅ PASS |
| UP-23 Display name length 5,000 characters DoS | .pdf | ✅ Cap 255 chars | ✅ L182 length guard: `.Substring(0, Math.Min(length, 255))` → cap 255 | ✅ PASS |
| UP-24 Cross-company document id forgery in URL parameter `/Documents/Download?id=company-B-doc-guid` → attempt to read Company B's file from Company A login | GUID internal | ✅ 403 UnauthorizedAccessException; cannot download cross-co | ✅ 17.E two-stage isolation: 1) `document.CompanyId == user.CompanyId` → 2) `fullPath.StartsWith(companyRootFolder, Ordinal)` → both enforced. If Company B's GUID passed to A's Download: 1 fails → NotFound or 403. Even if attacker somehow has full path leaked → 2 fails because companyRootFolder = `storageRoot + "\\" + companyGuid.ToString("N")` (different company GUID subfolder name different) so fullPath B not under root A. Dual independent isolation. | ✅ PASS (2-stage excellent) |
| UP-25 Storage path enumeration: `/Documents/Download?id=../../../AppSettings.json` (path-traversal-in-id though id parameter is supposed to be Document-GUID not path — defense in depth) | N/A | ✅ Cannot traverse; id is Document PK not path; Storage.LoadAsync takes document.InternalFileName not URL path segments | ✅ Download controller flow: id → DocumentsService.GetById(id, companyId) → NotFound if cross-co; returns Document entity with .InternalFileName (GUID.pdf) THEN storage.Load(internalFilename, companyId) → company subfolder append → NEVER uses user id parameter as filesystem path. Zero file path exposure in URL route semantics. | ✅ PASS |

**Negative Rejection Tests: 15/15 PASS ✅**

---

## 17.E Cross-Company Storage Isolation — 2-Stage Deep Dive

`ProtectedDocumentStorage.DownloadAsync(document, userCompanyId)` implementation (lines 153-186):

| Stage # | Check Code | Check Description | Attack Defeated If Check Alone Failed | Status |
|---|---|---|---|---|
| Stage 1 | `if (document.CompanyId != userCompanyId) throw new UnauthorizedAccessException("Document not found.")` | Company ID FK on Document entity must equal logged-in user CompanyId | Stolen/forged Document GUID URL id=otherCoDoc → blocked | ✅ Stage 1 PASS |
| Stage 2 | `string companyRootFolder = Path.Combine(_storageRootPath, userCompanyId.Value.ToString("N"));` then `if (!fullPath.StartsWith(companyRootFolder, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Storage path mismatch.")` | Final resolved on-disk absolute path must start with user's company-specific GUID subfolder (N format: 32 hex digits no dashes). Even if DB row compromised Stage 1 false-pass, disk path cannot lie. | DB-integrity attack: attacker modifies document.CompanyId via bug (unlikely) → Stage 2 catches because actual file still stored under original company folder not attacker's company folder | ✅ Stage 2 PASS |
| Stage X (bonus) | InternalFileName = `Guid.NewGuid("N") + ext` always; never user input based | Predictable filenames? 122 bits entropy (Guid v4 N ≈ 122 bits random). Path.GetRandomFileName alternative referenced at FAUD-0029 LOW (different entropy method but both cryptographically strong). | Filename guessing / brute force download enumeration | ✅ Effectively unguessable regardless; FAUD-0029 noted LOW preference diff only |

**Conclusion: Cross-company storage isolation 3 independent layers (Policy + Entity CompanyId FK + Disk Path Prefix StartWith + Random Filenames = 4 layers total). Correctly hardened.**

---

## 17.F Minor Gap Issues (FAUD IDs Already Classified Section 24)

| ID | Location | Description | Severity Classification |
|---|---|---|---|
| **FAUD-0028 LOW** | ProtectedFileUploadValidator JPEG trailer check | Lines 260-275: JPEG trailer marker bytes `FF D9` at end of file ARE read into variable `trailerBytes`; comparison statement written as `if (trailerBytesMatch) { /* EMPTY conditional block */ }` — NO enforcement action inside the if-block. Code reads the EOI marker but then silently does nothing with the result. If JPEG lacks valid FFD9 EOI (truncated image), validator accepts it anyway. Low-severity: doesn't cause exploitability, means we accept truncated JPEG uploads that may fail to render fully for recipients. | LOW (no security boundary broken; UX only) |
| **FAUD-0029 LOW** | ProtectedDocumentStorage line 110 internal filename generator | Uses `Guid.NewGuid("N") + extension` to produce internal filename like `116f5c51fdd84...ab.pdf`. Guid v4 has 122 bits of entropy so functionally unguessable and acceptable for security. Alternative per spec template was `Path.GetRandomFileName` which uses RNGCryptoServiceProvider 8 chars base32 = 40 bits (less entropy). Guid is actually STRONGER, so LOW only "not same method as spec" — zero actual risk. Auditor rates as PASS-classified LOW documentation note only; no actual weakness. | LOW (actually better spec; cosmetic only) |
| **FAUD-0010 HIGH** | DocumentsController.List + Download [Authorize] policies wrong | Described UP-CTRL-2 / UP-CTRL-3 earlier. Functional classification HIGH (financial-data authorization bypass for VIEW + DOWNLOAD actions). | HIGH (policy wrong; storage layer Stage 1/2 still protect cross-co) |

---

## Upload + Storage Security Audit Summary

| Check Group | Total | PASS | HIGH FAUD-0010 Policy | LOW Gaps (FAUD-0028/29) |
|---|---|---|---|---|
| Positive Allowlist Uploads 10 files | 10 | 10 | 0 | 0 |
| Negative Rejection / Attacks 15 scenarios | 15 | 15 | N/A correct rejections | 0 |
| Authorization Policies (5 controller actions) | 5 | 3 (Upload/Delete + Storage layers) | 2 (List/Download — FAUD-0010) | 0 |
| Cross-Company Storage Isolation (2-stage + filename) | 3 stages | 3 | 0 | 0 |
| File size / extension / magic / inner-structure validators | 7 layers (Extension + MZ-guard + Magic per type + OOXML-structure + CSV-NUL + MinLength + MaxSize) | All 7 implemented correctly | 0 | 1 (JPEG trailer FAUD-0028) |
| Sanitization + Path handling (Traversal / Invalid chars / Length cap / Trim) | 4 validators | 4 | 0 | 1 (Filename gen diff spec FAUD-0029) |
| Configuration (max size mismatch) | 1 item | — (acceptable gap) | 0 | 1 (25 MB vs 10 MB LOW mismatch doc-only) |

**Total: 45 checks. PASS: 39 / HIGH FAUD-0010: 2 surfaces / LOW 4.**

**Upload Security Conclusion:** Validator is extremely well-engineered with 7-layer defense (extension + MZ guard + magic + deep OOXML inner structure + CSV NUL + min + max size). Storage layer has industry-leading dual-stage cross-company isolation + internal filenames always GUID. The ONLY functional authorization defect is DocumentsController LIST + DOWNLOAD using `CanViewOperationalData` policy instead of `CanViewFinancialData` (FAUD-0010 HIGH). Remediate FAUD-0010 before release. Upload + Storage architecture itself is high quality hardened pattern with defense-in-depth.
