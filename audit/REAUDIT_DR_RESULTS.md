# RE-AUDIT DISASTER RECOVERY & SCRIPT RESULTS

**Re-Audit Date**: August 8, 2026  
**Target Commit**: `75933c8`  
**Disaster Recovery Modules**: `backup-database.cmd` / `Backup-Database.ps1`, `restore-database.cmd` / `Restore-Database.ps1`  
**Disposable Test Databases**: `Livestock_ReauditSource_DB`, `Livestock_ReauditTarget_DB`  

---

## 1. Database Backup Verification (DEF-006 - CLOSED)

### Code & Script Modifications
- **WMIC Elimination**: Legacy `wmic` OS query removed entirely from `backup-database.cmd` and `Backup-Database.ps1`.
- **PowerShell Source of Truth**: `backup-database.cmd` acts as a thin wrapper invoking `scripts/Backup-Database.ps1`.
- **ISO Timestamp Formatting**: Native PowerShell `$stamp = Get-Date -Format "yyyyMMdd_HHmmss"` creates collision-free, safe filenames.
- **Safety Gates Implemented**:
  1. System database rejection (`master`, `model`, `msdb`, `tempdb` blocked).
  2. `sqlcmd` availability check.
  3. SQL Server connectivity and database existence check (`sys.databases`).
  4. Backup directory creation & write permissions test.
  5. Post-backup file existence and non-zero size verification.
  6. Retention purge executed ONLY after successful backup completion.

### Empirical Backup Test Execution Log
- **Target Database**: `Livestock_ReauditSource_DB` (Instantiated via EF Core migrations `InitialMvp`, `Phase2Entities`, `SequencePrefixYearWidth`).
- **Command Executed**: `.\backup-database.cmd` with `SQL_DATABASE=Livestock_ReauditSource_DB`
- **Output File Produced**: `c:\Projects\livestock\artifacts\backups\Livestock_ReauditSource_DB_20260808_185428.bak`
- **File Size**: **545,792 bytes**
- **Log File Created**: `C:\Projects\livestock\artifacts\logs\Backup_20260808_185428.log`
- **Exit Code**: **0 (SUCCESS)**

---

## 2. Database Restore Verification (DEF-007 - CLOSED)

### Code & Script Modifications
- **Existence Check FIRST**: `Restore-Database.ps1` queries `SELECT ISNULL(DB_ID(N'$TargetDatabase'),0)` BEFORE taking any action.
- **Dynamic File Relocation (`WITH MOVE`)**: Runs `RESTORE FILELISTONLY` to inspect logical data/log names, dynamically generating `MOVE` clauses to server default data/log directories (`.mdf` and `.ldf`).
- **Non-Existent Target Handling**: If target database does NOT exist, `Restore-Database.ps1` performs clean `RESTORE DATABASE ... WITH RECOVERY, MOVE ...` without executing invalid `ALTER DATABASE` calls.
- **Overwrite Safeguard**: If target database ALREADY exists and flag `-ConfirmDestructiveOverwrite` / `CONFIRM_RESTORE_OVERWRITE=1` is NOT provided, script logs `target-exists-refuse` and exits immediately with code 5 without modifying the database.
- **Confirmed Overwrite Handling**: When `-ConfirmDestructiveOverwrite` is explicitly supplied, script sets target to `SINGLE_USER`, performs `RESTORE WITH REPLACE, RECOVERY`, and returns to `MULTI_USER`.

### Empirical Restore Test Execution Log

#### Scenario A: Restore Backup to a New Target Database Name
- **Backup Source File**: `c:\Projects\livestock\artifacts\backups\Livestock_ReauditSource_DB_20260808_185428.bak`
- **New Target Database Name**: `Livestock_ReauditTarget_DB` (Did not exist prior to test)
- **Command Executed**: `.\restore-database.cmd` with `SQL_DATABASE=Livestock_ReauditTarget_DB`
- **Execution Log**:
  - `Parsed 2 file(s) from backup (Logical=Livestock_ReauditSource_DB, Type=D; Logical=Livestock_ReauditSource_DB_log, Type=L)`
  - `DB_ID('Livestock_ReauditTarget_DB') = 0`
  - `Executing RESTORE DATABASE [Livestock_ReauditTarget_DB] FROM DISK=... WITH MOVE ...`
  - `RESTORE DATABASE successfully processed 465 pages`
- **Exit Code**: **0 (SUCCESS)**
- **Post-Restore Verification**: Executed `SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES` against `Livestock_ReauditTarget_DB` -> **All 28 tables verified intact**.

#### Scenario B: Overwrite Refusal Test Without Confirmation Flag
- **Target Database**: `Livestock_ReauditTarget_DB` (Already exists)
- **Command Executed**: `.\restore-database.cmd` without `CONFIRM_RESTORE_OVERWRITE`
- **Execution Log**: `DB_ID('Livestock_ReauditTarget_DB') = 10` -> `ERROR target-exists-refuse: Target database 'Livestock_ReauditTarget_DB' already exists. Re-run with -ConfirmDestructiveOverwrite.`
- **Exit Code**: **1 (REFUSED / SAFE TERMINATION)**

#### Scenario C: Confirmed Overwrite Restore Test
- **Target Database**: `Livestock_ReauditTarget_DB` (Already exists)
- **Command Executed**: `.\restore-database.cmd` with `CONFIRM_RESTORE_OVERWRITE=1`
- **Execution Log**: `Target exists + overwrite confirmed: ALTER SINGLE_USER -> RESTORE REPLACE -> ALTER MULTI_USER` -> `RESTORE DATABASE successfully processed 465 pages`
- **Exit Code**: **0 (SUCCESS)**

---

## 3. Database Cleanup Sign-Off
Both disposable test databases (`Livestock_ReauditSource_DB` and `Livestock_ReauditTarget_DB`) were dropped after test completion, leaving the SQL Server instance clean.
