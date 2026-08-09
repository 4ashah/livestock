# ROADMAP — Future Features (NOT IMPLEMENTED)

This document lists features that prior documentation claimed were implemented but were **PROVEN NOT EXISTS** in source code via audit/PHASE1_SOURCE_INVENTORY.md §H (22 of 23 doc-claimed features; 1 partial with 0 call sites).

Each feature is labeled **NOT IMPLEMENTED — planned/aspirational** with one-line evidence of absence.

---

## Future Features — NOT IMPLEMENTED

| # | Feature Name | Status | Evidence of Absence |
|---|---|---|---|
| 1 | **Hangfire dashboard at `/hangfire`** (SystemAdmin only, jobs/queues/processing/succeeded/failed/recurring) | NOT IMPLEMENTED — planned/aspirational | No Hangfire NuGet package, no `services.AddHangfire`/`MapHangfireDashboard` registration, no `/hangfire` route in Program.cs or controllers. |
| 2 | **SendGrid email API + SendGridEmailSender class** | NOT IMPLEMENTED — planned/aspirational | No SendGrid NuGet package, no `SendGrid` string in any .cs/csproj, no `Email__SendGridApiKey` config binder, no Email section in appsettings. |
| 3 | **SMTP queue / HangfireEmailQueue / background email retry (3× exponential backoff)** | NOT IMPLEMENTED — planned/aspirational | No SmtpClient/Smtp/Queue in services, no BackgroundService/IHostedService in any .cs, no OutboundEmails DbSet in AppDbContext (26 entity sets listed, none match). |
| 4 | **Azure Blob storage** (`IFileStorage → AzureBlobFileStorage : IFileStorage`) + Azure.Storage.Blobs | NOT IMPLEMENTED — planned/aspirational | No Azure.Storage.Blobs NuGet, no BlobContainerClient/AzureBlobFileStorage/StorageProvider references, no IFileStorage interface (only IProtectedDocumentStorage exists). |
| 5 | **Amazon S3 storage provider** + AWSSDK.S3 + StoredFiles StorageProvider column (LOC/AZB/S3) | NOT IMPLEMENTED — planned/aspirational | No AWSSDK/S3Client/Minio in source, no StoredFiles DbSet in AppDbContext, zero StorageProvider enum in Domain/Enums/. |
| 6 | **QuestPDF rendering** (`QuestPdfGenerator : IPdfGenerator`) + QuestPDF NuGet | NOT IMPLEMENTED — planned/aspirational | Zero QuestPDF NuGet in 8 csproj files, zero `QuestPDF`/`QuestPdf` in source; IPdfGenerator implemented only by FormattedPdfWriter + StubPdfGenerator. |
| 7 | **MediatR CQRS pipeline** with FluentValidation behaviors (MediatR + MediatR DI + IPipelineBehavior) | NOT IMPLEMENTED — planned/aspirational | No MediatR NuGet, zero `IMediator`/`Send(`/`Publish(` in any .cs; Application layer uses direct interfaces, not IRequest/IRequestHandler. |
| 8 | **Generic Repository pattern** `IRepository<T>` + `IUnitOfWork` | NOT IMPLEMENTED — planned/aspirational | Zero `IRepository`/`UnitOfWork` strings in src/; data access is direct `AppDbContext.Set<T>()` + DbSet<T>; IAppDbContext facade only has SaveChanges/BeginTransaction. |
| 9 | **Company impersonation / switcher** (SystemAdmin dropdown → Session ImpersonatingCompanyId; full cross-company context) | NOT IMPLEMENTED — planned/aspirational (PARTIAL skeleton only; 0 call sites) | Application interface `ICompanyService.SwitchCompanyAsync` declared but zero controller route calls it; zero `SwitchCompany`/`Impersonat` in Web layer; _Layout sidebar has no switcher UI; zero Session usage for ImpersonatingCompanyId. |
| 10 | **Maintenance mode** (enable: all POST → 503 except SysAdmin) | NOT IMPLEMENTED — planned/aspirational | Only `ExpenseCategory.Maintenance = 7` enum value exists; zero middleware, zero `IMiddleware`, zero MaintenanceMode/app.UseWhen+503 filter, zero [Maintenance] attribute, zero settings toggle. |
| 11 | **User invitations** (SysAdmin → Users tab → +Invite User; 24h one-time link) | NOT IMPLEMENTED — planned/aspirational | Zero `Invite`/`Invitation`/`UserInvit` strings in src/, no Account action=Invite, no token generation, no email dispatch (requires absent email channel). |
| 12 | **MFA reset workflow** for lost phone (Reset TOTP authenticator) | NOT IMPLEMENTED — planned/aspirational | Zero `ResetMfa`/`Reset2FA`/`TOTP`/`ResetAuthenticator`/`TwoFactor` tokens/reset in .cs or controllers; AccountController has zero MFA actions. |
| 13 | **Per-company storage quota enforcement** (50 GB default) | NOT IMPLEMENTED — planned/aspirational | Zero `Quota`/`StorageQuota` in src/; ProtectedDocumentStorage never sums or checks company usage; no byte-sum aggregation; no StorageUsedBytes on Company entity. |
| 14 | **Break-glass reset document counter** + Manually Set Invoice Number | NOT IMPLEMENTED — planned/aspirational | Zero `ResetSequence`/`BreakGlass`/`ManualInvoice`/`SetInvoiceNumber` in src/; no controller action exposes SequenceCounters edits; only mutation path is EfSequenceGenerator.GenerateNumberAsync atomic UPDATE/INSERT with no admin edit route. |
| 15 | **Password-force-change workflow** (post-login redirect if MustChangePassword flag) | NOT IMPLEMENTED — planned/aspirational | Zero `ForceChangePassword`/`MustChangePassword` in src/; ApplicationUser class has no such boolean property; no `OnSignedIn`/SignInManager hook redirect in AccountController. |
| 16 | **Transaction-log backup scripts** (TLOG backups) | NOT IMPLEMENTED — planned/aspirational | backup-database.ps1 ONLY performs `BACKUP DATABASE` (full); no `BACKUP LOG`/TSQL code anywhere; no PowerShell script for LOG backups. |
| 17 | **SQL Server Agent jobs** (Full/Diff/TLOG schedule) | NOT IMPLEMENTED — planned/aspirational | No .sql scripts for SQL Agent jobs, no sp_add_job/sp_add_schedule calls, no MSDB configuration in backup/restore PS1 scripts; deployment docs claim scheduling but source has zero automation. |
| 18 | **Point-in-time restore** with tail-log / STOPAT | NOT IMPLEMENTED — planned/aspirational | restore-database.ps1 ONLY uses `RESTORE DATABASE FROM DISK WITH RECOVERY, MOVE...`; no `RESTORE LOG` + `STOPAT = 'datetime'`; no tail-log-backup step before restore. |
| 19 | **Email reminders** (auto 3× retry reminder) | NOT IMPLEMENTED — planned/aspirational | Zero `Reminder`/`RecurringJob`/`IHostedService`/`BackgroundService` in src/; also requires absent email channel (#1/#2/#3 above). |
| 20 | **Custom report builder UI + XLSX multi-sheet export** | NOT IMPLEMENTED — planned/aspirational (Reports page UI placeholder exists but no backend) | Reports/Details.cshtml line 10 has UI placeholder text only ("Report chart, table, filters, export as PDF/XLSX/CSV."); no ClosedXML/EPPlus/NPOI/Worksheet NuGet; no .xlsx byte array generation in Services/; CSV only via CsvExporter (no multi-sheet). |
| 21 | **Native/offline mobile app** | NOT IMPLEMENTED — planned/aspirational | No .csproj for MAUI/Xamarin/Android/iOS, no manifest, no native resources; only one web csproj. |
| 22 | **PWA install** (manifest, service worker, offline cache) | NOT IMPLEMENTED — planned/aspirational | No manifest.json, no service-worker.js, no sw registration in site.js or _Layout; no `<link rel="manifest">` tag. |
| 23 | **App Store availability** (iOS/Android store listings) | NOT IMPLEMENTED — planned/aspirational | No listing assets, no Fastlane, no build targets for iOS/Android; pure web. |

---

## Summary

- **NOT IMPLEMENTED (full):** 22 of 23 doc-claimed features
- **NOT IMPLEMENTED (partial/skeleton with 0 call sites):** 1 (Company impersonation — interface exists with zero callers)
- **Total doc-claimed features NOT in source:** 22 + 1 partial = 23

Cross-reference: See `docs/IMPLEMENTED_FEATURES.md` for the authoritative list of features that DO exist with source-level evidence.
