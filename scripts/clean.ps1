param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Sln = Join-Path $RepoRoot "LivestockManager.sln"
Write-Host "=== LivestockManager: Clean ==="
if (-not (Test-Path $Sln)) { throw "Solution not found at $Sln" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
& dotnet clean $Sln -c $Configuration --nologo -v minimal
$Artifacts = Join-Path $RepoRoot "artifacts"
if (Test-Path $Artifacts) { Remove-Item -Recurse -Force $Artifacts -ErrorAction SilentlyContinue }
