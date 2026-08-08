# E2E TESTING (Playwright for .NET, no npm/node/TypeScript/React)

Livestock Manager E2E tests use **Microsoft.Playwright** and **Microsoft.Playwright.NUnit**
nupkg directly inside the existing .NET test project `tests/LivestockManager.EndToEndTests`.
No `node`, `npm`, `yarn`, `pnpm`, `TypeScript`, or `React` is used, installed, or required.

## What the runner does

`scripts/Run-E2ETests.ps1` orchestrates:

1. Auto-detects repository root and E2E project `TargetFramework`.
2. Runs `dotnet restore` then `dotnet build -c Release`.
3. Locates the `playwright.ps1` / `playwright.CLI.js` helper installed by the
   Playwright build targets inside the E2E project's `bin/Release/net8.0/.playwright/`.
4. Installs **Chromium only if the Playwright browser is missing** (via cached
   `ms-playwright` directory heuristic). Skip with `-SkipBrowserInstall`.
5. Creates a **dedicated disposable SQL Server database** named
   `LivestockManager_E2E<unique timestamp suffix>` on the specified instance.
   Never uses or deletes your development or production database.
   The database is dropped after the run unless `-KeepDatabase` is supplied.
6. Sets `ASPNETCORE_ENVIRONMENT=Testing` + `EnableE2ESeed=1` so `DemoDataSeeder`
   runs deterministic roles and users.
7. Applies all EF Core migrations via `dotnet ef database update` using the
   disposable connection string.
8. Starts `LivestockManager.Web` once to ensure the startup scope actually
   runs `SeedAsync` (Testing env + `EnableE2ESeed=1`).
9. Picks an available localhost port (or the `-AppPort` you provided) and
   starts the web host long-running.
10. Waits for `/health/live` with a bounded timeout (app startup fail => nonzero exit).
11. Exports `E2E_BASE_URL`, `E2E_SCREENSHOT_DIR`, `E2E_TRACE_DIR`, `E2E_VIDEO_DIR`,
    and `E2E_LOG_DIR`.
12. Runs all E2E tests in Release mode and writes a `.trx` result file under
    `artifacts/e2e/results`.
13. Screenshots, traces (on failure), videos (opt-in), and application logs
    are stored under `artifacts/e2e/{screenshots,traces,videos,logs}`.
14. Stops the web process in a `finally` block.
15. Drops the disposable E2E database unless `-KeepDatabase` was supplied.
16. Prints a final summary (discovered / passed / failed / skipped / duration +
    exit code) and **never prints passwords or connection strings**.

## Runner parameters

PowerShell: `scripts\Run-E2ETests.ps1 [-ServerInstance <string>] [-DatabaseName <string>]
[-AppPort <int>] [-KeepDatabase] [-Headed] [-SkipBrowserInstall] [-TestFilter <string>]`

| Parameter | Default | Purpose |
|---|---|---|
| `-ServerInstance` | `.` | SQL Server instance. Use `.\SQLEXPRESS` for SQLEXPRESS, tcp:FQDN,1433 for remote. |
| `-DatabaseName` | `LivestockManager_E2E` | Base DB name. A unique suffix is appended unless `-KeepDatabase` is set. |
| `-AppPort` | OS-assigned free port | Force a fixed localhost port (e.g. `6005`). |
| `-KeepDatabase` | false | Do not drop the E2E DB after the run; also disables the unique suffix (useful to debug). |
| `-Headed` | false (headless) | Show a visible Chromium window during tests. Good for debugging. |
| `-SkipBrowserInstall` | false | Skip the `playwright install chromium` step if your workstation already has it. |
| `-TestFilter` | (all tests) | A `dotnet test --filter` expression. Example: `FullyQualifiedName~Workflow_01`. |

## Local developer invocations

**With a visible Chromium window (handy for debugging):**
```powershell
.\scripts\Run-E2ETests.ps1 -ServerInstance "." -Headed
```

**Automated / CI / headless (default):**
```powershell
.\scripts\Run-E2ETests.ps1 -ServerInstance "."
```

**CMD wrapper (same semantics, positional tokens):**
```
run-e2e-tests.cmd headed                        :: local developer, visible Chromium
run-e2e-tests.cmd                               :: headless automated, Server=.
run-e2e-tests.cmd ".\SQLEXPRESS" headed         :: custom instance + headed
run-e2e-tests.cmd . "" 6005 keepdatabase        :: fixed port 6005 + keep E2E DB
set TEST_FILTER=FullyQualifiedName~Workflow_01
run-e2e-tests.cmd                               :: run a single workflow by filter
```

## External blockers & conditional skips

The 18 remediation checklist tests deliberately **do not use `[Fact(Skip = ...)]`**.
Instead, every test calls `E2ETestEnvironment.DetectBlocker()` and
`Skip.If(true, documentedReason)` at the first point of Playwright/host usage.
This means:

| Situation | Outcome | Runner Exit |
|---|---|---|
| All green | Tests run; pass count = 18 | `0` |
| `E2E_BASE_URL` not set (no app runner used) | Skip = 18 with reason `blocked_external: E2E_BASE_URL` | nonzero (unexpected skip = pipeline fail) |
| Playwright + Chromium missing | Skip = 18 with reason `blocked_external: Playwright/Chromium` | nonzero (unexpected skip = pipeline fail) |
| Real assertions fail | Failed = N | nonzero (dotnet exit) |
| App startup or migrations fail | Pipeline error before tests run | nonzero |

The only way to produce a zero exit code is to run the orchestrator, have all
blockers resolved, and have every test pass (expected skipped count = 0).

## Artifacts produced

| Directory | Contents |
|---|---|
| `artifacts/e2e/results` | `.trx` test result file. |
| `artifacts/e2e/screenshots` | Full-page PNG screenshots on failure. File name = test id + UTC timestamp. |
| `artifacts/e2e/traces` | Playwright `.zip` traces on failure (when the installed browser supports it). |
| `artifacts/e2e/videos` | Videos when `E2E_ENABLE_VIDEO=1` is set externally. Heavy; disabled by default. |
| `artifacts/e2e/logs` | Web host stdout/stderr from the seeding run and the long-running test run; plus `e2e-tests.log` from tests. |

## Deterministic E2E users (DemoDataSeeder in Testing mode)

The runner exports `ASPNETCORE_ENVIRONMENT=Testing` + `EnableE2ESeed=1`. This
activates `DemoDataSeeder.SeedAsync` for the Testing environment exactly the
same way it works in Development, producing the deterministic 6 users:

| Email | Role | Password (Development/Testing ONLY) |
|---|---|---|
| `admin@livestock.dev` | CompanyAdministrator | `Dev@123456` |
| `accounts@livestock.dev` | Accounts | `Dev@123456` |
| `farmmanager@livestock.dev` | FarmManager | `Dev@123456` |
| `dataentry@livestock.dev` | DataEntry | `Dev@123456` |
| `viewer@livestock.dev` | Viewer | `Dev@123456` |
| `sysadmin@livestock.dev` | SystemAdministrator | `Dev@123456` |

Passwords are used only inside `DemoDataSeeder` (Dev-only). They are never
printed by the runner; never logged to app stdout, trx, or screenshots names.

## Exit codes (nonzero = block further automation)

| Exit | When |
|---|---|
| 0 | All E2E tests discovered = 18, passed = 18, failed = 0, skipped = 0. |
| 1-99 (dotnet exit) | One or more E2E tests failed. `trx` is written; inspect failures. |
| 2 | dotnet exit 0 but skips nonzero (missing documented blocker). Use the runner. |
| 10-15 / 99 | Pipeline setup error (sqlcmd missing, sql not connectable, migrations fail, browser install fail, app startup not healthy). |

## Required one-time workstation prerequisites

- .NET SDK 8 (already required by the solution).
- `sqlcmd.exe` on PATH (comes with SQL Server / SSMS / `Microsoft.SqlServer.SqlCmd` package).
- Windows-only at the moment; runner uses Windows trusted auth `sqlcmd -E`.
- No Node.js, npm, or external browser downloads required beyond Playwright's
  own `playwright install chromium` (which downloads only Chromium from
  Playwright's CDN; no npm packages installed).

## Troubleshooting

1. **`Playwright managed assembly, native playwright CLI, or Chromium browser is unavailable`**
   Run `.\scripts\Run-E2ETests.ps1 -ServerInstance "."` once WITHOUT `-SkipBrowserInstall`.
   The runner installs Chromium automatically when absent. If it still fails,
   inspect `tests\LivestockManager.EndToEndTests\bin\Release\net8.0\.playwright\` —
   the Playwright `buildMultiTargeting` targets in `Microsoft.Playwright` produce
   this directory during build + restore.
2. **`E2E_BASE_URL environment variable is not set`**
   Don't run the tests via standalone `dotnet test` unless you first start the
   app and export `E2E_BASE_URL`. Always use `scripts\Run-E2ETests.ps1` or
   `run-e2e-tests.cmd` for normal runs.
3. **`System.AggregateException` / DB deadlocks on very slow machines**
   Raise health timeouts in the script constants or set `-AppPort` explicitly.
4. **`TestFilter` not working**
   Syntax for `dotnet test --filter`: `FullyQualifiedName~<class_or_method_substring>`,
   e.g. `FullyQualifiedName~Workflow_10` to run the PDF test alone.
