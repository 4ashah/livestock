# install-windows-service.ps1
# Registers the published LivestockManager.Web app as a Windows service named
# LivestockManagerWeb, sets its environment variables, and starts it.
#
# Prerequisites:
#   1. The app must be published first (dotnet publish ... -o artifacts/service-publish)
#   2. The service account (default LocalSystem) must have access to the SQL database
#      (see artifacts/grant-service-db.sql)
#   3. This script must run elevated (Administrator)
param(
    [string]$ServiceName = 'LivestockManagerWeb',
    [string]$Port = '5100'
)
$ErrorActionPreference = 'Stop'

$DisplayName = 'Livestock Manager Web'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BinPath = Join-Path $RepoRoot "artifacts\service-publish\LivestockManager.Web.exe"

if (-not (Test-Path $BinPath)) {
    Write-Error "Published app not found at $BinPath. Publish first: dotnet publish src\LivestockManager.Web\LivestockManager.Web.csproj -c Release -o artifacts\service-publish"
    exit 1
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
# NOTE: sc.exe requires "key=" and its value to be separate command-line arguments
# (a space is mandatory between the equal sign and the value); a single argument
# containing both is rejected with error 1639.
if ($existing) {
    Write-Host "Service '$ServiceName' already exists (status: $($existing.Status)); reconfiguring binary path."
    & sc.exe config $ServiceName 'binPath=' $BinPath 'start=' 'auto' 'DisplayName=' $DisplayName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc config failed with exit code $LASTEXITCODE" }
} else {
    & sc.exe create $ServiceName 'binPath=' $BinPath 'start=' 'auto' 'DisplayName=' $DisplayName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sc create failed with exit code $LASTEXITCODE" }
}

# SCM has no native UI for per-service environment variables; they are stored as a
# REG_MULTI_SZ "Environment" value under the service's registry key.
$envValues = @(
    'ASPNETCORE_ENVIRONMENT=Production',
    'SeedDemoData=false',
    'EnableDevSeed=false',
    "ASPNETCORE_URLS=http://localhost:$Port"
)
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName" -Name Environment -PropertyType MultiString -Value $envValues -Force | Out-Null
Write-Host "Set service environment variables: $($envValues -join ', ')"

Write-Host "Starting service '$ServiceName'..."
& sc.exe start $ServiceName | Out-Null
if ($LASTEXITCODE -ne 0) { throw "sc start failed with exit code $LASTEXITCODE" }

Start-Sleep -Seconds 3
$svc = Get-Service -Name $ServiceName
Write-Host "Service status: $($svc.Status)"
Write-Host "Health check: http://localhost:$Port/health/live"
