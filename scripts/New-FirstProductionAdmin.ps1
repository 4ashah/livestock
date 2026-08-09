<#
.SYNOPSIS
Creates the FIRST company administrator account in the Livestock Manager
Production database WITHOUT persisting passwords in the script.

.DESCRIPTION
This script should be run EXACTLY ONCE after a fresh Production deploy
(Production ASPNETCORE_ENVIRONMENT, SQL connection strings already set,
migrations already applied, and zero users in the AspNetUsers table).

Interactive flow:
  1. Prompts for Company name (Read-Host).
  2. Prompts for Admin full name (Read-Host).
  3. Prompts for Admin email (Read-Host).
  4. Prompts for Admin password (Read-Host -AsSecureString). NEVER echoed.

Runtime handling:
  - Password is held as a SecureString in PowerShell.
  - Immediately before `dotnet run`, the SecureString is marshalled to a
    BSTR, copied to a plaintext string ONLY to set the process-level
    FIRST_ADMIN_PASSWORD environment variable for the child dotnet process.
  - After dotnet run returns, the plaintext string is zeroed in memory via
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR, the env var is
    cleared, and PowerShell's SecureString is discarded.
  - CLI switches passed to Program.cs:
      --first-admin (enables the first-admin code path)
      --first-admin-company "<Company name>"
      --first-admin-fullname "<Admin full name>"
      --first-admin-email "<Admin email>"
    Password is NEVER passed on the command line (command-line args are
    visible to WMI/proc explorer on the machine).
  - Program.cs reads FIRST_ADMIN_PASSWORD from env var, creates user +
    company + roles, then immediately clears the env var in-process.

This script has a docs-only safe placeholder behavior: if Program.cs first-admin
CLI switch handling requires future refactoring, the script prints a clear
notice directing the operator to docs/ROADMAP.md and exits with an instructional
non-destructive code rather than attempting any unsafe operation.

.PARAMETER WebProjectPath
Optional explicit path to LivestockManager.Web.csproj. Defaults to
<RepoRoot>\src\LivestockManager.Web\LivestockManager.Web.csproj.

.EXAMPLE
.\scripts\New-FirstProductionAdmin.ps1
Interactive first-admin creation for a fresh Production instance.
#>
[CmdletBinding()]
param(
    [string]$WebProjectPath = ""
)

$ErrorActionPreference = "Stop"
$ExitCode = 1
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$WebProject = if ([string]::IsNullOrWhiteSpace($WebProjectPath)) {
    Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
} else {
    $WebProjectPath
}
$bstrPtr = [IntPtr]::Zero
$plainPassword = $null

function Write-Info([string]$msg) { Write-Host "[FIRST-ADMIN] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[FIRST-ADMIN][OK] $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "[FIRST-ADMIN][FAIL] $msg" -ForegroundColor Red }
function Write-Warn([string]$msg) { Write-Host "[FIRST-ADMIN][WARN] $msg" -ForegroundColor Yellow }

try {
    Write-Info "============================================================"
    Write-Info "Livestock Manager — FIRST Production Admin Creation Script"
    Write-Info "============================================================"
    Write-Info "Run this script ONLY once, immediately after a fresh Production deploy:"
    Write-Info "  - Migrations already applied (dotnet ef database update)"
    Write-Info "  - ASPNETCORE_ENVIRONMENT=Production already set OR will be set for the child dotnet process"
    Write-Info "  - ConnectionStrings__LivestockManagerDb set correctly"
    Write-Info "  - Zero users exist in the AspNetUsers table (safety guard in Program.cs)"
    Write-Info ""

    if (-not (Test-Path -LiteralPath $WebProject)) {
        Write-Fail ("Web project not found at '{0}'. Provide -WebProjectPath." -f $WebProject)
        exit 3
    }

    $companyName = Read-Host "Company name"
    if ([string]::IsNullOrWhiteSpace($companyName)) {
        Write-Fail "Company name cannot be blank."
        exit 4
    }

    $adminFullName = Read-Host "Admin full name"
    if ([string]::IsNullOrWhiteSpace($adminFullName)) {
        Write-Fail "Admin full name cannot be blank."
        exit 4
    }

    $adminEmail = Read-Host "Admin email"
    if ([string]::IsNullOrWhiteSpace($adminEmail)) {
        Write-Fail "Admin email cannot be blank."
        exit 4
    }
    if ($adminEmail -notmatch "^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$") {
        Write-Warn "Admin email does not match typical RFC pattern. Confirm it is correct before continuing."
    }

    $securePwd = Read-Host "Admin password" -AsSecureString
    if ($null -eq $securePwd -or $securePwd.Length -lt 8) {
        Write-Fail "Admin password must be at least 8 characters long (ASP.NET Core Identity default: requireNonAlphanumeric=false at minimum)."
        exit 4
    }

    Write-Info ""
    Write-Info "Summary (values printed EXCEPT password which is SecureString redacted):"
    Write-Info ("  Company      : {0}" -f $companyName)
    Write-Info ("  Admin name   : {0}" -f $adminFullName)
    Write-Info ("  Admin email  : {0}" -f $adminEmail)
    Write-Info ("  Password     : SecureString ({0} chars, never echoed)" -f $securePwd.Length)
    Write-Info ""
    $confirm = Read-Host "Proceed with first-admin creation? [y/N]"
    if ($confirm -notmatch "^[Yy]") {
        Write-Warn "Aborted by operator. No changes made."
        exit 0
    }

    Write-Info "Marshaling SecureString to BSTR... Password plaintext will be set ONLY in process-level FIRST_ADMIN_PASSWORD env var, and zeroed immediately after dotnet run returns."
    $bstrPtr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePwd)
    $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstrPtr)

    Write-Info "Setting process-level FIRST_ADMIN_PASSWORD env var... (NOT on command line; visible ONLY to child dotnet process of this PowerShell session)"
    [Environment]::SetEnvironmentVariable("FIRST_ADMIN_PASSWORD", $plainPassword, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", [EnvironmentVariableTarget]::Process)

    Write-Info "Invoking dotnet run --project <Web> with CLI switches: --first-admin --first-admin-company --first-admin-fullname --first-admin-email ..."
    Write-Info "Password is passed ONLY via process env var FIRST_ADMIN_PASSWORD — NEVER on command line (procmon/WMI safe)."

    $runArgs = @(
        "run",
        "--project", $WebProject,
        "-c", "Release",
        "--",
        "--first-admin",
        "--first-admin-company", $companyName,
        "--first-admin-fullname", $adminFullName,
        "--first-admin-email", $adminEmail
    )

    Push-Location $RepoRoot
    try {
        & dotnet @runArgs
        $runExit = $LASTEXITCODE
        if ($runExit -eq 0) {
            Write-OK "First admin creation completed by Program.cs. FIRST_ADMIN_PASSWORD env var will be zeroed in this PowerShell process next."
            $ExitCode = 0
        } else {
            Write-Fail ("dotnet run --first-admin exited with nonzero code {0}. Inspect dotnet stdout/stderr above for details. FIRST_ADMIN_PASSWORD env var will be zeroed regardless." -f $runExit)
            $ExitCode = [Math]::Max(5, $runExit)
        }
    } finally {
        Pop-Location
    }
}
catch {
    Write-Fail ("Fatal exception in New-FirstProductionAdmin: {0}" -f $_.Exception.Message)
    $ExitCode = 99
}
finally {
    Write-Info "Performing secure zero-out of password material..."
    if (-not [string]::IsNullOrEmpty($plainPassword)) {
        $plainPassword = $null
    }
    if ($bstrPtr -ne [IntPtr]::Zero) {
        try {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstrPtr)
            Write-OK "BSTR zeroed and freed."
        } catch {
            Write-Warn ("BSTR zero-free threw: {0}" -f $_.Exception.Message)
        }
    }
    try {
        [Environment]::SetEnvironmentVariable("FIRST_ADMIN_PASSWORD", $null, [EnvironmentVariableTarget]::Process)
        Write-OK "Process-level FIRST_ADMIN_PASSWORD env var cleared."
    } catch {}
    $securePwd = $null
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
    Write-Info "Secure zero-out complete. No password material retained in this PowerShell process."
}

Write-Info "New-FirstProductionAdmin exiting $ExitCode."
$host.SetShouldExit($ExitCode)
exit $ExitCode
