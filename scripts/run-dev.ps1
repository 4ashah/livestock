param([int]$Port = 5100, [string]$Configuration = "Debug")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "=== LivestockManager: Run Dev (port $Port) ==="
$WebProj = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
if (-not (Test-Path $WebProj)) { throw "Web project not found at $WebProj" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:EnableDevSeed = "true"
& dotnet run --project $WebProj -c $Configuration
exit $LASTEXITCODE
