<#
.SYNOPSIS
Run Playwright-based E2E tests for the AHK Livestock Manager ASP.NET Core web application
against a dedicated disposable SQL Server database.

.DESCRIPTION
Automates the full E2E pipeline:
  1. Detects repository root and EndToEnd test project TargetFramework.
  2. Restores + Release builds the solution (E2E project only).
  3. Locates the playwright.ps1 install helper shipped with Microsoft.Playwright.
  4. Installs Chromium ONLY if the Playwright browser is missing.
  5. Creates a dedicated disposable SQL Server database (default name:
     LivestockManager_E2E with optional suffix). Never uses Development or
     Production databases.
  6. Applies EF Core migrations and seeds deterministic E2E data using
     DemoDataSeeder with EnableE2ESeed=1 in the Testing environment.
  7. Starts LivestockManager.Web on an available localhost port.
  8. Waits for /health/live with bounded timeout.
  9. Sets E2E_BASE_URL so Playwright tests hit the correct application URL.
 10. Runs all E2E tests in Release mode, generating TRX + screenshots +
     traces + videos in artifacts/e2e.
 11. Stops the application in a finally block.
 12. Drops the E2E database unless -KeepDatabase is supplied.
 13. Returns exit 0 only when all E2E tests run and pass. Nonzero when tests
     fail, are unexpectedly skipped, startup fails, migrations fail, or
     browser install fails.
 14. Prints a final summary with discovered/passed/failed/skipped/duration.
 15. Never prints passwords or connection string secrets.

.PARAMETER ServerInstance
SQL Server instance. Defaults to "." (default MSSQLSERVER on localhost).

.PARAMETER DatabaseName
Base database name. Default: "LivestockManager_E2E". A unique suffix is
appended unless -KeepDatabase is set.

.PARAMETER AppPort
Optional fixed localhost port for the web application. If not supplied, an
available OS-assigned port is picked automatically.

.PARAMETER KeepDatabase
If set, the disposable E2E database is NOT dropped after tests complete.
Also disables the unique suffix (allows debugging the exact E2E state).

.PARAMETER Headed
If set, Playwright tests run with a visible Chromium window (headed mode).
Default is headless (suitable for automated/CI runs).

.PARAMETER SkipBrowserInstall
If set, the Playwright Chromium install step is skipped. Use only when you
have already installed browsers and want a faster run.

.PARAMETER TestFilter
Optional dotnet test --filter expression. Example:
  "FullyQualifiedName~Workflow_01_Login"
Default runs all E2E tests.

.EXAMPLE
.\scripts\Run-E2ETests.ps1 -ServerInstance "." -Headed
Local developer run with visible browser and disposable database.

.EXAMPLE
.\scripts\Run-E2ETests.ps1 -ServerInstance "."
Default CI/automated headless run.
#>
[CmdletBinding()]
param(
    [string]$ServerInstance = ".",
    [string]$DatabaseName = "LivestockManager_E2E",
    [int]$AppPort = 0,
    [switch]$KeepDatabase,
    [switch]$Headed,
    [switch]$SkipBrowserInstall,
    [string]$TestFilter
)

$ErrorActionPreference = "Continue"
$PSNativeCommandUseErrorActionPreference = $false
$ExitCode = 1
$WebProcess = $null
$RunStart = Get-Date
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$E2EArtifacts = Join-Path $ArtifactsRoot "e2e"
$E2EResults = Join-Path $E2EArtifacts "results"
$E2EScreenshots = Join-Path $E2EArtifacts "screenshots"
$E2ETraces = Join-Path $E2EArtifacts "traces"
$E2EVideos = Join-Path $E2EArtifacts "videos"
$E2ELogs = Join-Path $E2EArtifacts "logs"
$E2ETestProject = Join-Path $RepoRoot "tests\LivestockManager.EndToEndTests\LivestockManager.EndToEndTests.csproj"
$WebProject = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
$InfraProject = Join-Path $RepoRoot "src\LivestockManager.Infrastructure\LivestockManager.Infrastructure.csproj"
$TrxName = "e2e-$(Get-Date -Format 'yyyyMMdd_HHmmss').trx"
$TrxPath = Join-Path $E2EResults $TrxName
$UniqueSuffix = if ($KeepDatabase) { "" } else { "_$(Get-Date -Format 'yyyyMMddHHmmssffff')" }
$FinalDatabaseName = "${DatabaseName}${UniqueSuffix}"
$AppUrl = $null
$Discovered = 0
$Passed = 0
$Failed = 0
$Skipped = 0
$Duration = ""

function Write-Info([string]$msg) { Write-Host "[E2E] $msg" -ForegroundColor Cyan }
function Write-Warn([string]$msg) { Write-Host "[E2E][WARN] $msg" -ForegroundColor Yellow }
function Write-Fail([string]$msg) { Write-Host "[E2E][FAIL] $msg" -ForegroundColor Red }
function Test-SqlCmd { $null -ne (Get-Command sqlcmd.exe -ErrorAction SilentlyContinue) }
function Test-DotNet { $null -ne (Get-Command dotnet -ErrorAction SilentlyContinue) }
function Test-Identifier([string]$name) { return $name -match '^[A-Za-z0-9_@#$-]+$' }
function New-Directory([string]$path) {
    if (!(Test-Path $path)) { [void](New-Item -ItemType Directory -Force -Path $path -ErrorAction Stop) }
}
function Get-TargetFramework([string]$csprojPath) {
    [xml]$x = Get-Content -Raw -Path $csprojPath
    $fx = $x.Project.PropertyGroup.TargetFramework | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($fx)) { throw "Could not read TargetFramework from $csprojPath" }
    return $fx.Trim()
}
function Test-DatabaseExists([string]$server, [string]$db) {
    $checkQ = "SET NOCOUNT ON; SELECT ISNULL(DB_ID(N'$db'), 0) AS present;"
    $raw = & sqlcmd.exe -S $server -E -Q $checkQ -h -1 -W 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($raw)) { return $false }
    $v = 0
    return [int]::TryParse($raw.Trim(), [ref]$v) -and $v -gt 0
}
function Invoke-Sql([string]$server, [string]$query) {
    & sqlcmd.exe -S $server -E -Q $query -b | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit $LASTEXITCODE" }
}
function Invoke-ExternalNative([scriptblock]$sb, [string]$errMsg) {
    $prev = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & $sb
        if ($LASTEXITCODE -ne 0) { throw "$errMsg (exit $LASTEXITCODE)." }
    } finally {
        $ErrorActionPreference = $prev
    }
}
function Get-AvailablePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    } finally {
        $listener.Stop()
    }
}
function Wait-Health([string]$url, [int]$timeoutSec = 90) {
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    try {
        $client.Timeout = [TimeSpan]::FromSeconds(4)
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        while ($watch.Elapsed.TotalSeconds -lt $timeoutSec) {
            try {
                $resp = $client.GetAsync("${url}/health/live").GetAwaiter().GetResult()
                if ($resp.IsSuccessStatusCode) { return $true }
            } catch {}
            Start-Sleep -Milliseconds 500
        }
        return $false
    } finally {
        $client.Dispose()
    }
}
function Parse-TrxSummary([string]$trxFile) {
    $script:Discovered = 0
    $script:Passed = 0
    $script:Failed = 0
    $script:Skipped = 0
    $script:Duration = ""
    if (!(Test-Path $trxFile)) { return }
    try {
        [xml]$trx = Get-Content -Raw -Path $trxFile
        $unit = $trx.TestRun.ResultSummary.Counters
        if ($null -ne $unit) {
            $script:Discovered = [int]$unit.total
            $script:Passed = [int]$unit.passed
            $script:Failed = [int]$unit.failed
            $script:Skipped = [int]$unit.skipped
        }
        $times = $trx.TestRun.Times
        if ($null -ne $times) {
            $start = [DateTimeOffset]::Parse($times.start)
            $finish = [DateTimeOffset]::Parse($times.finish)
            $script:Duration = ($finish - $start).ToString("hh\:mm\:ss")
        }
    } catch {
        Write-Warn "Could not parse TRX summary: $_"
    }
}

try {
    $ErrorActionPreference = "Stop"
    New-Directory $ArtifactsRoot
    New-Directory $E2EArtifacts
    New-Directory $E2EResults
    New-Directory $E2EScreenshots
    New-Directory $E2ETraces
    New-Directory $E2EVideos
    New-Directory $E2ELogs

    Write-Info "Repository root: $RepoRoot"

    if (!(Test-DotNet)) { throw "dotnet CLI not found on PATH." }
    if (!(Test-SqlCmd)) { throw "sqlcmd.exe not found on PATH (required for SQL operations)." }
    if (!(Test-Path $E2ETestProject)) { throw "E2E test project missing: $E2ETestProject" }
    if (!(Test-Path $WebProject)) { throw "Web project missing: $WebProject" }
    if (!(Test-Path $InfraProject)) { throw "Infrastructure project missing: $InfraProject" }
    if (!(Test-Identifier $FinalDatabaseName)) {
        throw "Database name contains illegal characters: '$FinalDatabaseName'"
    }

    $Tfm = Get-TargetFramework $E2ETestProject
    Write-Info "E2E test project TargetFramework: $Tfm"

    Write-Info "Checking DB $FinalDatabaseName does not exist on Server=$ServerInstance ..."
    $dbExists = Test-DatabaseExists $ServerInstance $FinalDatabaseName
    if ($dbExists -and !$KeepDatabase) {
        throw "Database '$FinalDatabaseName' already exists. Refusing to overwrite without -KeepDatabase."
    }

    $ConnStringSafeRedacted = "Server={0};Database={1};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;" -f $ServerInstance, "<DB_NAME>"
    Write-Info "Connection string (secrets redacted): $ConnStringSafeRedacted"
    $ConnStringRaw = "Server={0};Database={1};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;" -f $ServerInstance, $FinalDatabaseName

    # 1. Create empty DB first only if it does not exist (skip CREATE if -KeepDatabase + DB already exists).
    if (!$dbExists) {
        Write-Info "Creating database '$FinalDatabaseName' ..."
        Invoke-Sql $ServerInstance "CREATE DATABASE [$FinalDatabaseName];"
        if (!(Test-DatabaseExists $ServerInstance $FinalDatabaseName)) {
            throw "Database '$FinalDatabaseName' not found after CREATE DATABASE."
        }
    } else {
        Write-Info "Database '$FinalDatabaseName' already exists and -KeepDatabase set; skipping CREATE DATABASE."
    }

    # 2. Set env for EF tooling migrations and seeding (Testing env)
    $env:ASPNETCORE_ENVIRONMENT = "Testing"
    $env:EnableE2ESeed = "1"
    $env:EnableDevSeed = "true"
    $env:SeedDemoData = "0"
    $env:ConnectionStrings__LivestockManagerDb = $ConnStringRaw
    $env:LIVESTOCK_E2E_DATABASE = $FinalDatabaseName
    $env:LIVESTOCK_E2E_SERVER = $ServerInstance

    # 3. dotnet restore
    Write-Info "dotnet restore (E2E project)..."
    Invoke-ExternalNative { dotnet restore $E2ETestProject --verbosity minimal } "dotnet restore failed"

    # 4. Release build
    Write-Info "dotnet build -c Release (E2E project)..."
    Invoke-ExternalNative { dotnet build $E2ETestProject -c Release --no-restore --verbosity minimal } "Release build failed"

    # 5. Apply migrations with the EF tool using the disposable connection string.
    Write-Info "Applying EF Core migrations to '$FinalDatabaseName' ..."
    Invoke-ExternalNative { dotnet ef database update --project $InfraProject --startup-project $WebProject -- --environment Testing } "EF migrations failed"

    # 6. Seed deterministic E2E users and data by running the web host once so
    #    Program.cs startup migrates (already done) + DemoDataSeeder.SeedAsync runs
    #    (ASPNETCORE_ENVIRONMENT=Testing + EnableE2ESeed=1).
    #    Build the app once and run in background for a short time so the
    #    synchronous Main startup scope actually runs SeedAsync.
    $seedPort = Get-AvailablePort
    $seedUrl = "http://localhost:${seedPort}"
    $seedStdout = Join-Path $E2ELogs "e2e-seed-stdout-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    $seedStderr = Join-Path $E2ELogs "e2e-seed-stderr-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    Write-Info "Seeding deterministic E2E data (Testing + EnableE2ESeed=1) via app startup on port $seedPort ..."
    $seedStartArgs = @(
        "run",
        "--project", $WebProject,
        "-c", "Release",
        "--no-build",
        "--",
        "--urls", $seedUrl
    )
    $seedPs = Start-Process -FilePath dotnet -ArgumentList $seedStartArgs -PassThru -NoNewWindow -RedirectStandardOutput $seedStdout -RedirectStandardError $seedStderr
    try {
        if (!(Wait-Health $seedUrl 60)) {
            throw "Application did not reach healthy /health/live during deterministic seeding."
        }
        Start-Sleep -Seconds 3  # allow seeding background tasks to settle
    } finally {
        try {
            if (!$seedPs.HasExited) { Stop-Process -Id $seedPs.Id -Force -ErrorAction SilentlyContinue }
            [void](Wait-Process -Id $seedPs.Id -ErrorAction SilentlyContinue -Timeout 8)
        } catch {}
    }
    Write-Info "Seed logs written to: $seedStdout ; $seedStderr"

    # 7. Pick app port, then start long-running web process for actual tests.
    if ($AppPort -gt 0) {
        $FinalAppPort = $AppPort
    } else {
        $FinalAppPort = Get-AvailablePort
    }
    $AppUrl = "http://localhost:${FinalAppPort}"
    $env:E2E_BASE_URL = $AppUrl
    Write-Info "E2E application will listen at: $AppUrl"

    # Playwright artifacts env vars (used by Microsoft.Playwright.NUnit or test code)
    $env:HEADED = if ($Headed) { "1" } else { "0" }
    $env:PLAYWRIGHT_HEADED = if ($Headed) { "1" } else { "0" }
    $env:E2E_SCREENSHOT_DIR = $E2EScreenshots
    $env:E2E_TRACE_DIR = $E2ETraces
    $env:E2E_VIDEO_DIR = $E2EVideos
    $env:E2E_LOG_DIR = $E2ELogs

    # 8. Locate playwright install CLI shipped with Microsoft.Playwright nupkg via build output.
    $PublishE2EBin = Join-Path $RepoRoot "tests\LivestockManager.EndToEndTests\bin\Release\$Tfm"
    $NodeExe = Join-Path $PublishE2EBin ".playwright\node\win32_x64\node.exe"
    if (!(Test-Path $NodeExe)) { $NodeExe = $null }
    $PlaywrightProgramJs = Join-Path $PublishE2EBin ".playwright\package\lib\cli\program.js"
    if (!(Test-Path $PlaywrightProgramJs)) { $PlaywrightProgramJs = $null }
    $PlaywrightPs1 = Join-Path $PublishE2EBin ".playwright\package\cli.js"
    if (!(Test-Path $PlaywrightPs1)) { $PlaywrightPs1 = $null }
    $PlaywrightCandidatesJs = @(
        (Join-Path $PublishE2EBin ".playwright\package\cli.js"),
        (Join-Path $PublishE2EBin ".playwright\package\lib\cli\program.js"),
        (Join-Path $PublishE2EBin ".playwright\node_modules\playwright-core\bin\playwright.CLI.js"),
        (Join-Path $PublishE2EBin ".playwright\cli.js")
    )
    $PlaywrightScript = $null
    foreach ($c in $PlaywrightCandidatesJs) {
        if (Test-Path $c) { $PlaywrightScript = $c; break }
    }
    if ($null -eq $PlaywrightScript -and (Test-Path (Join-Path $PublishE2EBin ".playwright\package\cli.js"))) { $PlaywrightScript = Join-Path $PublishE2EBin ".playwright\package\cli.js" }
    if ($null -eq $PlaywrightScript -and !$SkipBrowserInstall) {
        Write-Warn "playwright install CLI not found in E2E bin. Attempting 'dotnet build again to ensure Playwright targets ran."
        dotnet build $E2ETestProject -c Release --no-restore --verbosity minimal
        foreach ($c in $PlaywrightCandidatesJs) {
            if (Test-Path $c) { $PlaywrightScript = $c; break }
        }
    }

    # 9. Install Chromium only if browser missing (or skip when -SkipBrowserInstall).
    if (!$SkipBrowserInstall) {
        if ($null -eq $PlaywrightScript) {
            throw "Cannot locate playwright install CLI. Playwright build targets may not have run during build."
        }
        Write-Info "Playwright CLI: $PlaywrightScript"
        $ChromeInstalled = $false

        # Browsers install path: sandboxed repo-local location so installs work
        # even when %LocalAppData%\ms-playwright is unavailable (locked-down
        # profiles, sandboxed CI, etc.). PLAYWRIGHT_BROWSERS_PATH must be set
        # both for the 'playwright install chromium' CLI AND for the test
        # process that later calls Chromium.LaunchAsync().
        $LocalBrowsersRoot = Join-Path $E2EArtifacts ".playwright-browsers"
        New-Directory $LocalBrowsersRoot
        $env:PLAYWRIGHT_BROWSERS_PATH = $LocalBrowsersRoot
        Write-Info "Playwright browsers root: $LocalBrowsersRoot"

        # heuristic: check if any cache dir contains Chromium files
        $LocalAppData = [Environment]::GetFolderPath("LocalApplicationData")
        $CacheDirs = @(
            $LocalBrowsersRoot,
            (Join-Path $LocalAppData "ms-playwright"),
            (Join-Path $env:USERPROFILE ".cache\ms-playwright"),
            (Join-Path $PublishE2EBin ".playwright\.local-browsers")
        )
        foreach ($cd in $CacheDirs) {
            if (Test-Path $cd) {
                $chromium = Get-ChildItem -Path $cd -Recurse -Directory -ErrorAction SilentlyContinue |
                    Where-Object { $_.Name -like "*chromium*" } | Select-Object -First 1
                if ($null -ne $chromium) { $ChromeInstalled = $true; break }
            }
        }
        if (!$ChromeInstalled) {
            Write-Info "Chromium not detected. Installing Chromium via Playwright CLI ..."
            if ($null -ne $NodeExe) {
                Invoke-ExternalNative { & $NodeExe $PlaywrightScript install chromium 2>&1 | Out-Host } "Playwright Chromium install failed"
            } else {
                Invoke-ExternalNative { & node $PlaywrightScript install chromium 2>&1 | Out-Host } "Playwright Chromium install failed (node + CLI)"
            }
        } else {
            Write-Info "Chromium browser already present. Skipping install."
        }
    } else {
        Write-Info "-SkipBrowserInstall set: skipping Chromium install step."
    }

    # 10. Start web process long-running in background.
    $webStdout = Join-Path $E2ELogs "e2e-web-stdout-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    $webStderr = Join-Path $E2ELogs "e2e-web-stderr-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    Write-Info "Starting LivestockManager.Web at $AppUrl (logs: $webStdout ; $webStderr) ..."
    $webStartArgs = @(
        "run",
        "--project", $WebProject,
        "-c", "Release",
        "--no-build",
        "--",
        "--urls", $AppUrl
    )
    $WebProcess = Start-Process -FilePath dotnet -ArgumentList $webStartArgs -PassThru -NoNewWindow -RedirectStandardOutput $webStdout -RedirectStandardError $webStderr
    if (!(Wait-Health $AppUrl 90)) {
        throw "Application did not reach healthy /health/live within timeout on $AppUrl."
    }
    Write-Info "Application healthy on $AppUrl"

    # 11. Run all E2E tests in Release mode with TRX + Loggers.
    Write-Info "Running E2E Playwright tests (Release) against $AppUrl ..."
    $testArgs = @(
        "test", $E2ETestProject,
        "-c", "Release",
        "--no-build",
        "--logger", "trx;LogFileName=$TrxPath",
        "--verbosity", "normal"
    )
    if (![string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArgs += "--filter"
        $testArgs += $TestFilter
    }
    $ErrorRecord = $null
    try {
        & dotnet @testArgs 2>&1 | Tee-Object -FilePath "$E2ELogs\e2e-test-stdout-$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
        $TestExit = $LASTEXITCODE
    }
    catch {
        $TestExit = if ($LASTEXITCODE -ne 0) { $LASTEXITCODE } else { 1 }
        $ErrorRecord = $_
    }

    # 12. Parse TRX summary.
    Parse-TrxSummary $TrxPath
    $Duration = if ([string]::IsNullOrWhiteSpace($Duration)) { ((Get-Date) - $RunStart).ToString("hh\:mm\:ss") } else { $Duration }

    if ($TestExit -ne 0) {
        Write-Warn "E2E tests reported nonzero exit code: $TestExit (see test log file under $E2ELogs for details)."
    }
    if ($null -ne $ErrorRecord) {
        Write-Warn "dotnet test threw an exception (details logged, summary continued)."
    }

    # Exit 0 only when:
    #  - All ran (dotnet exit 0)
    #  - No unexpected skips: skips allowed ONLY when explicitly documented as
    #    "blocked_external" (Playwright/Chromium missing). Because the runner
    #    installs Chromium before tests, any skipped tests beyond 0 are treated
    #    as unexpectedly skipped => nonzero exit.
    if ($TestExit -eq 0 -and $Failed -eq 0 -and $Skipped -eq 0) {
        $ExitCode = 0
    } else {
        $ExitCode = if ($TestExit -ne 0) { $TestExit } else { 2 }
    }
}
catch {
    Write-Fail "Fatal E2E pipeline error: $_"
    $ExitCode = 99
}
finally {
    # Always attempt to stop the web process.
    if ($null -ne $WebProcess -and !$WebProcess.HasExited) {
        try {
            Write-Info "Stopping web process (PID $($WebProcess.Id)) ..."
            Stop-Process -Id $WebProcess.Id -Force -ErrorAction SilentlyContinue
            [void](Wait-Process -Id $WebProcess.Id -ErrorAction SilentlyContinue -Timeout 10)
        } catch {}
    }
    # Conditionally drop the E2E DB.
    if (!$KeepDatabase -and ![string]::IsNullOrWhiteSpace($FinalDatabaseName)) {
        try {
            if (Test-DatabaseExists $ServerInstance $FinalDatabaseName) {
                Write-Info "Dropping disposable E2E database '$FinalDatabaseName' ..."
                $dropQ = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$FinalDatabaseName')
BEGIN
  ALTER DATABASE [$FinalDatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [$FinalDatabaseName];
END
"@
                Invoke-Sql $ServerInstance $dropQ
            }
        } catch {
            Write-Warn "Could not drop E2E database '$FinalDatabaseName': $_"
        }
    }
}

# Final summary.
$TotalWall = ((Get-Date) - $RunStart).ToString("hh\:mm\:ss")
Write-Info "=============== E2E TEST SUMMARY ==============="
Write-Info ("Server        : {0}" -f $ServerInstance)
Write-Info ("Database      : {0}{1}" -f $FinalDatabaseName, ($(if ($KeepDatabase) { " (kept)" } else { " (dropped)" })))
Write-Info ("App URL       : {0}" -f $(if ($AppUrl) { $AppUrl } else { "<not started>" }))
Write-Info ("Mode          : {0}" -f $(if ($Headed) { "Headed" } else { "Headless" }))
Write-Info ("Filter        : {0}" -f $(if ([string]::IsNullOrWhiteSpace($TestFilter)) { "<all>" } else { $TestFilter }))
Write-Info ("Discovered    : $Discovered")
Write-Info ("Passed        : $Passed")
Write-Info ("Failed        : $Failed")
Write-Info ("Skipped       : $Skipped")
Write-Info ("Test Duration : $Duration")
Write-Info ("Wall Duration : $TotalWall")
Write-Info ("TRX           : $TrxPath")
Write-Info ("Exit Code     : $ExitCode")
Write-Info ("Secrets logged: NEVER (passwords and connection strings never printed)")
Write-Info "================================================="

$host.SetShouldExit($ExitCode)
exit $ExitCode
