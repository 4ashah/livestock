# FULL AUDIT SECURITY / AUTH / TENANT ISOLATION RESULTS — Sections 11+18+20

**Status:** COMPLETE

---

## 11.A Role × Feature Matrix (Key Findings)

| Feature / 6 Roles | Viewer | DataEntry | FarmMgr | Accounts | CoAdmin | SysAdmin |
|---|---|---|---|---|---|---|
| View Operational Dashboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Financial KPIs on Dashboard** | ❌ **FAUD-0002 LEAK** | ❌ FAUD-0002 | — | ✅ | ✅ | ✅ |
| Livestock List+Details | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Register/Edit/Discharge Stock | — | ✅ | ✅ | ✅ | ✅ | ✅ |
| Stock Addition NP+NB | — | ✅ | ✅ | ✅ | ✅ | ✅ |
| Farms CRUD | — | — | — | — | ✅ | ✅ |
| Customers/Suppliers | — | — | ✅ | ✅ | ✅ | ✅ |
| Purchases/Expenses CRUD | — | — | ✅ | ✅ | ✅ | ✅ |
| Sales Draft/Confirm/Reverse | — | — | ✅ | ✅ | ✅ | ✅ |
| Invoices/Payments/Receipts | — | — | — | ✅ | ✅ | ✅ |
| Reports ActiveLivestock (op) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Reports Financial 3 + CSV | — | — | — | ✅ | ✅ | ✅ |
| Documents Upload | — | — | — | ✅ | ✅ | ✅ |
| **Documents List+Download** | ❌ **FAUD-0010 LEAK** | ❌ FAUD-0010 | ⚠️ FarmM access fin PDF | ✅ | ✅ | ✅ |
| Settings normal / companyId | — — | — — | — — | — — | ✅ — | ✅ ✅ |
| Users/Roles + Companies | — — | — — | — — | — — | ✅ — | ✅ ✅ |
| Audit Logs | — | — | — | — | ✅ | ✅ |
| Health x3 | Public | Public | Public | Public | Public | Public |

## 11.B Cross-Company Tenant Isolation (16 entities × 8 vectors)

Known leaks classified:
- **FAUD-0001 CRIT** CompanyService.GetDefault/List no filter → cross-company default
- **FAUD-0002 CRIT** Wrong-role financial dashboard (authorization, not cross-co, grouped here)
- **FAUD-0003 CRIT** LivestockService Register company fallback arbitrary first Co via `FirstAsync` no filter
- **FAUD-0008 HIGH** 6 service-level .Customer nav properties not re-verified CompanyId after FK join invoice/sale/payment/receipt GetById → 6 locations PII cross-company possible via navigation property access despite correct invoice-level company WHERE

All other vectors (hidden form CompanyId overwrite, JSON endpoint, CSV export, Download storage-level, Search API, BulkAdd eligibility, Parent ID tamper) PASS correctly with company scope applied. Server overwrites hidden `dto.CompanyId = identityCompanyId` for ALL 12 form POST endpoints → ZERO endpoints accept client CompanyId as authoritative ✅

## 11.C 8 Threat Injection Tests All PASS

| # | Vector | Result |
|---|---|---|
| T1 URL ID tamper /Livestock/Details/B-Guid | 404 ✅ |
| T2 <hidden> CompanyId=B overwrite server-side → identity company | Overwritten ✅ 0 endpoints accept hidden |
| T3 JSON /Sales/BulkAdd body companyId=B | Not accepted; company via identity ✅ |
| T4 Mobile MobilePurchase tamper farmId | Service farm eligibility filters by user company ✅ |
| T5 Download?documentId=Cross-Co | ProtectedDocumentStorage.CompanyId path isolation check throws UnauthorizedAccessException ✅ FAUD-0010 is role-level not storage-level |
| T6 SearchCustomers?keyword | Always WHERE Customer.CompanyId = userCompanyId ✅ |
| T7 BulkAdd mix animals A+B companies | B company filtered out; Skipped counter reported ✅ |
| T8 Parent Search submitted Cross-Co IDs | Eligibility filter returns empty; cannot register cross-co parent offspring ✅ |

## 18. Health / Errors / Logging

3 health endpoints (/health, /health/live, /health/ready). DB fails → Degraded, not Unhealthy (correct). Health JSON schema = minimal (status + duration + check name) = no secrets.
Production exceptions → /Home/Error with RequestId only = no stack trace ✅. DeveloperExceptionPage only for IsDevelopment ✅.
Empty catch blocks = 12 total (FAUD-0006 2 startup catches CRITICAL; others FAUD-0023 MEDIUM).
No conn strings / passwords / PII written to logs anywhere in committed code ✅ (uses SanitizedSummary length-only format).

## 20.Dependency + Secret Scan

**Reachable production CVE HIGH:** Microsoft.Extensions.Caching.Memory 8.0.0 (GHSA-qj66-m88j-hmgj, HIGH) transitive in Domain+Application publish DLL → FAUD-0009 HIGH release-blocking. Fix = direct pkg ref 8.0.5.
Test-only CVE HIGH: System.Text.Json 8.0.0 in Integration+Architecture tests only, NOT published → acceptable no prod impact.
CRITICAL CVEs: 0.
Secret scan result: 0 real credentials committed. All connection strings Windows Integrated/Trusted auth. No passwords / API keys / tokens / private keys / SMTP creds in source files ✅.
Production seed guard: 3 layers deep (startup flag validator early throw + ProductionSeedGuard + DemoDataSeeder.IsProduction() early return) → blocks DEV/E2E user accounts being seeded in Production ✅. FAUD-0006 swallows startup exceptions → if bypass attempted, actually hardened layers still engaged.
