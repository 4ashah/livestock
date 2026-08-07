param([string]$Configuration = "Release", [string]$Runtime = "win-x64")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "=== LivestockManager: Package Release ==="
$WebProj = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
$OutDir = Join-Path $RepoRoot "artifacts\publish"
if (-not (Test-Path $WebProj)) { throw "Web project not found at $WebProj" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found on PATH" }
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir -ErrorAction SilentlyContinue }
& dotnet publish $WebProj -c $Configuration -r $Runtime --self-contained false -o $OutDir --nologo -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$BackupDir = Join-Path $RepoRoot "artifacts\backups"
if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null }
Write-Host "Publish complete: $OutDir"
