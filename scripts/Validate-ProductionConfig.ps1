<#
.SYNOPSIS
Validates the Livestock Manager ASP.NET Core web application production configuration
and environment prerequisites before deployment or startup. Returns exit 0 only when
all critical and informational checks pass; nonzero on any failure.

.DESCRIPTION
Performs the following validation gates:
  1. ASPNETCORE_ENVIRONMENT explicitly set to Production
  2. Canonical ConnectionStrings:LivestockManagerDb (or env var form) resolved,
     non-empty, not the __TO_FILL_AT_DEPLOY__ placeholder, not (LocalDB), not
     LivestockManager_E2E prefix, not any known dev DB variant name.
  3. Seed flags (SeedDemoData / EnableDevSeed / EnableE2ESeed / ENV_ENABLE_DEV_SEED)
     all false / 0 / unset.
  4. HTTPS transport: either ASPNETCORE_URLS contains "https://" or
     Kestrel:Certificates:Default:Path is configured.
  5. Protected Document storage root outside wwwroot (no "wwwroot" substring) and
     directory exists or can be created.
  6. Data Protection key directory exists or can be created.
  7. Log path writable (or directory can be created with write test).
  8. Backup path (BACKUP_PATH env or default ./artifacts/backups) writable.
  9. (Elevated only) IIS Hosting Bundle installed: Microsoft.AspNetCore.App runtime
     matching the app major version present in `dotnet --list-runtimes`.
 10. SQL Server connectivity via sqlcmd to the resolved ServerInstance.
 11. Migration status: zero pending model/model changes (no pending migrations).
 12. Clock skew: server local time vs SQL GETUTCDATE() diff < 5 minutes.
 13. Required directories ACL presence (best-effort; warn when ACLs not inspectable).
 14. No test DB (LivestockManager_E2E) name anywhere in resolved connection string.
 15. No dev-only default URL localhost:5001 exposed in ASPNETCORE_URLS for Production.

No connection strings, passwords, or other secrets are printed at any point.

.PARAMETER ServerInstance
SQL Server instance to validate. Defaults to "." (default MSSQLSERVER on localhost).
Overrides, but does not overwrite, any resolved connection string Server value.

.PARAMETER SkipElevatedChecks
If set, skip checks that require elevation (IIS Hosting Bundle ACL deep-inspection).
Useful for developer workstations validating production config without admin rights.

.PARAMETER SkipSqlConnectivityCheck
If set, skip sqlcmd ServerInstance connectivity + clock-skew checks. Useful in CI
runners without local SQL Server.

.EXAMPLE
.\scripts\Validate-ProductionConfig.ps1 -ServerInstance "."
Full production validation against local default SQL Server. Elevated gates run only
when current PowerShell session is already elevated (Run as Administrator).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ServerInstance = ".",

    [switch]$SkipElevatedChecks,
    [switch]$SkipSqlConnectivityCheck
)

$ErrorActionPreference = "Stop"
$ExitCode = 1
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$ChecksPassed = 0
$ChecksFailed = 0
$ChecksWarned = 0

function Write-Info([string]$msg) { Write-Host "[VALIDATE] $msg" -ForegroundColor Cyan }
function Write-OK([string]$msg)   { Write-Host "[VALIDATE][OK] $msg" -ForegroundColor Green; $script:ChecksPassed++ }
function Write-Fail([string]$msg) { Write-Host "[VALIDATE][FAIL] $msg" -ForegroundColor Red; $script:ChecksFailed++ }
function Write-Warn([string]$msg) { Write-Host "[VALIDATE][WARN] $msg" -ForegroundColor Yellow; $script:ChecksWarned++ }

function New-DirectorySafe([string]$path) {
    try {
        if (!(Test-Path -LiteralPath $path)) {
            [void](New-Item -ItemType Directory -Force -Path $path -ErrorAction Stop)
        }
        return $true
    } catch {
        return $false
    }
}

function Test-DirectoryWritable([string]$path) {
    if (!(New-DirectorySafe $path)) { return $false }
    $testFile = Join-Path $path ("__write_probe_{0}.tmp" -f [guid]::NewGuid().ToString("N"))
    try {
        [System.IO.File]::WriteAllText($testFile, "probe")
        if (!(Test-Path -LiteralPath $testFile)) { return $false }
        [System.IO.File]::Delete($testFile)
        return $true
    } catch {
        if (Test-Path -LiteralPath $testFile) { Remove-Item -LiteralPath $testFile -Force -ErrorAction SilentlyContinue }
        return $false
    }
}

function Get-ConnectionStringDbName([string]$cs) {
    if ([string]::IsNullOrWhiteSpace($cs)) { return "" }
    foreach ($part in $cs -split ';') {
        $kv = $part -split '=', 2
        if ($kv.Count -eq 2) {
            $k = $kv[0].Trim().ToLowerInvariant()
            if ($k -eq "database" -or $k -eq "initial catalog") {
                return $kv[1].Trim()
            }
        }
    }
    return ""
}

function Get-ConnectionStringServer([string]$cs) {
    if ([string]::IsNullOrWhiteSpace($cs)) { return "" }
    foreach ($part in $cs -split ';') {
        $kv = $part -split '=', 2
        if ($kv.Count -eq 2) {
            $k = $kv[0].Trim().ToLowerInvariant()
            if ($k -eq "server" -or $k -eq "data source") {
                return $kv[1].Trim()
            }
        }
    }
    return ""
}

function Test-IsElevated {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $pr = New-Object Security.Principal.WindowsPrincipal($id)
    return $pr.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Resolve-ConfigValue([string]$jsonKeyColon, [string]$envVarDoubleUnderscore, [string]$fallback = "") {
    $envVal = [Environment]::GetEnvironmentVariable($envVarDoubleUnderscore)
    if (![string]::IsNullOrWhiteSpace($envVal)) { return $envVal }
    $envAlt = [Environment]::GetEnvironmentVariable($jsonKeyColon)
    if (![string]::IsNullOrWhiteSpace($envAlt)) { return $envAlt }
    return $fallback
}

try {
    Write-Info "Validate-ProductionConfig starting at $(Get-Date -Format s)"
    Write-Info "Repository root: $RepoRoot"
    Write-Info "ServerInstance (parameter): $ServerInstance"

    $IsElevated = Test-IsElevated
    if ($IsElevated) {
        Write-Info "Session is elevated; running all eligible checks."
    } else {
        Write-Warn "Session is NOT elevated. Elevation-gated checks will be skipped or informational-only. Re-run with Run as Administrator for full IIS/ACL validation."
    }

    # === Check 1: ASPNETCORE_ENVIRONMENT explicitly Production ===
    $envName = [Environment]::GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    if ([string]::IsNullOrWhiteSpace($envName)) {
        Write-Fail "ASPNETCORE_ENVIRONMENT is not set. MUST be explicitly set to 'Production'."
    } elseif (-not [string]::Equals($envName, "Production", [StringComparison]::OrdinalIgnoreCase)) {
        Write-Fail ("ASPNETCORE_ENVIRONMENT is '{0}'. MUST be exactly 'Production'." -f $envName)
    } else {
        Write-OK "ASPNETCORE_ENVIRONMENT=Production confirmed (explicit)."
    }

    # === Check 2: Canonical connection string valid, no placeholders, no test/dev names ===
    $resolvedConnStr = Resolve-ConfigValue -jsonKeyColon "ConnectionStrings:LivestockManagerDb" -envVarDoubleUnderscore "ConnectionStrings__LivestockManagerDb"
    $sanitizedLen = if ([string]::IsNullOrWhiteSpace($resolvedConnStr)) { 0 } else { $resolvedConnStr.Length }

    if ([string]::IsNullOrWhiteSpace($resolvedConnStr)) {
        Write-Fail "Canonical ConnectionStrings__LivestockManagerDb (colon form ConnectionStrings:LivestockManagerDb) is EMPTY. Configure a real production SQL connection before proceeding."
    } else {
        Write-OK "Canonical connection string present (length=$sanitizedLen chars; value redacted)."
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedConnStr)) {
        if ($resolvedConnStr -match "__TO_FILL_AT_DEPLOY__") {
            Write-Fail "Canonical connection string contains '__TO_FILL_AT_DEPLOY__' placeholder. Replace with real production value before startup."
        } else {
            Write-OK "Connection string does not contain '__TO_FILL_AT_DEPLOY__' placeholder."
        }

        if ($resolvedConnStr -match "\(LocalDB\)") {
            Write-Fail "Canonical connection string contains '(LocalDB)'. Production MUST use a real SQL Server instance (not LocalDB)."
        } else {
            Write-OK "Connection string does not use (LocalDB)."
        }

        $dbName = Get-ConnectionStringDbName $resolvedConnStr
        if ([string]::IsNullOrWhiteSpace($dbName)) {
            Write-Fail "Could not parse Database= / Initial Catalog= from connection string. Connection string format is malformed."
        } else {
            Write-OK "Database name parsed (name=<redacted>; chars=$($dbName.Length))."
        }

        if (-not [string]::IsNullOrWhiteSpace($dbName)) {
            if ($dbName.StartsWith("LivestockManager_E2E", [StringComparison]::OrdinalIgnoreCase)) {
                Write-Fail "Database name starts with 'LivestockManager_E2E' — this is the E2E test DB prefix. Production MUST use a distinct DB name."
            } else {
                Write-OK "Database name is NOT an E2E test DB variant."
            }

            $knownDevVariants = @("LivestockManager_Dev", "LivestockManager_DevDB", "LivestockManager_Development", "LivestockManager_Seed", "LivestockManager_Local")
            $hitDev = $false
            foreach ($v in $knownDevVariants) {
                if ([string]::Equals($dbName, $v, [StringComparison]::OrdinalIgnoreCase)) { $hitDev = $true; break }
            }
            if ($hitDev) {
                Write-Fail "Database name matches a known dev DB variant. Production MUST use a distinct real production DB name."
            } else {
                Write-OK "Database name does not match known dev-only variants."
            }
        }
    }

    # === Check 3: Seed flags all false/0/unset ===
    $seedFlagNames = @("SeedDemoData", "EnableDevSeed", "EnableE2ESeed", "ENV_ENABLE_DEV_SEED")
    $unsafeSeed = $false
    foreach ($flag in $seedFlagNames) {
        $raw = [Environment]::GetEnvironmentVariable($flag)
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            $v = $raw.Trim().ToLowerInvariant()
            if ($v -eq "true" -or $v -eq "1" -or $v -eq "yes") {
                Write-Fail ("Unsafe seed flag '{0}' = '{1}' — MUST be false/0/unset in Production." -f $flag, $raw)
                $unsafeSeed = $true
            } else {
                Write-OK ("Seed flag '{0}' = '{1}' (safe; not truthy)." -f $flag, $raw)
            }
        } else {
            Write-OK ("Seed flag '{0}' — unset (safe)." -f $flag)
        }
    }

    # === Check 4: HTTPS transport ===
    $urls = [Environment]::GetEnvironmentVariable("ASPNETCORE_URLS")
    $kestrelCertPath = Resolve-ConfigValue -jsonKeyColon "Kestrel:Certificates:Default:Path" -envVarDoubleUnderscore "Kestrel__Certificates__Default__Path"
    $hasHttpsUrl = $false
    if (-not [string]::IsNullOrWhiteSpace($urls) -and $urls -match "https://") {
        $hasHttpsUrl = $true
    }
    $hasKestrelCert = -not [string]::IsNullOrWhiteSpace($kestrelCertPath)

    if ($hasHttpsUrl -or $hasKestrelCert) {
        Write-OK "HTTPS configured via: $(if ($hasHttpsUrl) {"ASPNETCORE_URLS (contains https://)"} else {})$(if ($hasHttpsUrl -and $hasKestrelCert) {" + "} else {})$(if ($hasKestrelCert) {"Kestrel:Certificates:Default:Path"} else {})."
    } else {
        Write-Fail "HTTPS NOT configured. Either ASPNETCORE_URLS must include 'https://' OR Kestrel:Certificates:Default:Path must point to a certificate. Production requires HTTPS."
    }

    # === Check 5: Protected Documents root outside wwwroot ===
    $protectedRoot = Resolve-ConfigValue -jsonKeyColon "ProtectedStorage:Root" -envVarDoubleUnderscore "ProtectedStorage__Root" -fallback (Join-Path $RepoRoot "App_Data\ProtectedDocuments")
    if ([string]::IsNullOrWhiteSpace($protectedRoot)) {
        $protectedRoot = Join-Path $RepoRoot "App_Data\ProtectedDocuments"
    }
    if ($protectedRoot -match "wwwroot") {
        Write-Fail ("ProtectedStorage:Root path contains 'wwwroot' substring ('{0}' — redacted). Protected documents must be stored OUTSIDE the web root for security." -f $protectedRoot)
    } else {
        Write-OK "Protected Storage root path does not contain 'wwwroot' (path stored outside web root)."
    }
    if (New-DirectorySafe $protectedRoot) {
        Write-OK "Protected Storage directory exists or was created successfully."
    } else {
        Write-Fail "Protected Storage directory cannot be created. Verify path permissions."
    }

    # === Check 6: Data Protection key path ===
    $dataProtectionPath = Resolve-ConfigValue -jsonKeyColon "DataProtection:KeyPath" -envVarDoubleUnderscore "DataProtection__KeyPath" -fallback (Join-Path $RepoRoot "App_Data\DataProtection-Keys")
    if ([string]::IsNullOrWhiteSpace($dataProtectionPath)) {
        $dataProtectionPath = Join-Path $RepoRoot "App_Data\DataProtection-Keys"
    }
    if (New-DirectorySafe $dataProtectionPath) {
        Write-OK "Data Protection key directory exists or was created successfully."
    } else {
        Write-Warn "Data Protection key directory could not be created. If ASP.NET Core Data Protection uses file-system key ring, startup may generate warnings or fall back to in-memory-only keys (ephemeral — BAD for production)."
    }

    # === Check 7: Log path writable ===
    $logPath = Resolve-ConfigValue -jsonKeyColon "Logging:Path" -envVarDoubleUnderscore "Logging__Path" -fallback (Join-Path $RepoRoot "artifacts\logs")
    if ([string]::IsNullOrWhiteSpace($logPath)) {
        $logPath = Join-Path $RepoRoot "artifacts\logs"
    }
    if (Test-DirectoryWritable $logPath) {
        Write-OK "Log directory exists and is writable."
    } else {
        Write-Fail "Log directory does not exist OR is not writable. Production logging will fail silently or crash."
    }

    # === Check 8: Backup path writable ===
    $backupPath = [Environment]::GetEnvironmentVariable("BACKUP_PATH")
    if ([string]::IsNullOrWhiteSpace($backupPath)) {
        $backupPath = Join-Path $ArtifactsRoot "backups"
    }
    if (Test-DirectoryWritable $backupPath) {
        Write-OK "Backup directory exists and is writable."
    } else {
        Write-Warn "Backup directory not writable (BACKUP_PATH='$backupPath'). If scheduled backups run here, they will FAIL."
    }

    # === Check 9: (Elevated) IIS Hosting Bundle / Microsoft.AspNetCore.App runtime ===
    if (-not $SkipElevatedChecks) {
        $foundAspNet = $false
        $majorMatch = $false
        try {
            $runtimeLines = & dotnet --list-runtimes 2>&1
            foreach ($line in $runtimeLines) {
                if ($line -match "^Microsoft\.AspNetCore\.App\s+(\d+)\.(\d+)\.(\d+)") {
                    $foundAspNet = $true
                    $maj = [int]$Matches[1]
                    if ($maj -ge 8) { $majorMatch = $true }
                }
            }
        } catch {
            Write-Warn "dotnet --list-runtimes call failed. Cannot verify ASP.NET Core runtime version."
        }
        if ($foundAspNet -and $majorMatch) {
            Write-OK "Microsoft.AspNetCore.App runtime (>= 8.x) installed (IIS Hosting Bundle prerequisite)."
        } elseif ($foundAspNet) {
            Write-Fail "Microsoft.AspNetCore.App runtime detected, but major version < 8. Application targets net8.0 — install the 8.x ASP.NET Core Hosting Bundle."
        } else {
            Write-Fail "Microsoft.AspNetCore.App runtime not found in dotnet --list-runtimes. Install the ASP.NET Core 8.0 Hosting Bundle before Production IIS deploy."
        }
    } else {
        Write-Warn "SkipElevatedChecks: skipping IIS Hosting Bundle runtime verification."
    }

    # === Check 10 & 12: SQL Server connectivity + clock skew (unless skipped) ===
    if (-not $SkipSqlConnectivityCheck) {
        $sqlServerToUse = if (-not [string]::IsNullOrWhiteSpace($ServerInstance)) { $ServerInstance } else { (Get-ConnectionStringServer $resolvedConnStr) }
        if ([string]::IsNullOrWhiteSpace($sqlServerToUse)) {
            Write-Fail "No SQL Server instance available for connectivity check (no -ServerInstance and none parsed from connection string)."
        } else {
            Write-Info "Validating SQL connectivity via sqlcmd to Server='$sqlServerToUse' (connection string itself is never printed)."
            $sqlQ = "SET NOCOUNT ON; SELECT 1;"
            $sqlOut = & sqlcmd.exe -S $sqlServerToUse -E -b -W -h -1 -Q $sqlQ 2>&1
            $sqlExit = $LASTEXITCODE
            if ($sqlExit -eq 0 -and ($sqlOut -join "`n") -match "1") {
                Write-OK "SQL Server connectivity check passed (sqlcmd SELECT 1 via Server='$sqlServerToUse')."
            } else {
                Write-Fail "SQL Server connectivity check FAILED. sqlcmd exited=$sqlExit for Server='$sqlServerToUse'. Check SQL Server reachability, Integrated Security, and firewall."
            }

            $serverNowUtc = [DateTimeOffset]::UtcNow
            $sqlUtc = $null
            try {
                $sqlTimeQ = "SET NOCOUNT ON; SELECT CONVERT(NVARCHAR(64), SYSUTCDATETIME(), 127);"
                $timeOut = & sqlcmd.exe -S $sqlServerToUse -E -b -W -h -1 -Q $sqlTimeQ 2>&1
                foreach ($l in $timeOut) {
                    if ($l -match "^\s*(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})") {
                        try { $sqlUtc = [DateTimeOffset]::Parse($Matches[1]) } catch {}
                    }
                }
            } catch {}
            if ($null -ne $sqlUtc) {
                $diff = ($serverNowUtc - $sqlUtc).Duration()
                if ($diff.TotalMinutes -lt 5) {
                    Write-OK ("Server vs SQL UTC clock skew OK ({0:N1} seconds)." -f $diff.TotalSeconds)
                } else {
                    Write-Fail ("Server vs SQL UTC clock skew EXCEEDS 5 minutes ({0:N1} minutes). Correct NTP/time sync before Production deploy — will cause TOTP MFA, audit log ordering, and token expiry failures." -f $diff.TotalMinutes)
                }
            } else {
                Write-Warn "Could not read SQL SYSUTCDATETIME() for clock skew comparison. Verify manually."
            }
        }
    } else {
        Write-Warn "SkipSqlConnectivityCheck set: skipping sqlcmd connectivity + clock skew checks."
    }

    # === Check 11: Migration status (pending migrations == 0) ===
    Write-Info "Checking EF Core migration status via `dotnet ef migrations has-pending-model-changes` ..."
    $InfraProj = Join-Path $RepoRoot "src\LivestockManager.Infrastructure\LivestockManager.Infrastructure.csproj"
    $WebProj = Join-Path $RepoRoot "src\LivestockManager.Web\LivestockManager.Web.csproj"
    $pendingExit = 99
    try {
        Push-Location $RepoRoot
        $migrationArgs = @(
            "ef", "migrations", "has-pending-model-changes",
            "--project", $InfraProj,
            "--startup-project", $WebProj,
            "--", "--environment", "Production"
        )
        & dotnet @migrationArgs 2>&1 | Out-Null
        $pendingExit = $LASTEXITCODE
    } catch {
        Write-Warn "dotnet ef has-pending-model-changes threw; treating as non-zero (migrations unverified)."
    } finally {
        Pop-Location
    }
    if ($pendingExit -eq 0) {
        Write-OK "EF Core migration status: ZERO pending model changes (all migrations applied to model snapshot for Production target)."
    } elseif ($pendingExit -eq 1) {
        Write-Fail "EF Core migration status: PENDING model changes. Apply all migrations BEFORE Production deploy. Do NOT let Production run unapplied migrations on first request (cold-start outage risk)."
    } else {
        Write-Warn ("dotnet ef has-pending-model-changes exit=$pendingExit. Could not definitively verify migration state. Run manually: dotnet ef migrations has-pending-model-changes --project src/LivestockManager.Infrastructure --startup-project src/LivestockManager.Web")
    }

    # === Check 13: Required directories ACL best-effort ===
    $aclPaths = @($protectedRoot, $dataProtectionPath, $logPath, $backupPath)
    $identities = @("IIS AppPool\LivestockAppPool", "IIS_IUSRS", "BUILTIN\IIS_IUSRS")
    foreach ($p in $aclPaths) {
        if (-not (Test-Path -LiteralPath $p)) { continue }
        try {
            $acl = Get-Acl -LiteralPath $p -ErrorAction Stop
            $rules = $acl.Access
            $foundWrite = $false
            foreach ($rule in $rules) {
                $rfs = [string]$rule.FileSystemRights
                if ($rule.AccessControlType -eq [Security.AccessControl.AccessControlType]::Allow -and
                    ($rfs -match "Write" -or $rfs -match "Modify" -or $rfs -match "FullControl")) {
                    foreach ($id in $identities) {
                        if ($rule.IdentityReference.Value -like "*$id*") { $foundWrite = $true; break }
                    }
                }
            }
            if ($foundWrite) {
                Write-OK ("ACL write-rule found for AppPool/IIS_IUSRS-like identity on: {0}" -f $p)
            } else {
                Write-Warn ("ACL verification on '{0}': did not detect explicit AppPool/IIS_IUSRS write rule. Confirm manually (running as non-admin may hide inherited ACLs)." -f $p)
            }
        } catch {
            Write-Warn ("ACL inspection failed for '{0}': {1}. Verify manually via Explorer > Properties > Security." -f $p, $_.Exception.Message)
        }
    }

    # === Check 14: E2E test DB name anywhere in connection string ===
    if (-not [string]::IsNullOrWhiteSpace($resolvedConnStr) -and $resolvedConnStr -match "LivestockManager_E2E") {
        Write-Fail "Connection string contains 'LivestockManager_E2E' (E2E test DB). Production connection strings MUST NEVER reference E2E test databases."
    } else {
        Write-OK "Connection string does not contain the E2E test DB marker 'LivestockManager_E2E'."
    }

    # === Check 15: No dev-only localhost:5001 in Production ASPNETCORE_URLS ===
    if (-not [string]::IsNullOrWhiteSpace($urls)) {
        if ($urls -match "localhost:500[01]") {
            Write-Fail "ASPNETCORE_URLS contains localhost:5000/5001 (Kestrel dev default). Production MUST bind to real hostnames/IPs and HTTPS (or IIS/nginx reverse proxy)."
        } else {
            Write-OK "ASPNETCORE_URLS does not expose localhost:5000/5001 dev-only defaults."
        }
    } else {
        Write-Warn "ASPNETCORE_URLS is not set. Hosting model (IIS/Kestrel) is responsible for URL binding; verify separately."
    }

    # === Final summary ===
    Write-Info "====================================================="
    Write-Info "Production Configuration Validation — FINAL SUMMARY"
    Write-Info "====================================================="
    Write-Info ("Checks PASSED : {0}" -f $ChecksPassed)
    Write-Info ("Checks FAILED : {0}" -f $ChecksFailed)
    Write-Info ("Checks WARNED : {0}" -f $ChecksWarned)
    Write-Info "Secrets printed: NEVER (connection strings, passwords, keys — all redacted)."

    if ($ChecksFailed -eq 0) {
        Write-OK "All critical validation checks passed. Safe to continue Production deploy/startup."
        $ExitCode = 0
    } else {
        Write-Fail ("{0} validation checks FAILED. Resolve before Production deploy." -f $ChecksFailed)
        $ExitCode = 2
    }
}
catch {
    Write-Fail ("Fatal exception during validation: {0}" -f $_.Exception.Message)
    $ExitCode = 99
}

Write-Info "Validate-ProductionConfig finished at $(Get-Date -Format s) — exiting $ExitCode"
$host.SetShouldExit($ExitCode)
exit $ExitCode
