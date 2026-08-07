param(
    [string]$SqlServer = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { ".\SQLEXPRESS" }),
    [string]$Database  = $(if ($env:SQL_DATABASE) { $env:SQL_DATABASE } else { "LivestockManager" }),
    [string]$BackupDir = $(if ($env:BACKUP_DIR) { $env:BACKUP_DIR } else { (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\backups") }),
    [int]$RetentionDays = $(if ($env:RETENTION_DAYS) { [int]$env:RETENTION_DAYS } else { 0 })
)
$ErrorActionPreference = "Stop"
Write-Host "=== LivestockManager: Backup Database (sqlcmd) ==="
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { throw "sqlcmd not found on PATH. Install SQL Server Command Line Utilities." }
if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupFile = Join-Path $BackupDir ("{0}-{1}.bak" -f $Database, $Stamp)
Write-Host "SQL_SERVER  = $SqlServer"
Write-Host "SQL_DATABASE= $Database"
Write-Host "BACKUP_FILE = $BackupFile"
$Q = "BACKUP DATABASE [{0}] TO DISK='{1}' WITH INIT, COMPRESSION, STATS=10;" -f $Database, $BackupFile.Replace("'","''")
& sqlcmd -S $SqlServer -E -Q $Q -b
if ($LASTEXITCODE -ne 0) { throw "BACKUP FAILED" }
Write-Host "BACKUP OK: $BackupFile"
if ($RetentionDays -gt 0) {
    Write-Host "Purging backups older than $RetentionDays days..."
    $Cutoff = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupDir -Recurse -Filter *.bak | Where-Object { $_.LastWriteTime -lt $Cutoff } | Remove-Item -Force -ErrorAction SilentlyContinue
}
