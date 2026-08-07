$ErrorActionPreference = 'Stop'
$ScriptName = 'run-local'
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
        Write-Log "ERROR: Web project not found at $WebProject"
        Write-Error "Expected project at $WebProject"
        $ExitCode = 1
        return
    }

    $env:ASPNETCORE_URLS = 'http://localhost:5100'
    Write-Log 'Starting web project on http://localhost:5100...'
    Write-Log 'Press Ctrl+C to stop'

    & dotnet run --project $WebProject --launch-profile http --no-launch-profile
    $runExit = $LASTEXITCODE
    if ($runExit -ne 0) { throw "dotnet run exited with code $runExit" }

    Write-Log 'Run completed successfully'
}
catch {
    Write-Log "ERROR: $_"
    $ExitCode = 2
}
finally {
    Write-Log "=== $ScriptName.ps1 finished with exit code $ExitCode ==="
    exit $ExitCode
}
