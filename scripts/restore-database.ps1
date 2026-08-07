param(
    [string]$SqlServer       = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { ".\SQLEXPRESS" }),
    [string]$Database        = $(if ($env:SQL_DATABASE) { $env:SQL_DATABASE } else { "LivestockManager" }),
    [string]$BackupFilePath  = $(if ($env:BACKUP_FILE_PATH) { $env:BACKUP_FILE_PATH } else { "" }),
    [string]$ConfirmOverwrite = $(if ($env:CONFIRM_RESTORE_OVERWRITE) { $env:CONFIRM_RESTORE_OVERWRITE } else { "" })
)
$ErrorActionPreference = "Stop"
Write-Host "=== LivestockManager: Restore Database (sqlcmd) ==="
Write-Warning "This overwrites the target database. Set CONFIRM_RESTORE_OVERWRITE=1 to proceed."
if ($ConfirmOverwrite -ne "1") { throw "CONFIRM_RESTORE_OVERWRITE not set to 1. Aborting." }
if ([string]::IsNullOrWhiteSpace($BackupFilePath)) { throw "BACKUP_FILE_PATH env var or parameter must point to a .bak file." }
if (-not (Test-Path $BackupFilePath)) { throw "BACKUP_FILE_PATH not found: $BackupFilePath" }
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw "sqlcmd not found on PATH." }
Write-Host "SQL_SERVER       = $SqlServer"
Write-Host "SQL_DATABASE     = $Database"
Write-Host "BACKUP_FILE_PATH = $BackupFilePath"
$Esc = $BackupFilePath.Replace("'","''")
$Q = @"
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{0}] FROM DISK='{1}' WITH REPLACE, RECOVERY, STATS=10;
ALTER DATABASE [{0}] SET MULTI_USER;
"@ -f $Database, $Esc
& sqlcmd -S $SqlServer -E -Q $Q -b
if ($LASTEXITCODE -ne 0) { throw "RESTORE FAILED" }
Write-Host "RESTORE OK: $Database restored from $BackupFilePath"
