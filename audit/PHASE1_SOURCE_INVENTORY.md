# PHASE 1 SOURCE INVENTORY — Livestock Manager

Baseline: branch `remediation/final-production-hardening`, commit `ef1fac7`.
Methodology: Every statement below cites source file evidence — never documentation claims.
Sections A–L as specified; "Next Phase 2 priorities" ranked from `audit/DEFECTS.md` + Section H gaps + discovered Section B/E/J issues.

---

## A) LivestockType authoritative values

**Source file:** `src/LivestockManager.Domain/Enums/LivestockType.cs:3-15`

| Numeric value | Short name | Long-name alias |
|---|---|---|
| 1 | Ah | PurchasedCastratedRam |
| 2 | Su | UncastratedRam |
| 3 | Sa | PurchasedEwe |
| 4 | Ad | BredCastratedRam |
| 5 | Sd | BredEwe |

**Enum count:** 5 distinct integer values, 10 total declared members (5 short + 5 long aliases point to same 5 integers).

**Comments:** The C# source file contains ZERO comments. There is NO mention of Aves, poultry, swine, pigs, bovine, calves, sheep categories anywhere inside `LivestockType.cs`. (docs/ASSUMPTIONS.md §A5 makes those claims; the source enum does not prove them. The *only* semantic mapping evidence is the long-member aliases: PurchasedCastratedRam / UncastratedRam / PurchasedEwe / BredCastratedRam / BredEwe — all ovine-sheep terminology.)

**Additional mapping evidence:** `EfSequenceGenerator.cs:22-33 GetPrefix()` switch:

```csharp
LivestockType.PurchasedCastratedRam => "Ah"
LivestockType.UncastratedRam => "Su"
LivestockType.PurchasedEwe => "Sa"
LivestockType.BredCastratedRam => "Ad"
LivestockType.BredEwe => "Sd"
```

Livestock ID format (no year) = `Ah00001 / Su00001 / Sa00001 / Ad00001 / Sd00001` (EfSequenceGenerator.cs:38-39).

---

## B) Connection-string canonical key

### B.1 Authoritative consumption in C# code

- `src/LivestockManager.Infrastructure/ServiceCollectionExtensions.cs:22-31` — `AddDbContext<AppDbContext>` calls:
  ```csharp
  configuration.GetConnectionString("DefaultConnection")
  ```
  `GetConnectionString("X")` is the ASP.NET Core helper that reads configuration key **`ConnectionStrings:DefaultConnection`** (or double-underscore env form `ConnectionStrings__DefaultConnection`).

- `src/LivestockManager.Infrastructure/Persistence/DesignTimeAppDbContextFactory.cs:12-13` — design-time factory has **hardcoded inline connection string** (no config key lookup):
  `"Server=.;Database=LivestockManager;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=15"`.

### B.2 appsettings JSON files — keys present

All three JSON files use `ConnectionStrings:DefaultConnection`. No file uses `LivestockManagerDb`.

| File | Keys present under `ConnectionStrings:` |
|---|---|
| `src/LivestockManager.Web/appsettings.json:2-5` | **`DefaultConnection`** + `FallbackConnection` (FallbackConnection is NOT read by any C# code anywhere) |
| `src/LivestockManager.Web/appsettings.Development.json:2-4` | **`DefaultConnection`** only |
| `src/LivestockManager.Web/appsettings.Production.example.json:2-4` | **`DefaultConnection`** only |

### B.3 PowerShell scripts — connection string key variants set/read

| Script | Connection-string key / behaviour |
|---|---|
| `scripts/Run-E2ETests.ps1:225-226, 244` | **DISCREPANCY:** Builds `$ConnStringRaw = "Server={server};Database={e2e_db};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"` then sets environment variable **`ConnectionStrings__LivestockManagerDb` = $ConnStringRaw** (line 244). However the C# code reads `ConnectionStrings__DefaultConnection`. This means the E2E orchestrator's disposable database override **cannot reach AddDbContext** through configuration. The web host will instead fall back to `appsettings.json` DefaultConnection (Database=LivestockManager) — potentially touching the *shared* dev database instead of the disposable E2E database. Also exports `LIVESTOCK_E2E_DATABASE` and `LIVESTOCK_E2E_SERVER` (lines 245-246) — neither is read by any C# file. |
| `scripts/database-update.ps1:10` | Invokes `dotnet ef database update` directly — design-time factory uses its hardcoded string, NOT any environment override. |
| `scripts/backup-database.ps1:88, 53-61` | Uses positional `-ServerInstance` / `-DatabaseName` parameters passed inline to `sqlcmd -S $ServerInstance -E`. Never sets `ConnectionStrings:*` env vars. |
| `scripts/restore-database.ps1:78-86` | Uses positional `-ServerInstance` / `-BackupFile` / `-TargetDatabase` passed inline to `sqlcmd -S $ServerInstance -E`. Never sets `ConnectionStrings:*` env vars. |
| `scripts/publish-iis.ps1` | Delegates to `package-release.ps1`; zero connection-string handling. |
| `scripts/seed-demo-data.ps1:39-40` | Sets `SEED_DEMO_DATA=1` + `ASPNETCORE_ENVIRONMENT=Development`; no ConnectionStrings env var (relies on appsettings.json DefaultConnection). |
| `scripts/run-dev.ps1:10-11` | Sets `ASPNETCORE_ENVIRONMENT=Development` + `EnableDevSeed=true`; relies on appsettings.json DefaultConnection. |
| `scripts/run-local.ps1:42` | Sets only `ASPNETCORE_URLS`; relies on appsettings.Production.example.json / DefaultConnection. |
| `scripts/smoke-test.ps1:9-11` | Sets `ASPNETCORE_ENVIRONMENT=Production`, `SeedDemoData=0`, `EnableDevSeed=false`; relies on DefaultConnection. |
| `scripts/test.ps1` / `build.ps1` / `clean.ps1` / `package-release.ps1` | No connection string handling. |

### B.4 Summary of canonical key variants used across all source files

| Configuration key / identifier | Used in C# code? | Used in appsettings JSON? | Used in scripts? |
|---|---|---|---|
| **`ConnectionStrings:DefaultConnection` (canonical)** | ✅ ServiceCollectionExtensions.cs:25 | ✅ All 3 JSON files | ❌ (no script sets this env var) |
| `ConnectionStrings:FallbackConnection` | ❌ Never read | ✅ appsettings.json only | ❌ |
| **`ConnectionStrings:LivestockManagerDb`** (Run-E2E) | ❌ Never read by any C# | ❌ Never present | ⚠️ Run-E2ETests.ps1:244 ONLY — WRONG KEY |
| Inline hardcoded design-time string | ✅ DesignTimeAppDbContextFactory.cs:12-13 | N/A | N/A |
| `LIVESTOCK_E2E_DATABASE` / `LIVESTOCK_E2E_SERVER` | ❌ Never read by any C# | ❌ | ⚠️ Run-E2ETests.ps1:245-246 — dead vars |

---

## C) PDF Generation

### C.1 Abstraction location

- `src/LivestockManager.Domain/Abstractions/` — Domain layer (0 dependencies). `IPdfGenerator` interface lives here (confirmed by `FormattedPdfWriter.cs:10 : IPdfGenerator` and `StubPdfGenerator.cs:7 : IPdfGenerator`).

### C.2 DI registration

`src/LivestockManager.Infrastructure/ServiceCollectionExtensions.cs:53-54`:
```csharp
services.AddScoped<ISequenceGenerator, EfSequenceGenerator>();
services.AddScoped<IPdfGenerator, FormattedPdfWriter>();
```
**StubPdfGenerator is NOT registered in DI.** It exists as source (`StubPdfGenerator.cs`) but no `services.AddScoped<IPdfGenerator, StubPdfGenerator>()` line exists. Only `FormattedPdfWriter` is active.

### C.3 FormattedPdfWriter page-tree / multi-page capability

`FormattedPdfWriter.cs:290-338 AssemblePdf()` method produces **exactly 1 PDF page**, hardcoded. Evidence:

```csharp
objects.Add((1, "<< /Type /Catalog /Pages 2 0 R /OpenAction [3 0 R /XYZ null null null] >>"));
objects.Add((2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));  // <-- /Count 1, /Kids has single ref
objects.Add((3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R ... >>"));
```

There is NO loop, NO second page object, NO additional /Kids array entries, NO `/Type /Pages` intermediate node beyond the single root. Document overflow truncates at:
- Notes+Terms block: `if (y < MarginBottom + 30) break;` (line 495) / `if (y < MarginBottom + 12) break;` (line 509).
- Items table: y just decrements 13 per line with NO `if (y < …) NewPage()` guard.
**Conclusion: Proper multi-page page tree DOES NOT EXIST; content past page 1 is silently truncated.** (Matches audit/DEFECTS.md DEF-008 deferred.)

### C.4 Unicode font embedding

`FormattedPdfWriter.cs:304-305` registers only 2 standard Type1 fonts:
```
(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")
(6, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>")
```
No `/DescendantFonts`, no `/CIDSystemInfo`, no `/ToUnicode CMap`, no `/FontFile/FontFile2/FontFile3` stream embedding, no `Identity-H` encoding. Non-ASCII characters are escaped to raw octal (`EscapePdfString` lines 536-537: `c > 126 → \ooo octal`) but Type1 Helvetica has no glyphs for >127 code points. **Unicode / CJK / accented rendering evidence: NOT EXISTS.** (Matches DEF-009 deferred.)

### C.5 StubPdfGenerator usage

`src/LivestockManager.Infrastructure/Services/StubPdfGenerator.cs` — exists as a 101-line class producing a minimal 1-page Helvetica PDF. **NOT registered in DI.** Zero call sites. Zero `#if DEBUG` toggle. Zero constructor / factory conditional. Dead code in current build.

---

## D) File uploads

### D.1 Allow-list enumeration (5 independent locations)

| Location | Allow-list evidence |
|---|---|
| `appsettings.json:8-12 FileStorage section` | `AllowedExtensions: ".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx,.csv"` — note: this config section is **NOT read by any C# code** (ProtectedDocumentStorage.cs takes allowedExtensions as a runtime array parameter, never binds IOptions to this section). MaxUploadBytes 10485760 (= 10 MB) also declared here but not consumed from config by the storage class. |
| `src/LivestockManager.Web/Controllers/DocumentsController.cs:86` | `new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".csv" }` — **this is the actual enforced server-side allow-list.** NOTE: adds `.gif` + `.txt` that are NOT in the appsettings FileStorage list, and **OMITS `.doc/.docx/.xls/.xlsx`** that ARE in the appsettings list. 7 extensions server-side; 9 claimed in JSON. |
| `src/LivestockManager.Web/Views/Documents/Upload.cshtml:39` | UI text only: *"Allowed: .pdf, .jpg, .jpeg, .png, .gif, .txt, .csv (max 10 MB each)"* — matches DocumentsController.cs server list (7). |
| `Upload.cshtml:41 file input` | `<input asp-for="Files" type="file" multiple class="form-control d-none" id="fileInput" />` — **NO `accept=` attribute on the file input.** The browser does not filter at the picker level. |
| `src/LivestockManager.Infrastructure/Services/Storage/ProtectedDocumentStorage.cs:210-234 ValidateAndNormalizeExtension()` | Takes `string[] allowedExtensions` runtime parameter passed from controller; rejects any extension not in the passed array. |

### D.2 Magic-byte validation

`ProtectedDocumentStorage.cs:236-295 VerifyMagicBytes()`. Switches on extension:

- `.pdf`: header[0..3] must equal `0x25 0x50 0x44 0x46` (= `%PDF`) ✅
- `.jpg/.jpeg`: header[0..1] = `0xFF 0xD8` (JPEG SOI) ✅
- `.png`: header[0..7] = PNG 8-byte signature `89 50 4E 47 0D 0A 1A 0A` ✅
- `.gif`: header[0..2] = `0x47 0x49 0x46` (= `GIF`) ✅
- `.txt`: `break` (no validation — any bytes pass)
- **default**: `break` (no validation — any bytes pass).

Because DocumentsController passes `.csv` alongside `.txt`, both `.csv` and the 6th+7th slots will fall through `default: break;` → **`.csv` has zero magic-byte / NUL-byte validation.** (docs/SECURITY.md §4 claims: "CSV no NUL bytes in first 4096 bytes" — actual source does NOT enforce this.)

### D.3 Size limits

- `DocumentsController.cs:87` — `const long maxSizeBytes = 10 * 1024 * 1024;` (= 10,485,760 B).
- `ProtectedDocumentStorage.cs:42-46` (seekable streams) + `69-75` (non-seekable buffered via ReadBufferedWithMax) — double enforcement, throws on exceed.

### D.4 Cross-company isolation

Enforced in TWO storage code paths (+ controller query filter):

1. `StoreAsync` line 87: `var companySub = $"{companyId:N}";` sub-folder (user's companyId passed from line 84 GetCurrentCompanyAndUser).
2. `DownloadAsync` line 127-135: loads document by id then explicit check `if (document.CompanyId != companyId) throw new UnauthorizedAccessException(...)`.
3. `DocumentsController.List` line 148-149: `.Where(d => d.CompanyId == companyId)` on the Documents DbSet query.
4. Global EF filter: `AppDbContext.cs:61-77 ConfigureGlobalFilters` applies `IsDeleted == false` to every BaseAuditableEntity. (Note: there is NO CompanyId global query filter; isolation is manual per-method.)

### D.5 Storage location (outside wwwroot)

`ProtectedDocumentStorage.cs:19-29`:
```
configuredRoot = configuration["ProtectedStorage:Root"]   // appsettings.json:14 = "./App_Data/ProtectedDocuments"
relativeRoot = string.IsNullOrWhiteSpace(configuredRoot) ? "./App_Data/Documents" : configuredRoot;
combined = Path.Combine(AppContext.BaseDirectory, relativeRoot);
_rootFolder = Path.GetFullPath(combined);
```
`AppContext.BaseDirectory` = the application bin directory (e.g. `bin/Debug/net8.0/` or IIS publish `bin/`); `wwwroot` is a sibling at the project root level. Files are stored at `{basedir}/App_Data/ProtectedDocuments/{companyGuidN}/{yyyy-MM}/{guid128}.{ext}`. Path-traversal guard at line 97-98: `if (!finalDir.StartsWith(resolvedRoot, StringComparison.Ordinal)) throw`. **Files are physically outside wwwroot and never served by UseStaticFiles.**

---

## E) Seed protection flags (DemoDataSeeder guards)

### E.1 Source file: `src/LivestockManager.Infrastructure/Persistence/Seed/DemoDataSeeder.cs:16-56 SeedAsync`

```csharp
// Line 22: Reads ASPNETCORE_ENVIRONMENT from env-var FIRST, then config fallback:
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? configuration["ASPNETCORE_ENVIRONMENT"];
var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);  // line 23

// Lines 25-27: three independent config flags (EXACT string-match, no bool parse):
var seedDemoData = configuration["SeedDemoData"] == "1";      // line 25 — match exact "1"
var enableDevSeed = configuration["EnableDevSeed"] == "true"; // line 26 — match exact lowercase "true"
var enableE2ESeed = configuration["EnableE2ESeed"] == "1";    // line 27 — match exact "1"

// Line 29: allowed environments (case-sensitive string equality, not OrdinalIgnoreCase):
var seedEnv = (env == "Development" || env == "Staging" || env == "Testing");
// NOTE: "development" with lowercase d would FAIL this guard.

// Line 30: combines environment + dev/e2e flag with OR
var shouldSeed = seedEnv && (enableDevSeed || enableE2ESeed);

// Lines 31-34: PRODUCTION EXPLICIT NO-OP gate
if (isProduction && !seedDemoData && !enableE2ESeed)  // Production + all 3 flags off → return
{
    return;
}
// Lines 35-38: general NO-OP if non-Prod but no flag set
if (!shouldSeed && !seedDemoData && !enableE2ESeed)
{
    return;
}
```

### E.2 Comparison with docs/ASSUMPTIONS.md §A7 specification

| Docs claim (§A7 dual-guard) | Actual source behaviour | Match? |
|---|---|---|
| **Dual-guard: BOTH `ASPNETCORE_ENVIRONMENT == "Development"` ordinal-ignore-case AND `ENV_ENABLE_DEV_SEED ∈ {true, 1, yes}` case-insensitive parse** | Source checks env == "Development" (case-SENSITIVE exact) AND `configuration["EnableDevSeed"] == "true"` exact lowercase string. **NO `ENV_ENABLE_DEV_SEED` environment variable is ever read by name** — source uses IConfiguration key `EnableDevSeed` with exact "true" match, not `bool.TryParse`, and doesn't accept 1/yes. | ❌ MISMATCH on 3 axes: (1) env name is `EnableDevSeed` not `ENV_ENABLE_DEV_SEED`; (2) case- and type-sensitive exact `"true"` only (not bool parse of true/1/yes); (3) ASPNETCORE_ENV comparison is case-sensitive (docs claim ordinal-ignore-case). |
| Additional sanity guard: reject `@example.com` email inserts when Production | Source seeds `admin@livestock.dev` + `{role}@livestock.dev` (lines 136, 198-205). The `@example.com` domain never appears in DemoDataSeeder.cs. No `@example.com` production rejection logic exists because those emails are not in the seed domain at all. | N/A neutral. |

### E.3 Summary of flags actually honoured

- `ASPNETCORE_ENVIRONMENT`: Development / Staging / Testing (case-sensitive) → line 29 seedEnv=true. Production line 31 is explicit NO-OP unless SeedDemoData or EnableE2ESeed override it.
- `SeedDemoData = "1" (config)` → bypasses environment check, runs in any env including Production.
- `EnableDevSeed = "true" (config)` → runs inside Development/Staging/Testing only.
- `EnableE2ESeed = "1" (config)` → runs inside Development/Staging/Testing only (and is the flag Run-E2ETests.ps1 sets at line 241).

---

## F) Date/time — IDateTime provider implementation

### F.1 Interface

`src/LivestockManager.Domain/Abstractions/IDateTime.cs:1-6`:
```csharp
namespace LivestockManager.Domain.Abstractions;
public interface IDateTime
{
    DateTimeOffset Now { get; }
}
```

### F.2 Implementation

`src/LivestockManager.Infrastructure/Services/DateTimeProvider.cs:1-8`:
```csharp
public class DateTimeProvider : IDateTime
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
```
Registered scoped at `ServiceCollectionExtensions.cs:52`: `services.AddScoped<IDateTime, DateTimeProvider>();`.

### F.3 Audit timestamps use UTC

**Confirmed 100% UTC.** Direct `DateTimeOffset.UtcNow` usage (not via IDateTime) in the audit hot-path (AppDbContext SaveChanges pipeline):

- `AppDbContext.cs:107 UpdateTimestamps` — `var now = DateTimeOffset.UtcNow;` → writes CreatedAt / ModifiedAt for all BaseAuditableEntity.
- `AppDbContext.cs:152 HandleSoftDeletes` — `var now = DateTimeOffset.UtcNow;` → writes ModifiedAt on soft-delete.
- `AppDbContext.cs:168 PrepareAuditEntries` — `var now = DateTimeOffset.UtcNow;` → audit entry CreatedAt.
- `AppDbContext.cs:249` — `var auditLog = new AuditLog(auditEntry.Action, auditEntry.CreatedAt)` → uses the UTC now from 168.

**Note on consistency:** SaveChanges / audit pipeline uses `DateTimeOffset.UtcNow` directly (not injected `IDateTime.Now`). Unit tests that mock IDateTime cannot mock the audit / soft-delete / timestamp path — only service-layer usages of `IDateTime` are mockable.

---

## G) Health checks (Program.cs)

### G.1 Service registration

`src/LivestockManager.Web/Program.cs:16-18`:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("UP"), tags: new[] { "live", "ready" })
    .AddDbContextCheck<AppDbContext>("db", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });
```
Two checks:
1. `"live"` — always returns Healthy (liveness probe, tags: live+ready)
2. `"db"` — DbContext health via `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (referenced in Web.csproj line 19), tags: ready only

### G.2 Endpoint routes mapped (Program.cs lines 98-113)

| Route | Predicate (tags filter) | Notes |
|---|---|---|
| `/health/live` | c.Tags.Contains("live") → only the `"live"` check runs | Returns 200 OK with JSON {status,totalDuration,checks[]} via `WriteHealthJson` static helper at lines 81-96. |
| `/health/ready` | c.Tags.Contains("ready") → `"live"` + `"db"` checks both run (2 total) | `db` failure sets Degraded per line 17 failureStatus — ready probe will return 200 even with degraded DB (the default middleware threshold is Unhealthy → 503). |
| `/health` | No predicate → all registered checks run (full set) | Same JSON writer. |

Used by Run-E2ETests.ps1:157 `Wait-Health` at `${url}/health/live` with 90s timeout (line 397), and smoke-test.ps1:25 hits `/health` for 200.

---

## H) Doc-claimed features vs source-code evidence (EXISTS / NOT EXISTS table)

Documentation claims sources: docs/ARCHITECTURE.md, docs/SECURITY.md, docs/DATABASE.md, docs/DEPLOYMENT.md, docs/ADMIN_GUIDE.md, docs/USER_GUIDE.md, docs/MASTER_PLAN.md, docs/THREAT_MODEL.md, docs/REQUIREMENTS.md, docs/TEST_PLAN.md. Each claim row checked against: csproj PackageReference grep, Program.cs registration line, Controller action + route, ServiceCollectionExtensions registration, _Layout.cshtml nav link.

| # | Doc-claimed feature | EXISTS / NOT EXISTS | Source evidence (file:line) or proof of absence |
|---|---|---|---|
| 1 | Hangfire dashboard at `/hangfire` (SystemAdmin only, jobs/queues/processing/succeeded/failed/recurring) | **NOT EXISTS** | Zero `Hangfire` string in any `.cs` file in src/. Zero Hangfire.* NuGet in any csproj. Zero `/hangfire` route in Program.cs or controllers. Zero `MapHangfireDashboard` / `services.AddHangfire`. |
| 2 | SendGrid email API + SendGridEmailSender class | **NOT EXISTS** | Zero `SendGrid` in any .cs / csproj. No `Email__SendGridApiKey` config binder anywhere; appsettings JSON has no Email section. |
| 3 | SMTP queue / HangfireEmailQueue / background email retry (3× exponential backoff) | **NOT EXISTS** | Zero `SmtpClient / Smtp / Queue` in services. No `BackgroundService` / `IHostedService` in any .cs. No `OutboundEmails` DbSet in AppDbContext.cs (check lines 22-44 — 26 entity sets listed, none are OutboundEmails). |
| 4 | Azure Blob storage (`IFileStorage` → `AzureBlobFileStorage : IFileStorage`) + Azure.Storage.Blobs | **NOT EXISTS** | Zero `Azure.Storage.Blobs` / `BlobContainerClient` / `AzureBlobFileStorage` / `StorageProvider` references. No NuGet. No `IFileStorage` interface anywhere in Domain Abstractions (only `IProtectedDocumentStorage` exists, not the claimed generic IFileStorage with LOC/AZB/S3 enum). |
| 5 | Amazon S3 storage provider + AWSSDK.S3 + StoredFiles StorageProvider column (LOC / AZB / S3) | **NOT EXISTS** | Zero `AWSSDK / S3Client / Minio` in source. No `StoredFiles` DbSet in AppDbContext. Zero StorageProvider enum in Domain/Enums/. |
| 6 | QuestPDF rendering (`QuestPdfGenerator : IPdfGenerator`) + QuestPDF NuGet | **NOT EXISTS** | Zero QuestPDF NuGet in 8 csproj files. Zero `QuestPDF` / `QuestPdf` in source. IPdfGenerator is implemented only by FormattedPdfWriter + StubPdfGenerator (Section C). |
| 7 | MediatR CQRS pipeline with FluentValidation behaviors (MediatR + MediatR DI + IPipelineBehavior) | **NOT EXISTS** | Zero MediatR NuGet. Zero `IMediator / Send( / Publish(` in any .cs file. Application layer services are direct interfaces, not IRequest/IRequestHandler. Note: FluentValidation 11.9.0 IS referenced (Application.csproj:8-9) — but zero `services.AddValidatorsFromAssembly*` / `AddFluentValidation*` registration exists in ServiceCollectionExtensions or Program.cs. FluentValidation DLL is present but unused in DI. |
| 8 | Generic Repository pattern `IRepository<T>` + `IUnitOfWork` | **NOT EXISTS** | Zero `IRepository / UnitOfWork` strings in src/. Data access is direct `AppDbContext.Set<T>()` + DbSet<T> properties on the context; services inject `AppDbContext` / `IAppDbContext` (note: `IAppDbContext` facade exists with SaveChanges/BeginTransaction only — not a generic repository). |
| 9 | Company impersonation / switcher 🏢 (SystemAdmin dropdown → Session `ImpersonatingCompanyId`, cross-company full context, visible in _Layout nav) | **NOT EXISTS** in Web layer; PARTIAL skeleton only in Application | Application layer has `ICompanyService.SwitchCompanyAsync(Guid companyId, CancellationToken)` declared at `Application/Services/Companies/ICompanyService.cs:11` + implemented at CompanyService.cs:67. However: zero controller route calls this method (grep src/LivestockManager.Web for `SwitchCompany` / `Impersonat` → 0 matches). _Layout.cshtml sidebar lines 143-264 contain no Company/Switcher UI item. Zero `Session.Set*("ImpersonatingCompanyId" / HttpContext.Session` usage. The Application interface exists with 0 call sites. |
| 10 | Maintenance mode (enable: all POST → 503 except SysAdmin; docs/ADMIN_GUIDE §5.6) | **NOT EXISTS** | Only `ExpenseCategory.Maintenance = 7` enum value was hit by grep; zero middleware, zero `IMiddleware`, zero `MaintenanceMode` / `app.UseWhen` + 503 filter, zero controller `[Maintenance]` attribute, zero settings toggle in SettingsViewModel. |
| 11 | User invitations (SysAdmin → Users tab → +Invite User, 24h one-time link) | **NOT EXISTS** | Zero `Invite` / `Invitation` / `UserInvit` strings in src/. No Account action = Invite, no token generation, no email dispatch (see also #1/#2/#3 — no email channel exists at all). |
| 12 | MFA reset workflow for lost phone (Reset TOTP authenticator; ADMIN_GUIDE §3.3) | **NOT EXISTS** | Zero `ResetMfa / Reset2FA / TOTP / ResetAuthenticator / TwoFactor` tokens/reset in .cs or controllers. AccountController has zero MFA actions (controllers exist: AccountController is in the list of 14 controllers, no reset actions for 2FA in grep results). |
| 13 | Per-company storage quota enforcement (50 GB default; docs/SECURITY §4) | **NOT EXISTS** | Zero `Quota / StorageQuota` in src/. ProtectedDocumentStorage never sums or checks company usage before accepting a new upload. No byte-sum aggregation in DocumentsController.List. No `StorageUsedBytes` column on Company entity. |
| 14 | Break-glass reset document counter (ADMIN_GUIDE §5.2 Reset Counter) + §5.3 Manually Set Invoice Number | **NOT EXISTS** | Zero `ResetSequence / BreakGlass / ManualInvoice / SetInvoiceNumber` in src/. No controller action exposes `SequenceCounters` table edits. The only mutation path is `EfSequenceGenerator.GenerateNumberAsync` atomic UPDATE OUTPUT / INSERT (lines 118-156) — no admin edit route. |
| 15 | Password-force-change workflow (post-login redirect if MustChangePassword flag) | **NOT EXISTS** | Zero `ForceChangePassword / MustChangePassword` in src/. ApplicationUser class (Identity) has no such boolean property. No `OnSignedIn / SignInManager` hook redirect exists in AccountController. |
| 16 | Transaction-log backup scripts (TLOG backups) | **NOT EXISTS** | `backup-database.ps1` ONLY performs `BACKUP DATABASE` (full backup) at line 88. No `BACKUP LOG` / TSQL code anywhere. No PowerShell script for LOG backups. |
| 17 | SQL Server Agent jobs (Full/Diff/TLOG schedule) | **NOT EXISTS** | No `.sql` scripts for SQL Agent jobs, no `sp_add_job`/`sp_add_schedule` calls, no MSDB configuration in backup/restore PS1 scripts. Deployment docs claim §10.1 scheduling but source has zero automation. |
| 18 | Point-in-time restore with tail-log / STOPAT | **NOT EXISTS** | `restore-database.ps1` ONLY uses `RESTORE DATABASE FROM DISK WITH RECOVERY, MOVE...`. No `RESTORE LOG` + `STOPAT = 'datetime'`. No tail-log-backup step before restore. |
| 19 | Email reminders (USER_GUIDE §5.4 auto 3× retry reminder) | **NOT EXISTS** | Zero `Reminder` / `RecurringJob` / `IHostedService` / `BackgroundService` in src/. Also requires #1/#2/#3 email channel which itself is absent. |
| 20 | Custom report builder UI + XLSX multi-sheet export (USER_GUIDE §7: "Excel XLSX multi-sheet where applicable"; REQ-RP-009 MUST) | **NOT EXISTS (Reports page exists, multi-sheet XLSX = NOT EXISTS)** | Reports/Details.cshtml line 10 has UI placeholder text: *"Report chart, table, filters, export as PDF/XLSX/CSV."* No ClosedXML / EPPlus / NPOI / `Worksheet` NuGet in any csproj. No `.xlsx` byte array generation in Services/. CSV export only via `CsvExporter` class name in Application/Common (no multi-sheet format). |
| 21 | Native/offline mobile app | **NOT EXISTS** | No `.csproj` for MAUI/Xamarin/Android/iOS, no manifest, no native resources. Only one web csproj. |
| 22 | PWA install (manifest, service worker, offline cache) | **NOT EXISTS** | No `manifest.json`, no `service-worker.js`, no sw registration in site.js or _Layout. No `<link rel="manifest">` tag. |
| 23 | App Store availability (iOS/Android store listings) | **NOT EXISTS** | No listing assets, no Fastlane, no build targets for iOS/Android. Pure web. |

**Summary counts:** EXISTS = 0 (partial: #9 is skeleton only with 0 call sites; #20 has UI placeholder text but zero backend). Full NOT EXISTS = 22 of 23 + 1 partial.

---

## I) Responsive layout

### I.1 site.css @media rules

`src/LivestockManager.Web/wwwroot/css/site.css` — 3 explicit `@media` blocks:

1. Line 23: `@media (min-width: 768px)` — scales root HTML `font-size: 16px` (default 14px on mobile).
2. Line 284: `@media (max-width: 767.98px)` (mobile/tablet-small):
   - Shrinks navbar container-fluid paddings to 0.75rem (line 285-293)
   - `.card-body.p-5` → `1.5rem` padding (line 295-297)
   - `.display-5` → `1.75rem` (line 299-301)
   - Tables: smaller font 0.85–0.9rem + 0.5–0.75rem cell padding (line 303-310)
3. Line 313: `@media (min-width: 992px)` (desktop):
   - Enables `.sidebar-layout` flex container with fixed `.sidebar { width: 260px; flex-shrink: 0; }` + `.main-area { flex: 1 }` (line 314-327).

### I.2 Bootstrap grid usage

Bootstrap 5.3 classes used pervasively throughout Views (Upload.cshtml example lines 25, 51: `row g-4`, `col-md-6`, `col-12`; _Layout footer line 271-278: `row`, `col-md-6`, `text-md-end`). `Views/_ViewImports.cshtml` + _Layout.cshtml lines 11/15 load `~/lib/bootstrap/dist/css/bootstrap(.min).css` + lines 283/286 load `bootstrap.bundle(.min).js`. Bootstrap version: 5.x (grid `container-fluid`, `row-cols-*`, `g-*`, btn-outline-primary all used).

### I.3 Mobile drawer implementation

Complete custom mobile sidebar in _Layout.cshtml inline styles + vanilla JS.

CSS (inline in `<style>` block _Layout.cshtml lines 28-123):
- `.sidebar { position: fixed; top: 56px; left: 0; bottom: 0; width: var(--sidebar-width); z-index: 1020; transition: transform 0.25s ease; min-height: calc(100vh - 56px); overflow-y: auto; background-color: var(--agri-primary); }`
- `.main-content { margin-left: var(--sidebar-width); padding-top: calc(56px + 1.5rem); }`
- Breakpoint line 86: `@@media (max-width: 991.98px) { .sidebar { transform: translateX(-100%); } .sidebar.open { transform: translateX(0); } .main-content { margin-left: 0; } }`
- `.sidebar-backdrop { position: fixed; inset: 0; z-index: 1015; display: none; background: rgba(0,0,0,0.4); top: 56px; }` + `.show` class.

JS toggle (lines 288-307 vanilla IIFE, no jQuery/BS collapse dependency):
- Hamburger button line 129: `<button class="sidebar-toggler d-lg-none" type="button" id="sidebarToggle" aria-label="Toggle navigation">&#9776;</button>` (shows only on <lg via BS `d-lg-none` utility).
- Toggles `.open` on sidebar + `.show` on backdrop. Backdrop click closes drawer.

**Responsive verification:** E2ETest E2eRemediationChecklist.cs lines 304-339 contains two explicit viewport tests: Workflow_16 sets 375×812×3 (iPhone mobile) and Workflow_17 sets 1920×1080×1 (desktop) — both render `/` without assertion failure.

---

## J) Scripts currently present (scripts/*.ps1 + repo-root *.cmd)

### scripts/*.ps1 (13 files)

| Script | Purpose tag | Behaviour (from source read) |
|---|---|---|
| `scripts/Run-E2ETests.ps1` | **e2e** | 16-step orchestrator: detect root → restore/build → playwright install chromium → create disposable DB → set `ASPNETCORE_ENVIRONMENT=Testing` + `EnableE2ESeed=1` → migrate → seed → start web → `/health/live` wait 90s → run Playwright xUnit TRX → capture artifacts → finally stop+drop DB. Params: -ServerInstance, -DatabaseName, -AppPort, -KeepDatabase, -Headed, -SkipBrowserInstall, -TestFilter. Exit 0 = all pass. |
| `scripts/build.ps1` | **build** | `dotnet build LivestockManager.sln -c $Configuration` (Release default). No additional restore. |
| `scripts/clean.ps1` | **clean** | `dotnet clean -c $Configuration` + recursively deletes `./artifacts` folder. |
| `scripts/test.ps1` | **test** | `dotnet test -c $Configuration --filter "FullyQualifiedName~UnitTests"` — runs **only UnitTests** project, not Integration/Architecture/E2E. |
| `scripts/package-release.ps1` | **package** | `dotnet publish LivestockManager.Web.csproj -c Release -r win-x64 --self-contained false -o ./artifacts/publish`. Creates artifacts/backups dir. |
| `scripts/publish-iis.ps1` | **publish** | Delegates to package-release.ps1; creates App_Data and App_Data/files under publish output; prints guidance to copy to `C:\inetpub\livestock\`. |
| `scripts/database-update.ps1` | **seed** | `dotnet ef database update --project Infrastructure --startup-project Web`. Applies EF migrations using DesignTimeAppDbContextFactory (hardcoded connection). `-NoBuild` flag supported. |
| `scripts/backup-database.ps1` | **backup** | 8 gated steps: identifier regex (no system DBs) → sqlcmd check → DB existence + connectivity → backup dir write-test → `BACKUP DATABASE ... TO DISK=... WITH INIT, COMPRESSION, STATS=10` → exit check → zero-length guard → retention purge on success. Logs to artifacts/logs. |
| `scripts/restore-database.ps1` | **restore** | Scenarios: (A) target DB not exists → RESTORE FILELISTONLY → WITH MOVE every logical file; (B) target exists + NO `-ConfirmDestructiveOverwrite` → exit 5 REFUSE (0 SQL executed); (C) target exists WITH confirm → `ALTER DATABASE SINGLE_USER WITH ROLLBACK IMMEDIATE → RESTORE DATABASE WITH REPLACE RECOVERY → ALTER MULTI_USER`. |
| `scripts/seed-demo-data.ps1` | **seed** | Sets `SEED_DEMO_DATA=1` + `ASPNETCORE_ENVIRONMENT=Development`; runs `dotnet run` on Web project so Program.cs startup scope (lines 119-141) runs Migrate + DemoDataSeeder. Logs to artifacts/logs. |
| `scripts/run-dev.ps1` | **run** | `ASPNETCORE_ENVIRONMENT=Development` + `ASPNETCORE_URLS=http://localhost:$Port (5100 default)` + `EnableDevSeed=true` → `dotnet run -c Debug`. |
| `scripts/run-local.ps1` | **run** | `ASPNETCORE_URLS=http://localhost:5100` → `dotnet run --no-launch-profile`. Wraps in log-script template with timestamps. |
| `scripts/smoke-test.ps1` | **smoke** | First runs test.ps1 (UnitTests only); then starts Web with `ASPNETCORE_ENVIRONMENT=Production / SeedDemoData=0 / EnableDevSeed=false` on port 5199 Release — waits for LISTENING socket 60s max; hits `/health` via `Invoke-WebRequest`; kills proc; exit 0 iff HTTP 200. |

### repo-root *.cmd (13 files — thin wrappers)

| CMD file | Wraps PS1 | Purpose tag |
|---|---|---|
| `build.cmd` | scripts\build.ps1 | **build** |
| `clean.cmd` | scripts\clean.ps1 | **clean** |
| `test.cmd` | scripts\test.ps1 | **test** |
| `run-e2e-tests.cmd` | scripts\Run-E2ETests.ps1 | **e2e** |
| `database-update.cmd` | scripts\database-update.ps1 | **seed** |
| `backup-database.cmd` | scripts\backup-database.ps1 | **backup** |
| `restore-database.cmd` | scripts\restore-database.ps1 | **restore** |
| `publish-iis.cmd` | scripts\publish-iis.ps1 | **publish** |
| `package-release.cmd` | scripts\package-release.ps1 | **package** |
| `seed-demo-data.cmd` | scripts\seed-demo-data.ps1 | **seed** |
| `run-dev.cmd` | scripts\run-dev.ps1 | **run** |
| `run-local.cmd` | scripts\run-local.ps1 | **run** |
| `smoke-test.cmd` | scripts\smoke-test.ps1 | **smoke** |

---

## K) E2E framework evidence

### K.1 Core classes

All in `tests/LivestockManager.EndToEndTests/`:

| Class | File | Purpose |
|---|---|---|
| `E2ETestBase` | `E2ETestBase.cs:80-262` | Abstract IAsyncLifetime base. Provides `NewPageAsync(testId, overrides?)` → calls `E2ETestEnvironment.DetectBlocker()` → `BlockerSkip.If(true, blocker)` runtime skip; opens 1280×800 Chromium context; Playwright tracing (screenshots/snapshots/sources=true) per test; per-test `RunAsync(testId, body)` wrapper that captures failure screenshots + trace zip via `CaptureFailureArtifactsIfAny` (line 156-188). `Sanitize()` filename helper. No unconditional `[Fact(Skip = ...)]` anywhere. |
| `E2ETestEnvironment` | `E2ETestEnvironment.cs:23-199` | Static. `DetectBlocker()` singleton → returns non-null string when: (a) `E2E_BASE_URL` env var empty/missing (`BlockerMissingBaseUrl` line 25-27), or (b) `Microsoft.Playwright.Playwright.CreateAsync()` + `Chromium.LaunchAsync()` throws (`BlockerPlaywrightOrChromiumMissing` line 29-32). Artifact dirs: `artifacts/e2e/screenshots`, `/traces`, `/videos`, `/logs` — env-overridable, created if missing. `FindRepoRoot()` walks up 16 dirs looking for `LivestockManager.sln`. ProcessExit + DomainUnload hooks call `CleanupAsync()`. |
| `E2eRemediationChecklist` | `E2eRemediationChecklist.cs:28-411` | xUnit test collection class `[Collection(nameof(E2ETestCollection))] : E2ETestCollectionBase`. Contains **18 [Fact] methods** (exactly Workflow_01..Workflow_18, numbered 1-18 with unique descriptive names). |
| `BlockerSkip` helper + `ClearSynchronizationContextAttribute` | `E2ETestBase.cs:29-62, 17-27` | `BlockerSkip.If(condition, reason)` uses reflection to construct and throw `Xunit.Sdk.SkipException` — this is a **runtime skip** (counts toward TRX `Counters.skipped`), NOT a compile-time `[Fact(Skip)]`. Important: the docs/E2E_TESTING.md §7 claim is TRUE; the earlier re-audit note that said `[Fact(Skip=...)]` was incorrect. |
| `E2ETestAssemblyFixture` + `E2ETestCollection` | E2ETestBase.cs lines 269-293 | Assembly fixture calls E2ETestEnvironment.CleanupAsync on Dispose. Collection definition wires the fixture to all E2eRemediationChecklist tests. |

### K.2 Count of tests actually present

18 facts (1 class × 18 `[Fact]` methods). List (method name → line):
1. Workflow_01_Login_ValidCredentials_AuthenticatedSession :38
2. Workflow_02_Login_InvalidPassword_LockoutAfterAttempts :59
3. Workflow_03_Livestock_Register_NewAnimal_AppearsInIndex :95
4. Workflow_04_Livestock_AddWeight_HistoryPersistsAndSorted :111
5. Workflow_05_Livestock_Discharge_StatusAndProfitLossCalculated :125
6. Workflow_06_Customer_Create_Edit_List_Works :139
7. Workflow_07_Supplier_Create_Edit_List_Works :153
8. Workflow_08_Sale_CreateDraft_Confirm_ProducesInvoice :166
9. Workflow_09_Invoice_Confirm_NumberUniqueAndSequential :180
10. Workflow_10_Invoice_Pdf_Download_ValidPdfBytes :194
11. Workflow_11_Payment_Partial50Percent_InvoicePartiallyPaid :235
12. Workflow_12_Payment_Remaining_InvoiceMarkedPaid :248
13. Workflow_13_Receipt_GeneratedForPayment_DownloadAndView :261
14. Workflow_14_CrossCompany_InvoiceAccess_Returns404NotFound :275
15. Workflow_15_DisallowedAccess_AccountsCannotCreateFarm_403 :290
16. Workflow_16_Viewport_Mobile375x812_ResponsiveLayout :304
17. Workflow_17_Viewport_Desktop1920x1080_FullLayout :323
18. Workflow_18_AuditLog_SensitiveActions_RecordedEntries :342

### K.3 Skip strategy

**RUNTIME ONLY.** Every [Fact] has NO Skip property. Skipping is guarded two ways:

1. **First call site** — `NewPageAsync()` (E2ETestBase.cs:113-114): every test calls this first thing via its RunAsync body.
2. **Second call site** — `LoginAsync()` (E2eRemediationChecklist.cs:377-378): double-redundant blocker check.

Skip conditions:
- `string.IsNullOrWhiteSpace(BaseUrl)` (BaseUrl = env:E2E_BASE_URL with trailing / trimmed) → BlockerMissingBaseUrl.
- Playwright.CreateAsync fails OR Chromium.LaunchAsync(Headless=!IsHeaded,Timeout=60000,Args=[--disable-dev-shm-usage --no-sandbox --disable-gpu]) fails or !IsConnected → BlockerPlaywrightOrChromiumMissing.

**No unconditional [Fact(Skip)] in any E2E test file.** (Conforms to DEF-010 and user's explicit hard constraint in project memory.)

### K.4 Test account convention (actually seeded, not guessed from docs)

DemoDataSeeder.cs lines 136 (`admin@livestock.dev`), 198-205 (accounts / farmmanager / dataentry / viewer / sysadmin `@livestock.dev`), password constant line 14 `Dev@123456`. E2eRemediationChecklist LoginAsync helper (line 375-376) and line 45 use the identical convention. This is `@livestock.dev` domain — **not** the docs/ASSUMPTIONS.md A7 note about `@example.com`. Domain consistent.

---

## L) Build configuration — csproj properties

All 8 projects share exact same `<PropertyGroup>` except test projects additionally declare `<IsPackable>false</IsPackable> <IsTestProject>true</IsTestProject>` (EndToEndTests additionally `<IsPublishable>false</IsPublishable>`). No project declares `<LangVersion>` (implicitly C# 12 for `TargetFramework net8.0`).

| # | Project file path | TargetFramework | Nullable | ImplicitUsings | LangVersion (effective) |
|---|---|---|---|---|---|
| 1 | `src/LivestockManager.Domain/LivestockManager.Domain.csproj` | `net8.0` | `enable` | `enable` | (default) C# 12 |
| 2 | `src/LivestockManager.Application/LivestockManager.Application.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 3 | `src/LivestockManager.Infrastructure/LivestockManager.Infrastructure.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 4 | `src/LivestockManager.Web/LivestockManager.Web.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 5 | `tests/LivestockManager.UnitTests/LivestockManager.UnitTests.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 6 | `tests/LivestockManager.IntegrationTests/LivestockManager.IntegrationTests.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 7 | `tests/LivestockManager.ArchitectureTests/LivestockManager.ArchitectureTests.csproj` | `net8.0` | `enable` | `enable` | C# 12 |
| 8 | `tests/LivestockManager.EndToEndTests/LivestockManager.EndToEndTests.csproj` | `net8.0` | `enable` | `enable` | C# 12 |

Uniform: 8/8 = net8.0 + Nullable enable. No `LangVersion` explicit overrides (good — no mismatches). No multi-targeting. All package refs: see csproj read results. Key NuGets actually present: `Microsoft.EntityFrameworkCore.SqlServer 8.0.28`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.28`, `Microsoft.Playwright 1.52.0`, `xunit 2.9.2`, `NetArchTest.Rules 1.3.2`, `FluentValidation 11.9.0`. (FluentValidation is present but unused DI registered — Section H #7.)

---

## Next Phase 2 priorities (ranked)

Derived from: `audit/DEFECTS.md` unresolved → Section H NOT-EXISTS gaps → newly discovered issues in Sections B/E/J above.

### Rank 1 — CRITICAL (production blocker): Fix E2E orchestrator's WRONG connection string env key (Section B)
- Run-E2ETests.ps1:244 sets `ConnectionStrings__LivestockManagerDb` but AddDbContext reads `ConnectionStrings__DefaultConnection`. Also design-time factory uses hardcoded string (lines 12-13), so `dotnet ef database update` under E2E env also targets the wrong DB.
- Fix scope (3 edits, < 50 lines total):
  1. `Run-E2ETests.ps1:244` → change env var name to `$env:ConnectionStrings__DefaultConnection = $ConnStringRaw` (keep LIVESTOCK_E2E_* for logging only).
  2. Add `-e ConnectionStrings__DefaultConnection=$ConnStringRaw` arg to the dotnet-ef database update call (line 258) to bypass DesignTimeFactory hardcoded string.
  3. Pass same env var to Start-Process dotnet run calls (lines 270-278 seeds + lines 388-396 long-running web) via `$env:ConnectionStrings__DefaultConnection` set before both starts (already done if edit #1 is correct — just verify via test).
- Regret cost: HIGH. If E2E runs ever touch a shared `LivestockManager` dev DB instead of disposable, test pollution = cross-suite non-deterministic failures + potential accidental data loss of dev data.

### Rank 2 — CRITICAL (docs/source mismatch): DemoDataSeeder flag parsing bug (Section E vs docs REQ-SE-010 MUST)
- 3 axes of mismatch: (a) flag name `EnableDevSeed` vs documented `ENV_ENABLE_DEV_SEED`; (b) value parse is exact `"1"` or exact `"true"` only, NOT bool.TryParse accepting true/1/yes case-insensitive; (c) ASPNETCORE_ENVIRONMENT comparison is case-SENSITIVE (line 29) but docs claim Ordinal Ignore-Case.
- Fix scope DemoDataSeeder.cs lines 22-38:
  1. Add line: `var envEnableDevSeed = Environment.GetEnvironmentVariable("ENV_ENABLE_DEV_SEED") ?? configuration["ENV_ENABLE_DEV_SEED"] ?? configuration["EnableDevSeed"];` then parse with `bool.TryParse(envEnableDevSeed, out var eds1) \|\| (envEnableDevSeed is "1" or "yes")`.
  2. Change `string.Equals(env, "Production", ...)` (line 23) — already OrdinalIgnoreCase, OK. Change line 29 from `==` to `string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase)` etc.
  3. Same for EnableE2ESeed (use `== "1"` OR `bool.TryParse` true OR `== "yes"`). SeedDemoData already `"1"` — also accept "true"/"yes".

### Rank 3 — HIGH (DEF-008 deferred — currently no-op truncation): Add FormattedPdfWriter page-break loop
- Section C evidence: Count=1 hardcoded in AssemblePdf page tree object #2. Currently DrawInvoiceItemsTable + DrawNotesAndTerms break on y<MarginBottom and simply drop overflow.
- Fix: Refactor `AssemblePdf(byte[] contentBytes)` into `AssembleMultiPagePdf(List<byte[]> pageStreams)`. Split content-stream building at `y < MarginBottom + 12 → yield currentStream → new page → y = PageHeight - MarginTop`. Requires adding page objects to the list, `/Kids [3 0 R 7 0 R ...]`, `/Count N`, each page /Parent 2 0 R.
- Estimate: ~150 lines refactor in FormattedPdfWriter. Keep page size Letter / MediaBox same.

### Rank 4 — HIGH (DEF-009 deferred — mojibake risk for non-ASCII names/addresses): Add minimal Unicode PDF subset font
- Today: Type1 Helvetica only. Customers with diacritics (French/German/Spanish/Zulu names common in livestock industry) = `\ooo` octal sequences → missing glyph boxes.
- Minimal viable fix: Register one FreeSerif or NotoSans subset `/Subtype /TrueType` with `/FirstChar /LastChar /Widths /FontFile2` stream or `/CIDFontType2 /DescendantFonts /Identity-H /ToUnicode`. Alternatively: swap StubPdfGenerator registration entirely for a QuestPDF + SkiaSharp (NOT the docs claim QuestPDF is "already there" — it is NOT, add it). Decision: prefer QuestPDF (licensing-compatible Apache 2 Community) because it handles page-wrap (#Rank3) + Unicode in 10 lines versus 500 lines of manual PDF subsetting.
- Trade-off: Adds one new NuGet to the closed allowlist (QuestPDF is pure .NET Standard — no Node/npm). Confirm with user's tech-stack allow-list constraint.

### Rank 5 — HIGH (security gap: ProtectedDocumentStorage .csv no magic bytes): Add CSV header NUL-byte guard
- Section D: DocumentsController allows `.csv` (line 86) + appsettings FileStorage claims 9 extensions including Office types that server-side does NOT enforce, and CSV has NUL-bytes-attack guard documented.
- Fix ProtectedDocumentStorage VerifyMagicBytes `default: break` today. Add:
  ```
  case "csv":
      if (headerLength > 0)
      {
          // Read up to 4096 bytes from tempCopy; if any byte == 0x00 throw.
          // Also allow BOM EF BB BF only. Prevents polyglot script-in-csv.
      }
      break;
  ```
- Align extension list in DocumentsController.cs:86 with appsettings.json:11 FileStorage AllowedExtensions (add .doc/.docx/.xls/.xlsx server side AND add magic bytes for Office ZIP PK\x03\x04 + subfile match [Content_Types].xml per docs/SECURITY §4 claims) OR remove those 4 from the JSON + UI help text. Decision: simpler and safer to remove the 4 Office extensions from JSON until Office magic bytes implemented — don't falsely advertise server-side enforcement that does not exist.

### Rank 6 — MEDIUM: Audit timestamps bypass IDateTime (Section F.3 note)
- Today: AppDbContext SaveChanges uses `DateTimeOffset.UtcNow` directly. Architecture test rule ARCH-012 from docs claims "no DateTime.Now anywhere in src" — which is true, but the stronger injectable-clock requirement is violated for audit entries. Unit tests that need fixed-time audit assertions cannot mock this path.
- Fix: AppDbContext constructor injection `IDateTime dateTime`; replace all 4 direct UtcNow references in UpdateTimestamps / HandleSoftDeletes / PrepareAuditEntries with `_dateTime.Now`. 4 substitutions + 1 ctor + 1 private readonly field. Also run ArchitectureTests rule ARCH-012 to add a disallow `DateTimeOffset.UtcNow` (use IDateTime.Now instead) inside Infrastructure+Domain (allow in Program.cs only).

### Rank 7 — MEDIUM: FluentValidation DLL is in Application.csproj but never DI-registered (Section H #7)
- Today: PackageReference FluentValidation + FluentValidation.DependencyInjectionExtensions (Application.csproj lines 8-9) are present with zero call sites. Dormant DLLs = attack surface.
- Two paths: (A) if no validators written yet → remove 2 PackageReferences (fastest, 2 lines removed); OR (B) `builder.Services.AddValidatorsFromAssemblyContaining<AnyDtoValidator>(ServiceLifetime.Scoped)` inside ServiceCollectionExtensions, and add `services.AddFluentValidationAutoValidation()` to Program.cs controllers options. If validators are being written for the DTOs (59 DTOs count from project memory), keep and activate path B.

### Rank 8 — MEDIUM (DEF-011 deferred): Improve E2E Workflow_01 retry + seed idempotency safeguards
- Current DemoDataSeeder idempotency guard lines 138-165 resets admin@livestock.dev AccessFailedCount/LockoutEnd/password if already exists. But E2ETest Workflow_02 purposely submits 3 invalid logins (lines 73-83) → increments AccessFailedCount → 5th attempt would lock. Tests currently run sequential so next Workflow after _02 = Workflow_03 AsDataEntry login hits the reset on next WebProgram start-up ok? Actually: E2E process reuses ONE long-running web process; DemoDataSeeder ran only once at startup. Between-test login pollution = Workflow_02 invalid attempts raise AccessFailedCount on admin@livestock.dev permanently within the run.
- Fix inside E2ETestBase: After each test that submits invalid logins, call a /seed-reset or direct `E2ETestEnvironment` hook (or add a test-only `[ApiExplorerSettings(IgnoreApi=true)]` controller action gated by `ASPNETCORE_ENVIRONMENT == "Testing"` that runs the same idempotent seeding reset lines 140-164 again). Low-risk; reduces intermittent Workflow_01 first-test flakes.

### Rank 9 — MEDIUM: Upload.cshtml add HTML5 accept filter (Section D note)
- Upload.cshtml line 41 `<input type="file">` has no `accept=`. The JS file picker filters nothing. Users see .exe/.bat/.ps1 offered by default in Windows file picker. Fix: `accept=".pdf,.jpg,.jpeg,.png,.gif,.txt,.csv"` (matches the 7 actually-enforced server-side list, NOT the 9 in JSON config). 1 attribute addition. Zero risk, marginal UX + minor defense-in-depth.

### Rank 10 — P2: DR scheduling gap (transaction-log backups / SQL Agent / PITR)
- Section H #16, #17, #18: backup-database.ps1 only does full backups; no LOG backups; no SQL Agent job scripts; restore-database.ps1 can't do STOPAT point-in-time.
- Fix: add `backup-transaction-log.ps1` using `BACKUP LOG ... WITH NORECOVERY/TRUNCATE_ONLY`, a restore-combine script that accepts STOPAT datetime and chains FULL+DIFF+LOG restores, and a `deploy-sql-agent-jobs.sql` / PowerShell that calls `sp_add_job`/`sp_add_jobschedule` via sqlcmd.

### Rank 11 — P2: Align appsettings.json FileStorage claims vs actual DocumentsController enforcement
- Current: FileStorage section declares 9 extensions + 10485760 bytes but storage class doesn't IOptions-bind. Risk: operator edits appsettings expecting enforcement and gets none.
- Fix: either (a) delete the FileStorage section from JSON (since it's dead config) and rely solely on DocumentsController constants, or (b) wire IOptions<FileStorageSettings> + bind ProtectedDocumentStorage + DocumentsController to read maxSize + allowedExtensions from that config section. Clean architecture favours (b) for production (ops can tweak without rebuild). Also resolves the extension list mismatch (appsettings claims .doc/.docx/.xls/.xlsx server-side-enforced that are NOT enforced today — remove from list or add enforcement per Rank 5).

### Rank 12 — P2: Add FallbackConnection read with retry fallback OR remove key from appsettings.json
- appsettings.json:4 declares `ConnectionStrings:FallbackConnection` with Server=localhost. No C# code reads it. If the intent was graceful "Server=." fails try localhost on named-pipes-denied environments, implement it. Otherwise delete from JSON to avoid audit false-positive confusion. 10 lines tops (inside AddDbContext options builder: try DefaultConnection open, if fail + FallbackConnection non-empty, use Fallback). Or delete 3 lines JSON.

### Rank 13 — P2: 19 doc-claimed features (Company switcher UI, PWA, native mobile, MediatR/CQRS, Generic repos, Azure Blob/S3, break-glass reset, custom report builder, reminders, invites, MFA reset, maintenance mode) are pure documentation with 0 code evidence
- Resolve each in 1 of 2 ways: (A) implement minimum viable feature IF it's truly roadmap-p0 (document in docs/MASTER_PLAN.md Phase 4..11 with actual task IDs); or (B) REMOVE claims from docs where they have 0 code. Today the docs over-promise vs reality — creates production deployment expectations mismatch. Recommendation: Audit scope (this inventory) is NOT to implement features (that's Phase 2+). For PHASE2 first pass: add explicit `[NOT IMPLEMENTED IN MVP]` warning banners at the top of docs/ADMIN_GUIDE §3.3, §5.2, §5.3, §5.4, §5.6; docs/ARCHITECTURE.md sections that list MediatR / QuestPDF / Generic repos; docs/DATABASE.md StoredFiles+OutboundEmails sections; docs/DEPLOYMENT.md §10 SQL Agent scheduling section. This prevents operators from following documented procedures that have zero corresponding backend code and ending in frustration.

---

*End of PHASE1_SOURCE_INVENTORY.md. All items cite source-location file:line where determinable; 0 claims from documentation-only evidence in Sections A–G and I–L. Section H explicitly cross-maps doc claims to EXISTS/NOT EXISTS against actual source.*
