param(
    [Parameter(Mandatory=$true)]
    [string]$ServerInstance,

    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[A-Za-z0-9_@#$-]+$')]
    [string]$DatabaseName,

    [string]$BackupDirectory = "./artifacts/backups",

    [int]$RetentionDays = -1,

    [string]$LogDirectory = "./artifacts/logs"
)

$ErrorActionPreference = "Stop"

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
if (-not (Test-Path (Join-Path $RepoRoot "LivestockManager.sln"))) {
    Write-Error "FATAL: RepoRoot detection failed. Expected LivestockManager.sln under: $RepoRoot"
    exit 99
}

function Resolve-AbsoluteFromRepo {
    param([string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    } else {
        $combined = Join-Path $RepoRoot $Path
        return [System.IO.Path]::GetFullPath($combined)
    }
}

$BackupDirectoryFull = Resolve-AbsoluteFromRepo $BackupDirectory
$LogDirectoryFull = Resolve-AbsoluteFromRepo $LogDirectory

New-Item -ItemType Directory -Force -Path $LogDirectoryFull | Out-Null
$logFile = Join-Path $LogDirectoryFull ("Backup_{0}.log" -f $stamp)

function Write-Log {
    param([string]$Level, [string]$Message)
    $line = "{0} {1} {2}" -f (Get-Date -Format s), $Level, $Message
    Write-Output $line
    Add-Content -Path $logFile -Value $line
}

Write-Log "INFO" "=== Database Backup Start ==="
Write-Log "INFO" "RepoRoot (detected): $RepoRoot"
Write-Log "INFO" "ServerInstance: $ServerInstance"
Write-Log "INFO" "DatabaseName:   $DatabaseName"
Write-Log "INFO" "BackupDirectory (user): $BackupDirectory"
Write-Log "INFO" "BackupDirectory (absolute resolved): $BackupDirectoryFull"
Write-Log "INFO" "RetentionDays:  $RetentionDays"
Write-Log "INFO" "LogDirectory (absolute resolved): $LogDirectoryFull"

$systemDbs = @('master', 'model', 'msdb', 'tempdb')
if ($systemDbs -contains $DatabaseName) {
    Write-Log "ERROR" "system-database-protected: Database '$DatabaseName' is a system database and cannot be backed up via this script."
    Write-Error -ErrorAction Continue "system-database-protected: Database '$DatabaseName' is a system database."
    exit 10
}
Write-Log "INFO" "System database check passed."

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Log "ERROR" "sqlcmd not found on PATH. Install SQL Server Command Line Utilities."
    Write-Error -ErrorAction Continue "sqlcmd not found on PATH."
    exit 11
}
Write-Log "INFO" "sqlcmd on PATH check passed."

$checkDbQuery = "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name = N'$DatabaseName'"
Write-Log "INFO" "Testing SQL connectivity and database existence..."
$checkResult = & sqlcmd -S $ServerInstance -E -b -Q $checkDbQuery 2>&1
$checkExit = $LASTEXITCODE
if ($checkExit -ne 0 -or ($checkResult -join "`n") -notmatch [regex]::Escape($DatabaseName)) {
    Write-Log "ERROR" "cannot-connect-or-missing (exit=$checkExit): Cannot connect to SQL Server or database '$DatabaseName' does not exist."
    Write-Error -ErrorAction Continue "cannot-connect-or-missing: Cannot connect to SQL Server or database '$DatabaseName' does not exist."
    exit 12
}
Write-Log "INFO" "SQL connectivity and existence check passed."

try {
    New-Item -ItemType Directory -Force -Path $BackupDirectoryFull | Out-Null
} catch {
    Write-Log "ERROR" "backup-dir-not-writable: Failed to create BackupDirectory '$BackupDirectoryFull'."
    Write-Error -ErrorAction Continue "backup-dir-not-writable: Failed to create BackupDirectory."
    exit 13
}

$probePath = Join-Path $BackupDirectoryFull ".write-probe.tmp"
try {
    New-Item -ItemType File -Path $probePath -Value "ok" -Force | Out-Null
    Remove-Item $probePath -Force
} catch {
    Write-Log "ERROR" "backup-dir-not-writable: Write-probe test failed on BackupDirectory '$BackupDirectoryFull'."
    Write-Error -ErrorAction Continue "backup-dir-not-writable: Write-probe test failed."
    if (Test-Path $probePath) { Remove-Item $probePath -Force -ErrorAction SilentlyContinue }
    exit 13
}
Write-Log "INFO" "BackupDirectory created and writable (write-probe passed)."

$BackupFileFull = Join-Path $BackupDirectoryFull ("{0}_{1}.bak" -f $DatabaseName, $stamp)
Write-Log "INFO" "BackupFile (absolute): $BackupFileFull"

$backupQuery = "BACKUP DATABASE [$DatabaseName] TO DISK=N'$($BackupFileFull.Replace("'","''"))' WITH INIT, COMPRESSION, STATS=10;"
Write-Log "INFO" "Executing BACKUP DATABASE (absolute path to SQL Server)..."
& sqlcmd -S $ServerInstance -E -b -Q $backupQuery
$backupExit = $LASTEXITCODE
if ($backupExit -ne 0) {
    Write-Log "ERROR" "backup-failed-sql (exit=$backupExit): BACKUP DATABASE command failed."
    if (Test-Path $BackupFileFull) {
        Write-Log "WARN" "Removing partially written backup file: $BackupFileFull"
        Remove-Item $BackupFileFull -Force -ErrorAction SilentlyContinue
    }
    Write-Error -ErrorAction Continue "backup-failed-sql: BACKUP command failed with exit $backupExit."
    exit 14
}
Write-Log "INFO" "BACKUP DATABASE sqlcmd returned 0."

if (-not (Test-Path $BackupFileFull) -or ((Get-Item $BackupFileFull).Length -le 0)) {
    Write-Log "ERROR" "backup-zero-length: Backup file missing or zero length."
    Write-Error -ErrorAction Continue "backup-zero-length: Backup file missing or zero length."
    exit 15
}
$bkSize = (Get-Item $BackupFileFull).Length
Write-Log "INFO" "Backup verified. Size = $bkSize bytes."

if ($RetentionDays -gt 0) {
    Write-Log "INFO" "Applying retention policy: older than $RetentionDays days."
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    $filter = $DatabaseName + "_*.bak"
    $toRemove = Get-ChildItem -Path $BackupDirectoryFull -Filter $filter -File | Where-Object { $_.LastWriteTime -lt $cutoff }
    foreach ($f in $toRemove) {
        Write-Log "INFO" "Retention purge: $($f.FullName) ($($f.LastWriteTime))"
        Remove-Item $f.FullName -Force -ErrorAction SilentlyContinue
    }
    Write-Log "INFO" "Retention policy applied. $($toRemove.Count) file(s) purged."
} else {
    Write-Log "INFO" "RetentionDays <= 0, skipping retention purge."
}

Write-Log "INFO" "BACKUP OK: $BackupFileFull"
Write-Output "BACKUP OK: $BackupFileFull"
exit 0
