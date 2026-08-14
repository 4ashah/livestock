param(
    [Parameter(Mandatory=$true)]
    [string]$ServerInstance,

    [Parameter(Mandatory=$true)]
    [string]$BackupFile,

    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[A-Za-z0-9_@#$-]+$')]
    [string]$TargetDatabase,

    [string]$DataFileDirectory = "",

    [string]$LogDirectory = "./artifacts/logs",

    [switch]$ConfirmDestructiveOverwrite
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
    if ([string]::IsNullOrWhiteSpace($Path)) { return "" }
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    } else {
        $combined = Join-Path $RepoRoot $Path
        return [System.IO.Path]::GetFullPath($combined)
    }
}

$LogDirectoryFull = Resolve-AbsoluteFromRepo $LogDirectory
if (-not [string]::IsNullOrWhiteSpace($DataFileDirectory)) {
    $DataFileDirectory = Resolve-AbsoluteFromRepo $DataFileDirectory
}
$BackupFile = Resolve-AbsoluteFromRepo $BackupFile

New-Item -ItemType Directory -Force -Path $LogDirectoryFull | Out-Null
$logFile = Join-Path $LogDirectoryFull ("Restore_{0}.log" -f $stamp)

function Write-Log {
    param([string]$Level, [string]$Message)
    $line = "{0} {1} {2}" -f (Get-Date -Format s), $Level, $Message
    Write-Output $line
    Add-Content -Path $logFile -Value $line
}

Write-Log "INFO" "=== Database Restore Start ==="
Write-Log "INFO" "RepoRoot (detected): $RepoRoot"
Write-Log "INFO" "ServerInstance:          $ServerInstance"
Write-Log "INFO" "BackupFile (resolved):   $BackupFile"
Write-Log "INFO" "TargetDatabase:          $TargetDatabase"
Write-Log "INFO" "DataFileDirectory:       '$DataFileDirectory'"
Write-Log "INFO" "ConfirmDestructiveOverwrite: $ConfirmDestructiveOverwrite"

$systemDbs = @('master', 'model', 'msdb', 'tempdb')
if ($systemDbs -contains $TargetDatabase) {
    Write-Log "ERROR" "system-database-protected: TargetDatabase '$TargetDatabase' is a system database."
    Write-Error -ErrorAction Continue "system-database-protected: TargetDatabase '$TargetDatabase' is a system database."
    exit 10
}
Write-Log "INFO" "System database check passed."

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Log "ERROR" "sqlcmd not found on PATH."
    Write-Error -ErrorAction Continue "sqlcmd not found on PATH."
    exit 11
}
Write-Log "INFO" "sqlcmd on PATH check passed."

if (-not (Test-Path $BackupFile)) {
    Write-Log "ERROR" "missing-backup: BackupFile '$BackupFile' does not exist."
    Write-Error -ErrorAction Continue "missing-backup: BackupFile not found."
    exit 3
}
Write-Log "INFO" "BackupFile existence check passed."

$bfItem = Get-Item $BackupFile
if ($bfItem.Extension -ne '.bak' -or $bfItem.Length -le 0) {
    Write-Log "ERROR" "invalid-backup-file: Extension='$($bfItem.Extension)', Length=$($bfItem.Length)."
    Write-Error -ErrorAction Continue "invalid-backup-file: File must be .bak with non-zero length."
    exit 4
}
Write-Log "INFO" "BackupFile extension/size check passed. Length=$($bfItem.Length)."

if ($DataFileDirectory -ne "") {
    try {
        New-Item -ItemType Directory -Force -Path $DataFileDirectory | Out-Null
    } catch {
        Write-Log "ERROR" "data-dir-create-failed: Cannot create DataFileDirectory '$DataFileDirectory'."
        Write-Error -ErrorAction Continue "data-dir-create-failed: Cannot create DataFileDirectory."
        exit 18
    }
}

Write-Log "INFO" "Step 0: Verify backup integrity via RESTORE VERIFYONLY..."
$verifyOnlyQuery = "RESTORE VERIFYONLY FROM DISK=N'$($BackupFile.Replace("'","''"))' WITH STATS=5;"
& sqlcmd -S $ServerInstance -E -b -Q $verifyOnlyQuery
$voExit = $LASTEXITCODE
if ($voExit -ne 0) {
    Write-Log "ERROR" "verifyonly-failed (exit=$voExit): Backup file failed RESTORE VERIFYONLY."
    Write-Error -ErrorAction Continue "verifyonly-failed: Backup file failed RESTORE VERIFYONLY — aborting restore."
    exit 19
}
Write-Log "INFO" "RESTORE VERIFYONLY passed."

Write-Log "INFO" "Step 1: Enumerate files via RESTORE FILELISTONLY..."
$flQuery = "RESTORE FILELISTONLY FROM DISK=N'$($BackupFile.Replace("'","''"))';"
$flRaw = & sqlcmd -S $ServerInstance -E -b -W -h -1 -s "|" -Q $flQuery 2>&1
$flExit = $LASTEXITCODE
if ($flExit -ne 0) {
    Write-Log "ERROR" "filelistonly-failed (exit=$flExit): RESTORE FILELISTONLY failed."
    Write-Error -ErrorAction Continue "filelistonly-failed: RESTORE FILELISTONLY failed with exit $flExit."
    exit 16
}

$fileList = @()
foreach ($line in $flRaw) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -match "rows affected") { continue }
    $cols = $line -split "\|"
    if ($cols.Count -ge 3 -and $cols[2] -match "^[DLFS]$") {
        $fileList += [PSCustomObject]@{
            Logical  = $cols[0].Trim()
            Physical = $cols[1].Trim()
            Type     = $cols[2].Trim()
        }
    }
}
if ($fileList.Count -eq 0) {
    Write-Log "ERROR" "filelist-empty: No files parsed from RESTORE FILELISTONLY output."
    Write-Error -ErrorAction Continue "filelist-empty: RESTORE FILELISTONLY returned no parseable rows."
    exit 16
}
Write-Log "INFO" "Parsed $($fileList.Count) file(s) from backup:"
foreach ($f in $fileList) {
    Write-Log "INFO" "  Logical=$($f.Logical)  Type=$($f.Type)  Physical=$($f.Physical)"
}

Write-Log "INFO" "Step 2: Build MOVE clauses..."
$defaultDataDir = ""
$defaultLogDir = ""
if ($DataFileDirectory -eq "") {
    $ddQuery = "SET NOCOUNT ON; SELECT ISNULL(CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(512)),N''), ISNULL(CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(512)),N'');"
    $ddRaw = & sqlcmd -S $ServerInstance -E -b -W -h -1 -s "|" -Q $ddQuery 2>&1
    foreach ($l in $ddRaw) {
        if ([string]::IsNullOrWhiteSpace($l)) { continue }
        if ($l -match "rows affected") { continue }
        $ddCols = $l -split "\|"
        if ($ddCols.Count -ge 2) {
            $defaultDataDir = $ddCols[0].Trim()
            $defaultLogDir = $ddCols[1].Trim()
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($defaultDataDir)) { $defaultDataDir = Split-Path -Parent $fileList[0].Physical }
    if ([string]::IsNullOrWhiteSpace($defaultLogDir)) { $defaultLogDir = Split-Path -Parent $fileList[$fileList.Count-1].Physical }
    Write-Log "INFO" "Using server default dirs DataDir=$defaultDataDir ; LogDir=$defaultLogDir"
}

$moveClauses = @()
foreach ($f in $fileList) {
    $ext = [System.IO.Path]::GetExtension($f.Physical)
    if ($DataFileDirectory -ne "") {
        if ($f.Type -eq "D") {
            $newName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, ".mdf"
        } elseif ($f.Type -eq "L") {
            $newName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, ".ldf"
        } else {
            $newName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, $ext
        }
        $NewPhysical = Join-Path $DataFileDirectory $newName
    } else {
        if ($f.Type -eq "L") {
            $useDir = $defaultLogDir
            $fileName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, ".ldf"
        } else {
            $useDir = $defaultDataDir
            if ($f.Type -eq "D") {
                $fileName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, ".mdf"
            } else {
                $fileName = "{0}_{1}{2}" -f $TargetDatabase, $f.Logical, $ext
            }
        }
        if ($useDir -and -not $useDir.EndsWith("\")) { $useDir = $useDir + "\" }
        $NewPhysical = [System.IO.Path]::GetFullPath($useDir + $fileName)
    }
    $moveClauses += "MOVE N'$($f.Logical.Replace("'","''"))' TO N'$($NewPhysical.Replace("'","''"))'"
    Write-Log "INFO" "  MOVE $($f.Logical) -> $NewPhysical"
}
$moveClauseString = ""
if ($moveClauses.Count -gt 0) {
    $moveClauseString = ", " + ($moveClauses -join ", ")
}

Write-Log "INFO" "Step 3: Check if target database exists..."
$existsQuery = "SET NOCOUNT ON; SELECT ISNULL(DB_ID(N'$TargetDatabase'),0)"
$targetExistsRaw = & sqlcmd -S $ServerInstance -E -b -W -h -1 -Q $existsQuery 2>&1
$teExit = $LASTEXITCODE
$targetExists = "0"
if ($teExit -eq 0) {
    foreach ($l in $targetExistsRaw) {
        if ($l -match "^\s*(\d+)\s*$") { $targetExists = $matches[1]; break }
    }
}
Write-Log "INFO" "DB_ID(N'$TargetDatabase') = $targetExists"

if ($targetExists -ne "0" -and -not $ConfirmDestructiveOverwrite) {
    Write-Log "ERROR" "target-exists-refuse: Target database '$TargetDatabase' already exists. Re-run with -ConfirmDestructiveOverwrite."
    Write-Error -ErrorAction Continue "Target database '$TargetDatabase' already exists. Re-run with -ConfirmDestructiveOverwrite."
    exit 5
}

Write-Log "INFO" "Step 4: Execute RESTORE..."
$restoreSetMulti = $false
try {
    if ($targetExists -ne "0" -and $ConfirmDestructiveOverwrite) {
        Write-Log "INFO" "Target exists + overwrite confirmed: ALTER SINGLE_USER -> RESTORE REPLACE -> ALTER MULTI_USER"
        $restoreQuery = @"
ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$TargetDatabase] FROM DISK=N'$($BackupFile.Replace("'","''"))' WITH REPLACE, RECOVERY, STATS=10$moveClauseString;
ALTER DATABASE [$TargetDatabase] SET MULTI_USER;
"@
        $restoreSetMulti = $true
    } else {
        Write-Log "INFO" "Target is new database: RESTORE with RECOVERY + MOVE."
        $restoreQuery = @"
RESTORE DATABASE [$TargetDatabase] FROM DISK=N'$($BackupFile.Replace("'","''"))' WITH RECOVERY, STATS=10$moveClauseString;
"@
    }
    Write-Log "INFO" "Restore SQL (partial shown): RESTORE DATABASE [$TargetDatabase] ... WITH ...$moveClauseString"
    & sqlcmd -S $ServerInstance -E -b -Q $restoreQuery
    $restoreExit = $LASTEXITCODE
    if ($restoreExit -ne 0) {
        Write-Log "ERROR" "restore-failed-sql (exit=$restoreExit): RESTORE DATABASE command failed."
        Write-Error -ErrorAction Continue "restore-failed-sql: RESTORE failed with exit $restoreExit."
        exit 16
    }
    $restoreSetMulti = $false
    Write-Log "INFO" "RESTORE DATABASE sqlcmd returned 0."
} finally {
    if ($restoreSetMulti) {
        Write-Log "WARN" "Post-restore finally: attempting to restore MULTI_USER on [$TargetDatabase]."
        try {
            & sqlcmd -S $ServerInstance -E -b -Q "ALTER DATABASE [$TargetDatabase] SET MULTI_USER;" | Out-Null
        } catch {
            Write-Log "WARN" "MULTI_USER attempt failed (non-fatal here)."
        }
    }
}

Write-Log "INFO" "Step 5: Verify target database exists."
$verifyQuery = "SET NOCOUNT ON; SELECT 1 WHERE DB_ID(N'$TargetDatabase') IS NOT NULL;"
$verifyRaw = & sqlcmd -S $ServerInstance -E -b -W -h -1 -Q $verifyQuery 2>&1
$vExit = $LASTEXITCODE
$gotOne = $false
foreach ($l in $verifyRaw) {
    if ($l -match "^\s*1\s*$") { $gotOne = $true; break }
}
if ($vExit -ne 0 -or -not $gotOne) {
    Write-Log "ERROR" "restore-verify-failed (exit=$vExit): DB_ID(N'$TargetDatabase') IS NOT NULL did not return 1."
    Write-Error -ErrorAction Continue "restore-verify-failed: Target database not verifiable after restore."
    exit 17
}
Write-Log "INFO" "Target database verification passed."

Write-Log "INFO" "RESTORE OK: TargetDatabase=$TargetDatabase from BackupFile=$BackupFile"
Write-Output "RESTORE OK: $TargetDatabase restored from $BackupFile"
exit 0
