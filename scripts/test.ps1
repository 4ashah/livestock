param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Sln = Join-Path $RepoRoot "LivestockManager.sln"
Write-Host "=== LivestockManager: Test ($Configuration) ==="
if (-not (Test-Path $Sln)) { throw "Solution not found at $Sln" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
& dotnet test $Sln -c $Configuration --nologo -v minimal --filter "FullyQualifiedName~UnitTests"
exit $LASTEXITCODE
