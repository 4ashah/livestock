$ErrorActionPreference = 'Stop'
$ScriptName = 'seed-demo-data'
$ExitCode = 0
$Timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$ScriptDir = Split-Path -Parent $PSScriptRoot
$ArtifactsDir = Join-Path $ScriptDir 'artifacts'
$LogDir = Join-Path $ArtifactsDir 'logs'
$LogFile = Join-Path $LogDir "$ScriptName-$Timestamp.log"

if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

function Write-Log {
    param([string]$Message)
    $logLine = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss)] $Message"
    Add-Content -Path $LogFile -Value $logLine
    Write-Host $logLine
}

Write-Log "=== $ScriptName.ps1 started ==="
Write-Log "Timestamp: $Timestamp"
Write-Log "Working directory: $PWD"

try {
    $dotnetCmd = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    if (-not $dotnetCmd) {
        Write-Log 'ERROR: dotnet CLI not found in PATH'
        Write-Error 'dotnet CLI is required but not installed or not in PATH'
        $ExitCode = 1
        return
    }
    $dotnetVer = & dotnet --version
    Write-Log "dotnet version: $dotnetVer"

    $WebProject = Join-Path $ScriptDir 'src\LivestockManager.Web\LivestockManager.Web.csproj'
    if (-not (Test-Path $WebProject)) {
        throw "Web project not found at $WebProject"
    }

    $env:SEED_DEMO_DATA = '1'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Write-Log 'SEED_DEMO_DATA=1'
    Write-Log 'Starting web project to seed demo data...'
    Write-Log 'Web project will seed data on startup and listen for shutdown...'

    & dotnet run --project $WebProject
    $runExit = $LASTEXITCODE
    if ($runExit -ne 0) { Write-Log "WARNING: dotnet run exited with code $runExit" }

    Write-Log 'Demo data seed process completed'
    Write-Host "Demo data seed process completed. Log: $LogFile"
}
catch {
    Write-Log "ERROR: $_"
    if ($ExitCode -eq 0) { $ExitCode = 2 }
}
finally {
    Write-Log "=== $ScriptName.ps1 finished with exit code $ExitCode ==="
    exit $ExitCode
}
