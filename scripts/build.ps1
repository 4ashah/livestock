param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Sln = Join-Path $RepoRoot "LivestockManager.sln"
Write-Host "=== LivestockManager: Build ($Configuration) ==="
if (-not (Test-Path $Sln)) { throw "Solution not found at $Sln" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
& dotnet build $Sln -c $Configuration --nologo -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
