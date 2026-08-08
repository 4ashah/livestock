# SECURITY & AUTHORIZATION FINDINGS

**Audit Date**: August 7, 2026  
**Target Architecture**: ASP.NET Core Identity, RBAC (6 Roles), Cookie Authentication, Multi-Company Data Isolation  

---

## Executive Security Assessment

The application incorporates key baseline security controls:
- **Authentication**: Standard ASP.NET Core Identity with PBKDF2 password hashing (100,000 iterations), cookie-based session management (`.Livestock.Identity` HttpOnly cookie), anti-forgery tokens (`ValidateAntiForgeryToken`) on state-changing POST forms.
- **File Storage Security**: `ProtectedDocumentStorage` validates file headers against magic bytes (JPEG `FF D8 FF`, PNG `89 50 4E 47`, PDF `%PDF-`), restricts allowed extensions (`.pdf`, `.jpg`, `.jpeg`, `.png`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.csv`), enforces a 10 MB maximum upload limit, normalizes storage paths outside `wwwroot`, and blocks path traversal attempts containing `../`.

However, the security audit identified **two Critical vulnerability categories** in server-side authorization enforcement and multi-tenant data isolation.

---

## Detailed Vulnerability & Risk Analysis

### 1. Authorization & Role Matrix Breakdown (CRITICAL)

#### Threat Description
The application architecture defines 6 distinct roles in `RoleNames.cs` (`Viewer`, `DataEntry`, `FarmManager`, `Accounts`, `CompanyAdministrator`, `SystemAdministrator`). However, controllers implemented across core business modules (`SalesController`, `PaymentsController`, `InvoicesController`, `FarmsController`, `CustomersController`, `LivestockController`) enforce authorization using hardcoded legacy Phase-1 strings:
- `[Authorize(Roles = "Administrator,Manager")]`
- `[Authorize(Roles = "Administrator,Manager,DataEntry")]`

#### Vulnerability Mechanics & Evidence
- In `RoleNames.cs` (L12): `public const string Administrator = CompanyAdministrator;`. There is no role named `"Manager"`.
- When an legitimate user assigned `Accounts` or `FarmManager` attempts to access POST `/sales/create`, POST `/invoices/confirm`, or GET `/invoices/downloadpdf`, ASP.NET Core Identity checks if `User.IsInRole("Manager")` or `User.IsInRole("Administrator")`.
- Because the user's claims contain `role = "Accounts"` or `role = "FarmManager"`, the check evaluates to `false`.
- **Result**: Legitimate accounts managers and farm managers are locked out with HTTP 403 Access Denied.
- Conversely, `SystemAdministrator` users (role `SystemAdministrator`) are blocked from all endpoints using `[Authorize(Roles = "Administrator,Manager")]` because `"SystemAdministrator"` does not match `"Administrator"` (which maps only to `"CompanyAdministrator"`).

---

### 2. Multi-Tenant Data Exposure via Indirect Object Reference (ID Tampering) (CRITICAL)

#### Threat Description
The system is designed to support multi-company data isolation. However, queries in application services (`InvoiceService.cs`) and Web controllers (`InvoicesController.cs`) retrieve records using primary key `Guid` parameters without filtering on `CompanyId`.

#### Vulnerability Mechanics & Evidence
- **Source Code**:
  - `InvoicesController.cs` L74: `var invoice = await _invoiceService.GetByIdAsync(id, ct);`
  - `InvoiceService.cs` L32: `var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct)`
  - `InvoiceService.cs` L48: `var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == dto.InvoiceId, ct)`
- **Exploitation Scenario**:
  1. Attendant User A belongs to Company A (`CompanyId = 11111111-1111-1111-1111-111111111111`).
  2. Invoice `22222222-2222-2222-2222-222222222222` belongs to Company B (`CompanyId = 99999999-9999-9999-9999-999999999999`).
  3. User A sends HTTP GET to `/invoices/details/22222222-2222-2222-2222-222222222222` or `/invoices/downloadpdf/22222222-2222-2222-2222-222222222222`.
  4. `_invoiceService.GetByIdAsync` fetches the record by ID alone, disregarding `user.CompanyId`.
- **Impact**: Attendants in Company A can read, confirm, cancel, or download financial PDFs belonging to Company B, breaching corporate privacy and data isolation compliance.

---

### 3. Application Settings & Audit Log Authorization (HIGH)

#### Findings
- `SettingsController` uses `[Authorize(Roles = RoleNames.CompanyAdministrator + "," + RoleNames.SystemAdministrator)]`.
- On GET `/settings`, if a user is `CompanyAdministrator`, `targetCompanyId` defaults to `userCompanyId`.
- On POST `/settings`, `SettingsController` validates `if (!isSystemAdmin && vm.Id != userCompanyId) return Forbid();`. This correctly prevents a `CompanyAdministrator` from modifying another company's profile.
- **Gap Identified**: `SettingsController` updates `company.TaxRate`, `company.Currency`, and `company.InvoicePrefix` without checking if posted invoices already exist for that company. `docs/DATABASE.md` Section 1 specifies: `CurrencyCode immutable after 1st posted invoice`. The application code lacks this enforcement.

---

### 4. File Security & Protected Storage Verification (PASSED WITH LOW RISKS)

#### Controls Audited & Passed
- **Magic-Byte Verification**: Tested uploading `.exe` file renamed as `.jpg`. `ProtectedDocumentStorage.VerifyMagicBytes` inspected initial header bytes (`MZ` signature `4D 5A`) and threw `InvalidOperationException("File signature (magic bytes) does not match expected signature for extension .jpg")`.
- **Path Traversal Protection**: `ProtectedDocumentStorage.NormalizeInternalPath` and path resolution verify `if (!fullPath.StartsWith(resolvedRoot)) throw InvalidOperationException("path traversal detected")`.
- **Cross-Company File Storage Isolation**: Files are stored under subfolders by `companyId` GUID (`./App_Data/ProtectedDocuments/{companyId}/{yyyy-MM}/{fileGuid}.ext`).
- **Download Verification**: `ProtectedDocumentStorage.DownloadAsync` requires `companyId` argument and asserts `if (document.CompanyId != companyId) throw UnauthorizedAccessException()`.

---

## Mandatory Security Remediation Steps

1. **Refactor Controller `[Authorize]` Attributes**: Replace all legacy `"Administrator,Manager"` role strings with explicit Phase-2 role constants across all 14 Web controllers.
2. **Enforce Tenant Context in Domain Services**: Update all service interfaces (`IInvoiceService`, `ISaleService`, `ILivestockService`, `IPaymentService`) to accept `Guid companyId` in `GetByIdAsync` and state-change methods, querying `.Where(x => x.Id == id && x.CompanyId == companyId)`.
3. **Enforce Currency Immutability**: Add rule in `SettingsController` and `CompanyService` checking `if (_db.Invoices.Any(i => i.CompanyId == companyId && i.Status != InvoiceStatus.Draft))` before allowing `Currency` edits.
