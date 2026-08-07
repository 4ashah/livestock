# Defects Log

## Active Defects

| ID | Title | Phase/Module | Severity | Priority | Status | Reported By | Reported | Notes |
|----|-------|--------------|----------|----------|--------|-------------|----------|-------|
|    |       |              |          |          |        |             |          |       |

## Resolved Defects

| ID | Title | Phase/Module | Severity | Resolved By | Resolution | Fixed Commit |
|----|-------|--------------|----------|-------------|------------|--------------|
| D-001 | EfSequenceGenerator ADO.NET transaction not assigned to DbCommand → InvalidOperationException "ExecuteScalar requires the command to have a transaction..." when command runs under ambient RepeatableRead transaction on SequenceCounters | S4 / Infrastructure / EfSequenceGenerator | Critical | Infra Team | Rewrote generator to open direct `DbConnection.BeginTransactionAsync(IsolationLevel.RepeatableRead)` and explicitly assign `dbCommand.Transaction = transaction` before `ExecuteScalarAsync`; guard with using/rollback on error. | (MVP v1 commit) |
| D-002 | SequenceCounter (singular) table-name mismatch vs pluralized `DbSet<SequenceCounter>` → CREATE/UPDATE targeted wrong table name, sequential ID increment failed | S4 / Infrastructure / EfSequenceGenerator | Critical | Infra Team | Renamed SQL identifiers inside the rowlock+UPDLOCK+HOLDLOCK UPDATE/INSERT statement from `SequenceCounter` to `SequenceCounters` to match EF pluralized table name. | (MVP v1 commit) |
| D-003 | AuditLog `Action` field always empty string → empty-Action guard in `CreateAuditLogsAsync` filtered out every audit, producing 0-row AuditLogs table for all prior saves | S2 / AppDbContext.CreateAuditLogsAsync | High | Infra Team | Moved `Action = Entry.State switch` assignment BEFORE the per-property `foreach (property in ...)` loop so Action is non-empty / populated BEFORE the guard checks whether property-level value pairs exist. | (MVP v1 commit) |
| D-004 | Dashboard RuntimeBinderException: `'decimal' does not contain a definition for 'HasValue'` when rendering recent-livestock table row | S8 / HomeController.Index + Views/Home/Index.cshtml | High | Web Team | Replaced `ViewData["RecentLivestock"]` anonymous-object projection (which inferred `CurrentWeight` non-nullable `decimal`) with strongly-typed `LivestockSummaryDto` (declares `decimal? CurrentWeight`); changed razor from `(IEnumerable<dynamic>)recentLivestock` to explicit `IEnumerable<LivestockSummaryDto>` with `@using LivestockManager.Application.DTOs.Livestock` + `.Any()` null-guarded loop. Invoice summary block fixed identically. | (MVP v1 commit) |
| D-005 | AuditLogs COUNT(*) = 0 even after D-003 Action-ordering fix | S2 / AppDbContext.CreateAuditLogsAsync | High | Infra Team | Replaced the guard condition `AuditLogs.Local.Any(a => a.Id == Guid.Empty)` (always false: EF auto-generates a non-empty Guid PK for entities marked `ValueGenerated.OnAdd` the moment `.Add()` is called) with `AuditLogs.Local.Count > 0` which correctly counts pending local inserts regardless of key-generation strategy. | (MVP v1 commit) |
| D-006 | LivestockTypeId enum = 0 default binding → `ArgumentOutOfRangeException: Actual value was 0. (Parameter 'type')` in EfSequenceGenerator.GetPrefix switch on POST /livestock/register | S4 / LivestockRegisterViewModel | High | Web Team | Added auto-property initializer `= LivestockType.Ah` to `LivestockTypeId` on `LivestockRegisterViewModel` so GET renders the Ah radio pre-selected; snapshot verified `states: [checked, readonly]` on first load, ASP.NET binder now receives valid value 1 (Ah) even if user submits untouched. | (MVP v1 commit) |
| D-007 | AuditLogs still persisted = 0 rows AFTER fixing D-005; real root: `base.SaveChangesAsync()` resets EntityState `Added → Unchanged` and `Deleted → Modified/Unchanged`, then the `CreateAuditLogsAsync` Action switch re-reads `Entry.State` → gets `Unchanged` → switch arm `_ => auditEntry.Action` (null) → `if (Action.IsNullOrWhiteSpace) continue` drops every entry. Also `HandleSoftDeletes()` sets `Deleted → Modified` AFTER PrepareAuditEntries so Delete events would be mislabeled "Update" if Action were set after HandleSoftDeletes. | S2 / AppDbContext.SaveChangesAsync pipeline ordering | Critical | Infra Team | Moved the `auditEntry.Action = auditEntry.Entry.State switch { Added => "Create", ... }` loop to **immediately after `PrepareAuditEntries()` and BEFORE `UpdateTimestamps()` / `HandleSoftDeletes()` / `base.SaveChangesAsync()`**. Then inside `CreateAuditLogsAsync`, guard the switch with `if (string.IsNullOrWhiteSpace(auditEntry.Action))` as a safe fallback, so the pre-captured state is never overwritten by stale post-Save `Entry.State`. | (MVP v1 commit) |
| D-008 | PowerShell variable name `$pid` is an automatic reserved constant → `Cannot overwrite variable PID because it is read-only or constant` when killing port-5100 listener processes | DevOps / Port-kill script (local session) | Medium | Infra Team | Renamed local process-ID variable from `$pid` to `$processId` throughout the netstat-based kill loop. | (MVP v1 commit) |
| D-009 | Chromium error `ERR_UNSAFE_PORT`: originally requested port 6000 is on Chromium's unsafe-port blocklist → browser refuses to connect to any service listening on 6000 locally | DevOps / ASPNETCORE_URLS (original user-requested port) | Medium | Infra Team | Switched Kestrel listen URL to `http://localhost:5100` and updated all documentation/STATE.md/resume instructions; this port is well outside Chromium's 1–1024 + restricted well-known-port blocklist and is not proxied/conflicted. | (MVP v1 commit) |

## Severity Definitions
- **Critical**: Blocks progress, data loss, crash
- **High**: Incorrect behavior, no workaround
- **Medium**: Incorrect behavior with workaround, UI issue
- **Low**: Cosmetic, typo, minor UX

## Status Definitions
- **New**: Reported, not triaged
- **Triaged**: Prioritized, assigned
- **In Progress**: Fix being worked on
- **Resolved**: Fix applied, awaiting verification
- **Verified**: Tested and confirmed fixed
- **Rejected**: Not a defect, duplicate, or out of scope
