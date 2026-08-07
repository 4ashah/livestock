param([string]$Configuration = "Release", [string]$Runtime = "win-x64")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "=== LivestockManager: Publish for IIS ==="
Write-Host "Notes:"
Write-Host "  * Install ASP.NET Core Hosting Bundle 8.x on the target server"
Write-Host "  * AppPool .NET CLR version = No Managed Code"
Write-Host "  * AppPool identity (IIS AppPool\LivestockAppPool) needs Modify ACL on App_Data"
& (Join-Path $PSScriptRoot "package-release.ps1") -Configuration $Configuration -Runtime $Runtime
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$OutDir = Join-Path $RepoRoot "artifacts\publish"
$AppData = Join-Path $OutDir "App_Data"
$FilesDir = Join-Path $AppData "files"
if (-not (Test-Path $AppData)) { New-Item -ItemType Directory -Path $AppData -Force | Out-Null }
if (-not (Test-Path $FilesDir)) { New-Item -ItemType Directory -Path $FilesDir -Force | Out-Null }
Write-Host ""
Write-Host "IIS publish folder ready: $OutDir"
Write-Host "Copy contents of this folder to C:\inetpub\livestock\ on the target IIS server."
