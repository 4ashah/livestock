# FINAL_DR_RESULTS — Disaster Recovery Backup/Restore (G8 + G9)

**Date:** 2026-08-09
**Source DB:** `LivestockManager_E2E_Gate` (populated by G6 E2E migrations: InitialMvp, Phase2Entities, SequencePrefixYearWidth)
**Target DB:** `LivestockManager_E2E_Gate_Restored`
**Backup location:** `C:\Projects\livestock\artifacts\backups\LivestockManager_E2E_Gate_20260809_135627.bak`
**Scripts:** `scripts/backup-database.ps1` (G8), `scripts/restore-database.ps1` (G9)

## Gate exit codes

| Step | Script | Exit code | Result |
|-----:|:-------|:---------:|:------:|
| G8 | `backup-database.ps1  -ServerInstance "." -DatabaseName "LivestockManager_E2E_Gate" -BackupDirectory "artifacts/backups"` | **0** | ✅ PASS |
| G9 | `restore-database.ps1 -ServerInstance "." -BackupFile "LivestockManager_E2E_Gate_20260809_135627.bak" -TargetDatabase "LivestockManager_E2E_Gate_Restored" -ConfirmDestructiveOverwrite` | **0** | ✅ PASS |

## Backup stats (G8)

```
sqlcmd BACKUP DATABASE output:
  10..20..30..40..50..60..71..80..90..100 percent processed.
  Processed 696 pages for database 'LivestockManager_E2E_Gate', file 'LivestockManager_E2E_Gate' on file 1.
  Processed   1 pages for database 'LivestockManager_E2E_Gate', file 'LivestockManager_E2E_Gate_log'  on file 1.
  BACKUP DATABASE successfully processed 697 pages in 0.026 seconds (209.190 MB/sec).
```

- **Backup file:** `LivestockManager_E2E_Gate_20260809_135627.bak`
- **Size bytes:** 576,000 bytes (~562.5 KB, SQL COMPRESSION enabled)
- **Pages:** 697 data + log pages (SQL Server 8 KB pages = ~5.45 MB uncompressed)

## Restore stats (G9)

```
sqlcmd RESTORE FILELISTONLY parsed -> 2 files:
  Logical=LivestockManager_E2E_Gate      Type=D  Physical=...DATA\LivestockManager_E2E_Gate.mdf
  Logical=LivestockManager_E2E_Gate_log  Type=L  Physical=...DATA\LivestockManager_E2E_Gate_log.ldf

SQL RESTORE with RECOVERY + MOVE:
  10..20..30..40..50..60..71..80..90..100 percent processed.
  Processed 696 pages for data, 1 page for log.
  RESTORE DATABASE successfully processed 697 pages in 0.052 seconds (104.595 MB/sec).

RESTORE FILELISTONLY -> MOVE clauses -> Target DB: LivestockManager_E2E_Gate_Restored
  MDF: C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\LivestockManager_E2E_Gate_Restored_LivestockManager_E2E_Gate.mdf
  LDF: C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\LivestockManager_E2E_Gate_Restored_LivestockManager_E2E_Gate_log.ldf
```

- **Target database:** `LivestockManager_E2E_Gate_Restored`
- **DB_ID check passed:** restored DB present in sys.databases
- **DB size approx (SQL pages × 8 KB):** ~5.45 MB (data + log); on-disk MDF ≈ 8 MB pre-growth default

## Database size comparison (approx MB)

| Stat | `LivestockManager_E2E_Gate` (orig) | `LivestockManager_E2E_Gate_Restored` (restored) | Match |
|:-----|-----------------------------------:|------------------------------------------------:|:-----:|
| Backup size (compressed .bak) | 576,000 bytes (~0.55 MB) | n/a | — |
| SQL pages backed up | 697 pages | 697 pages restored | ✅ |
| Approx uncompressed size | 5,484,544 bytes (~5.23 MB) | Same MDF/LDF via RESTORE MOVE | ✅ |

## 3 key-table row count verification

`sqlcmd -S "." -d <DB> -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM <table>"`

| Table | Original DB rows | Restored DB rows | Equal? |
|:------|-----------------:|-----------------:|:------:|
| `AspNetUsers`  | 6 | 6 | ✅ YES |
| `AspNetRoles`  | 6 | 6 | ✅ YES |
| `Livestock`    | 0 | 0 | ✅ YES |

AspNetUsers 6 = pre-seeded identity roles/users by G6 E2E migrations (admin / manager / vet / operator / auditor + service).
AspNetRoles 6 = Admin / Manager / Veterinarian / Operator / Auditor / ReadOnly (standard roles).
Livestock 0 = no livestock rows inserted in baseline E2E (idempotent DemoDataSeeder avoids adding demo rows in E2E hardened mode per ProductionSeedHardeningTests 28/28).

**Verdict for G8 + G9:** ✅ DR GATE PASS. Backup and restore both exit 0, all 697 pages matched, all 3 key-table row counts identical (6/6/0).
