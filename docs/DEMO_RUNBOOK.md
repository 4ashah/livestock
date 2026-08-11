# Livestock Manager — Local Development Demo Runbook

> **STATUS:** LOCAL DEVELOPMENT / STAGING DEMO ONLY. NOT PRODUCTION.
> Document target audience: Engineering, Demo operators, Independent auditors doing in-person walkthroughs.
> Demo build baseline: commit `f722925` on branch `remediation/final-audit-round-2`. Font status: UNICODE_BLOCKED_EXTERNAL_WITH_SAFE_BASIC_FALLBACK (WinAnsi Helvetica only — genuine Noto TTFs not yet installed, so no Unicode glyph promises for PDFs).

---

## 1. Prerequisites

Mandatory prerequisites for the demo machine (Windows 10/11 x64 recommended):

| Component | Minimum version | How to verify |
|-----------|-----------------|---------------|
| .NET 8 SDK | `8.0.100` or newer `8.0.xxx` SDK band | Run: `dotnet --list-sdks` |
| SQL Server | Express / LocalDB / Developer default instance at `Server=.` | Run: `sqlcmd -S "." -d "master" -Q "SELECT @@VERSION;"` |
| PowerShell | 5.1 or 7+ Desktop/Core | Run: `$PSVersionTable.PSVersion` |
| Playwright browsers for .NET | Chromium installed locally | Run once (if missing): `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/install-playwright.ps1` (or from NuGet cache: `dotnet tool install --global Microsoft.Playwright.CLI ; playwright install chromium`) |

Optional but recommended: 8 GB RAM minimum for SQL + .NET host + Chromium during E2E verification.

---

## 2. Start Command

Run **exactly ONE** of the following from the **repository root** (`C:\Projects\livestock`):

### Preferred wrapper (repo checked in, handles env + URLs):
```
.\run-dev.cmd
```

### Fallback inline PowerShell / CMD equivalent (if wrapper missing or broken):
```
ASPNETCORE_ENVIRONMENT=Development EnableDevSeed=true dotnet run --project src/LivestockManager.Web
```

PowerShell 5 form (explicit variable scoping for child `dotnet.exe`):
```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:EnableDevSeed='true'
dotnet run --project src/LivestockManager.Web
```

**What happens on start:**
1. Builds debug config of `LivestockManager.Web`.
2. Applies any pending EF migrations to `Server=.` / database `LivestockManager` on first run.
3. Runs `DemoDataSeeder` ONLY in `ASPNETCORE_ENVIRONMENT=Development` when `EnableDevSeed=true` is truthy. Seeds: 4 roles + 7 users (viewer/dataentry/companyadmin/sysadmin plus 3 support demo accounts), 1 Company, 2 Farms, 5 Livestock, 1 Customer.
4. Begins listening on the configured `ASPNETCORE_URLS` localhost ports (see §3).

---

## 3. Demo URLs (localhost ONLY — NEVER expose to LAN/Internet)

From `launchSettings.json` development profiles:

- **HTTP (primary for demo):** `http://localhost:5100`
- **HTTPS (if Kestrel Https certs installed locally):** `https://localhost:5101`

Quick smoke-health probe (run BEFORE walkthrough to confirm warm-up):
```powershell
Invoke-WebRequest -Uri "http://localhost:5100/health/live" -UseBasicParsing | Select-Object StatusCode, Content
```
Expected: `StatusCode = 200` + JSON payload containing `"live":"UP"`.

> ⚠️ The demo seed and demo credentials only work when `ASPNETCORE_ENVIRONMENT=Development` AND `EnableDevSeed=true`. Do not attempt to log in with demo accounts under `ASPNETCORE_ENVIRONMENT=Production`; Identity will reject unknown sign-ins.

---

## 4. DEVELOPMENT-ONLY Login Credentials

🔴 **NEVER EVER USE THESE IN PRODUCTION. Rotate or delete these seeded users before any Production publish. 🔴**

| Role | Login email | Password | Capabilities for demo |
|------|-------------|----------|-----------------------|
| System Administrator (full everything) | `sysadmin@livestock.dev` | `Dev@123456` | Creates companies, farms, users, roles, full finances, auditor audit-logs view. Use for walkthrough §5 steps 1–10. |
| Company Administrator | `companyadmin@livestock.dev` | `Dev@123456` | Scoped inside one seeded company. Accounts + sales OK. |
| Data Entry (least-privilege form fills) | `dataentry@livestock.dev` | `Dev@123456` | Can register livestock, add weight records, create customers/suppliers. Cannot delete, finalize invoices, or modify roles. |
| View-only / Auditor read-only | `viewer@livestock.dev` | `Dev@123456` | Read-only, perfect for auditors shadowing walkthrough. |
| `admin@livestock.dev` alias | `admin@livestock.dev` | `Dev@123456` | Original legacy seeded admin — same scope as company-admin for backward compatibility. |

Login page path: `http://localhost:5100/Account/Login`. If a returnUrl query param appears, Identity will redirect to it post-sign-in.

---

## 5. Five-Minute Demo Sequence (10 steps)

Run these 10 steps in order, from a freshly opened **Incognito / Guest Chrome / Edge** window to avoid stale cookies. Use role = `sysadmin@livestock.dev` / `Dev@123456`.

1. **Login** → Open `http://localhost:5100/Account/Login`. Enter `sysadmin@livestock.dev` + `Dev@123456`, click **Sign In**. Confirm redirect to `/` with Dashboard heading `📊 Dashboard`.
2. **Dashboard** → Land on `/Home` (Dashboard). Walk audience through 4 KPI cards (Total Livestock headcount, Active Customers, Unpaid invoices $, Month Sales $). Confirm clickable KPI cards jump to filterable lists.
3. **Register Livestock** → Nav `/Livestock/Register`. Fill: Farm = first dropdown, Acquisition date = today, Type radio = Cattle (Ah), Initial weight `= 250.50` kg, Purchase amount `= $1,250.00`, Comments `= "Round 2 demo animal"`. Click **Create**. Confirm redirect to Livestock list.
4. **Add Weight** → From Livestock list click on the newly created row, then action menu → **Add Weight**, or go to `/Livestock/AddWeight/<id>` directly. Enter today + `265.75` kg. Click **Save**. Confirm history row appended on livestock detail.
5. **Create Customer** → `/Customers/Create`. Fill Code = `"DEMOCUST-001"`, Name = `"Demo Customer Alpha"`, Phone, Email, Billing Country = `US`. Click **Create**. Confirm new customer now searchable under `/Customers`.
6. **Create Sale** → `/Sales/Create`. Pick: Farm, Customer = the one just created, Date = today. **Check at least 1 livestock checkbox** in the `Select Livestock (Active)` grid (this is required field). Price auto-fills suggested amount — edit one line to `$250.00`. Notes = `Round-2 demo sale`. Click **Create**.
7. **Open Invoice** → Sale creation auto-generates draft invoice. From `/Sales/Details/<newSaleId>` click the **View Invoice** link OR navigate from sidebar Invoices → most recent row → click **View** (`/Invoices/Details/<newId>`). Verify line items, Subtotal + Tax + Grand total math.
8. **Record Partial Payment** → On Invoice Details page click **Record Payment** button (navigates `/Payments/Create?invoiceId=<id>`). Amount = `$150.00` (partial), Method = BankTransfer, Reference = `CHECK-999-01`. Click **Create**. Confirm Invoice Outstanding decreases and new Payment row appears in timeline.
9. **Generate Receipt** → Pay the remainder: record second Payment for the exact Outstanding balance. Post-success, click the **Receipt** link (or go `/Receipts/Details/<rcptId>` → **Download PDF**). Save to Downloads; confirm bytes > 0, `application/pdf`.
10. **Mobile viewport (390×844 iPhone-style)** → Open Chrome DevTools → Device Toolbar → Dimensions = `iPhone 14` (390 × 844), reload `/Home`. Tap the **☰ hamburger** top-left to open sidebar drawer (confirm `.sidebar.open` + `.sidebar-backdrop.show`), scroll the drawer links, tap **Reports** inside drawer → Reports page opens. Tap backdrop → drawer closes cleanly, no double-tap glitch.

---

## 6. Stop Command

Graceful shutdown (preferred, gives EF change tracker and SQL connections a clean flush):

- Press `Ctrl + C` **ONCE** in the running terminal window hosting `dotnet run` or `.\run-dev.cmd`. Wait ~3s for the "Application is shutting down..." lifetime message.

Forcible termination (only if Ctrl+C hangs or window is unresponsive):
```powershell
Get-Process dotnet | Where-Object { (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine -match 'LivestockManager\.Web' } | Stop-Process -Force
```

Do NOT leave background `dotnet LivestockManager.Web` processes running — they hold SQL connection pools and lock SQLite App_Data files in some debug profiles.

---

## 7. Troubleshooting (3 Common Issues)

### Issue #1 — Port 5100 or 5101 already in use
Symptom: Start fails with `IOException: Failed to bind to address http://127.0.0.1:5100: address already in use.`
```powershell
netstat -ano | findstr :5100
# Kill PID shown in rightmost column
Stop-Process -Id <PID-FROM-NETSTAT> -Force
```
Then retry §2 start command. OR override with a custom port:
```
$env:ASPNETCORE_URLS='http://localhost:5200' ; .\run-dev.cmd
```

### Issue #2 — SQL not accessible / `Login failed` / `"."` instance not found
Symptom: EF migration throws provider-level errors or `A network-related or instance-specific error occurred while establishing a connection to SQL Server`.
- Verify default instance: `sqlcmd -S "." -d master -Q "SELECT @@SERVICENAME;"` must return non-error.
- If LocalDB only: change `appsettings.Development.json` connection string `Server=.` to `Server=(LocalDB)\\MSSQLLocalDB;`.
- Confirm Integrated Security works under the current Windows identity; do NOT attempt demo with SQL Auth SA unless audit explicitly required.

### Issue #3 — Demo seed not running ("Invalid login attempt" even with correct credentials)
Symptom: `DemoDataSeeder` did not fire → no demo users exist in AspNetUsers table.
Verify BOTH conditions are truthy:
1. `$env:ASPNETCORE_ENVIRONMENT` MUST equal exactly `Development` (case-insensitive check in hosting, but avoid "Developpment" typos).
2. `EnableDevSeed=true` set either in `appsettings.Development.json` or as an environment variable. Check live in Dashboard footer or logs: look for `[LivestockManager.Infrastructure] DemoDataSeeder.SeedAsync starting` info line during startup.

If still broken: delete the `LivestockManager` DB once (DEMO ONLY, NEVER PROD):
```sql
USE master; ALTER DATABASE [LivestockManager] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [LivestockManager];
```
Then re-run §2 start command — fresh migration + full seed runs.

---

## 8. NOT Production Statement (Read Before Every Demo!)

```
This runbook documents a LOCAL DEVELOPMENT / STAGING DEMO environment ONLY.
It is not Production. Do not use real customer data, do not expose port to public internet, and do not run with ASPNETCORE_ENVIRONMENT=Production.
```

Do not use this runbook as a deployment playbook for real production hosting. Production deployment follow `docs/DEPLOYMENT.md` + `scripts/publish-iis.ps1` + hardened connection strings + key-vault secrets + non-integrated SQL Auth + audit-level TLS 1.3 termination behind WAF. Demo seed users MUST be deleted or password-rotated before any promotion to higher environments.
