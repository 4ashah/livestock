param([int]$Port = 5199, [string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Write-Host "=== LivestockManager: Smoke Test ==="
& (Join-Path $PSScriptRoot "test.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$WebProj = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:SeedDemoData = "0"
$env:EnableDevSeed = "false"
$LogFile = Join-Path $env:TEMP "livestock-smoke.log"
$StartArgs = @("run","--project",$WebProj,"-c",$Configuration,"--no-build","--no-restore")
$Proc = Start-Process -FilePath "dotnet" -ArgumentList $StartArgs -RedirectStandardOutput $LogFile -RedirectStandardError $LogFile -PassThru -WindowStyle Hidden
$Timeout = (Get-Date).AddSeconds(60)
$Listening = $false
while ((Get-Date) -lt $Timeout -and -not $Proc.HasExited) {
    Start-Sleep -Milliseconds 800
    $row = netstat -ano | Select-String ":$Port\s" | Select-String "LISTENING" | Select-Object -First 1
    if ($row) { $Listening = $true; break }
}
$HTTPCODE = 0
if ($Listening) {
    try {
        $resp = Invoke-WebRequest -UseBasicParsing -TimeoutSec 10 -Uri "http://localhost:$Port/health"
        $HTTPCODE = [int]$resp.StatusCode
        Write-Host "Health endpoint HTTP=$HTTPCODE"
    } catch {
        if ($_.Exception.Response) { $HTTPCODE = [int]$_.Exception.Response.StatusCode }
        Write-Host "Health endpoint exception, HTTP=$HTTPCODE"
    }
}
if (-not $Proc.HasExited) { Stop-Process -Id $Proc.Id -Force -ErrorAction SilentlyContinue }
Start-Sleep -Milliseconds 600
if ($HTTPCODE -eq 200) { Write-Host "SMOKE OK: /health returned 200"; exit 0 }
Write-Host "SMOKE FAIL: /health did not return 200 (got $HTTPCODE)"; exit 1
