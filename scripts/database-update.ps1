param([switch]$NoBuild = $false)
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "=== LivestockManager: Apply EF Migrations ==="
$InfraProj = Join-Path $RepoRoot "src\LivestockManager.Infrastructure\LivestockManager.Infrastructure.csproj"
$StartupProj = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
if (-not (Test-Path $InfraProj)) { throw "Infrastructure project not found at $InfraProj" }
if (-not (Test-Path $StartupProj)) { throw "Web/startup project not found at $StartupProj" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
$Args = @("ef","database","update","--project",$InfraProj,"--startup-project",$StartupProj)
if ($NoBuild) { $Args += "--no-build" }
& dotnet @Args
exit $LASTEXITCODE
