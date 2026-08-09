<#
.SYNOPSIS
Checks machine prerequisites for building, testing, and deploying the Livestock
Manager ASP.NET Core application. Returns exit 0 when all mandatory prerequisites
are present; nonzero otherwise.

.DESCRIPTION
Mandatory checks (FAIL = nonzero exit):
  1. .NET 8 SDK: `dotnet --version` >= 8.x (must be SDK, not runtime-only)
  2. sqlcmd.exe: present on PATH (required for backup / restore / E2E / manual
     SQL ops)
  3. Free disk space on drive containing repo: > 2 GB available
  4. Free disk space on SQL Server default data/log drive: > 5 GB available
     (if SQL Server default data path detectable; otherwise this is informational)

Informational checks (WARN only, never cause nonzero exit):
  - IIS present? If so, verify ASP.NET Core Module / Hosting Bundle installation
    status and print OK / informational notice. Never a hard fail because
    deployment may be via Kestrel-only / reverse proxy other than IIS.

.EXAMPLE
.\scripts\Check-Prerequisites.ps1
Standard developer workstation prerequisites check.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$SqlDataDriveOverride = ""
)

$ErrorActionPreference = "Stop"
$ExitCode = 1
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$MandatoryPassed = 0
$MandatoryFailed = 0
$InfoWarnings = 0

function Write-Info([string]$msg) { Write-Host "[PREREQ] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[PREREQ][OK] $msg" -ForegroundColor Green; $script:MandatoryPassed++ }
function Write-Fail([string]$msg) { Write-Host "[PREREQ][FAIL] $msg" -ForegroundColor Red; $script:MandatoryFailed++ }
function Write-Warn([string]$msg) { Write-Host "[PREREQ][WARN] $msg" -ForegroundColor Yellow; $script:InfoWarnings++ }

function Get-FreeBytes([string]$path) {
    try {
        $driveName = if ($path -match '^([A-Za-z]):') { $Matches[1] + ":" } else { (Get-Location).Drive.Root }
        $disk = Get-CimInstance Win32_LogicalDisk -Filter ("DeviceID='{0}'" -f $driveName) -ErrorAction Stop
        if ($null -eq $disk) { return -1 }
        return [int64]$disk.FreeSpace
    } catch {
        return -1
    }
}

function Test-DotNetSdkVersion([string]$raw) {
    if ([string]::IsNullOrWhiteSpace($raw)) { return $false }
    $clean = $raw.Trim()
    if ($clean -notmatch '^(\d+)\.(\d+)\.(\d+)') { return $false }
    $major = [int]$Matches[1]
    return ($major -ge 8)
}

function Get-SqlDefaultDataDrive {
    if (-not [string]::IsNullOrWhiteSpace($SqlDataDriveOverride)) { return $SqlDataDriveOverride }
    try {
        if (Get-Command sqlcmd.exe -ErrorAction SilentlyContinue) {
            $q = @"
SET NOCOUNT ON;
DECLARE @dp NVARCHAR(512), @lp NVARCHAR(512);
EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'DefaultData', @dp OUTPUT, 'no_output';
EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'DefaultLog', @lp OUTPUT, 'no_output';
SELECT ISNULL(@dp, N'') AS DataPath;
SELECT ISNULL(@lp, N'') AS LogPath;
"@
            $lines = & sqlcmd.exe -S "." -E -b -W -h -1 -Q $q 2>$null
            foreach ($l in $lines) {
                if (-not [string]::IsNullOrWhiteSpace($l) -and $l -match '^([A-Za-z]):\\') {
                    return $Matches[1] + ":"
                }
            }
        }
    } catch {}
    return ""
}

try {
    Write-Info "Check-Prerequisites starting at $(Get-Date -Format s)"
    Write-Info "Repository root: $RepoRoot"

    # === Check 1: dotnet 8 SDK ===
    $dotnetVersionRaw = ""
    try {
        $dotnetVersionRaw = (& dotnet --version 2>&1) | Out-String
        $dotnetExit = $LASTEXITCODE
    } catch { $dotnetExit = 99 }
    if ($dotnetExit -ne 0 -or [string]::IsNullOrWhiteSpace($dotnetVersionRaw)) {
        Write-Fail "dotnet CLI not found or failed. Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0"
    } else {
        $dv = $dotnetVersionRaw.Trim()
        Write-Info ("dotnet --version output: {0}" -f $dv)
        if (Test-DotNetSdkVersion $dv) {
            Write-OK (".NET SDK version {0} detected (>= 8.x requirement satisfied)." -f $dv)
        } else {
            Write-Fail (".NET SDK version '{0}' detected, but .NET SDK >= 8.x is required. Install the .NET 8 SDK." -f $dv)
        }
    }

    # === Check 2: sqlcmd command present ===
    $sqlCmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
    if ($null -ne $sqlCmd) {
        Write-OK ("sqlcmd.exe present on PATH (source: {0})." -f $sqlCmd.Source)
    } else {
        Write-Fail "sqlcmd.exe not found on PATH. Install SQL Server Command Line Utilities (SqlPackage/sqlcmd via `winget install Microsoft.SQLCMD` or from Microsoft Download Center)."
    }

    # === Check 3: Free disk on repo drive (> 2GB) ===
    $repoFreeBytes = Get-FreeBytes $RepoRoot
    $repoFreeGB = if ($repoFreeBytes -lt 0) { -1 } else { [math]::Round($repoFreeBytes / 1GB, 2) }
    if ($repoFreeBytes -lt 0) {
        Write-Warn ("Could not read free-space on drive containing repo ('{0}'). Verify manually." -f $RepoRoot)
    } elseif ($repoFreeGB -gt 2) {
        Write-OK ("Repo drive has {0:N2} GB free (> 2 GB required)." -f $repoFreeGB)
    } else {
        Write-Fail ("Repo drive has only {0:N2} GB free (< 2 GB required). Free space before builds/tests." -f $repoFreeGB)
    }

    # === Check 4: SQL data drive free (> 5GB) ===
    $sqlDrive = Get-SqlDefaultDataDrive
    if ([string]::IsNullOrWhiteSpace($sqlDrive)) {
        Write-Warn "Could not detect SQL Server default data drive (no SQL locally or registry key unreadable). Provide -SqlDataDriveOverride 'D:' for this check, or manually verify >= 5 GB free on SQL data/log drive."
    } else {
        $sqlFreeBytes = Get-FreeBytes $sqlDrive
        $sqlFreeGB = if ($sqlFreeBytes -lt 0) { -1 } else { [math]::Round($sqlFreeBytes / 1GB, 2) }
        if ($sqlFreeBytes -lt 0) {
            Write-Warn ("Could not read free-space on SQL data drive '{0}'. Verify manually." -f $sqlDrive)
        } elseif ($sqlFreeGB -gt 5) {
            Write-OK ("SQL data drive '{0}' has {1:N2} GB free (> 5 GB required)." -f $sqlDrive, $sqlFreeGB)
        } else {
            Write-Fail ("SQL data drive '{0}' has only {1:N2} GB free (< 5 GB required). Free space on SQL volume before Production DB attach/restore." -f $sqlDrive, $sqlFreeGB)
        }
    }

    # === Informational: IIS present + Hosting Bundle / ANCM ===
    $iisInstalled = $false
    try {
        $iisKey = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\InetStp" -ErrorAction SilentlyContinue
        if ($null -ne $iisKey -and (-not [string]::IsNullOrWhiteSpace($iisKey.SetupString))) {
            $iisInstalled = $true
        }
    } catch {}
    if (-not $iisInstalled) {
        Write-Warn "IIS not detected on this machine. If target is IIS deploy, run on Windows Server with IIS role enabled. (Kestrel-only or non-IIS reverse-proxy deploys ignore this.)"
    } else {
        $ancmPresent = $false
        $modules = $null
        try {
            Import-Module WebAdministration -ErrorAction SilentlyContinue
            $modules = Get-WebGlobalModule -ErrorAction SilentlyContinue
        } catch {}
        if ($null -ne $modules) {
            foreach ($m in $modules) {
                if ($m.Name -like "*AspNetCoreModule*") { $ancmPresent = $true; break }
            }
        }
        if ($ancmPresent) {
            Write-Warn "IIS detected with ASP.NET Core Module (Hosting Bundle) installed. NOTICE: IIS deploy target prerequisites OK."
        } else {
            Write-Warn "IIS detected but ASP.NET Core Module NOT found (no Hosting Bundle). If deploying to IIS, install the ASP.NET Core 8.0 Hosting Bundle: https://dotnet.microsoft.com/download/dotnet/8.0"
        }
    }

    # === Final summary ===
    Write-Info "====================================================="
    Write-Info "Check-Prerequisites — FINAL SUMMARY"
    Write-Info "====================================================="
    Write-Info ("Mandatory checks PASSED : {0}" -f $MandatoryPassed)
    Write-Info ("Mandatory checks FAILED : {0}" -f $MandatoryFailed)
    Write-Info ("Informational warnings : {0}" -f $InfoWarnings)

    if ($MandatoryFailed -eq 0) {
        Write-OK "All MANDATORY prerequisites satisfied. Informational warnings above describe optional / deploy-target-only concerns."
        $ExitCode = 0
    } else {
        Write-Fail ("{0} MANDATORY prerequisite checks FAILED. Resolve before attempting build, test, or deploy." -f $MandatoryFailed)
        $ExitCode = 2
    }
}
catch {
    Write-Fail ("Fatal exception in Check-Prerequisites: {0}" -f $_.Exception.Message)
    $ExitCode = 99
}

Write-Info "Check-Prerequisites finished at $(Get-Date -Format s) — exiting $ExitCode"
$host.SetShouldExit($ExitCode)
exit $ExitCode
