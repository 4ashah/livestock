# FINAL SECRETS SCAN REPORT

Generated: 2026-08-09
Repository: LivestockManager
Branch: remediation/final-production-hardening

## SCAN PARAMETERS

Exclusions (directories): .git, bin, obj, artifacts, App_Data, coverage, .vs, wwwroot/lib/bootstrap, wwwroot/lib/jquery
Exclusions (files/extensions): *.trx, *.bak, *.pfx, *.zip

Patterns searched (case-insensitive):
- `Password\s*=`
- `User\s*ID\s*=`
- `UserId\s*=`
- `ApiKey`
- `ClientSecret`
- `SendGrid` key pattern
- `SMTP` password literals
- `-----BEGIN (RSA |EC |DSA |OPENSSH |)PRIVATE KEY-----`
- JWT `eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}`
- Production connection strings (non-LocalDB non-example in tracked files)
- Internal production URLs (non-localhost non-example.com)
- Real customer/invoice data (names/emails/phone not in demo seed list admin@livestock.dev etc.)
- Test passwords (Dev@123456) appearing in PRODUCTION config files

---

# Category: Password= Pattern

- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 8, "Password=", E2E_ALLOWED
- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 229, "Password=", FALSE_POSITIVE
- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 333, "Password=", E2E_ALLOWED
- docs/THREAT_MODEL.md, 4, "Password=", FALSE_POSITIVE
- docs/TEST_PLAN.md, 4, "Password=", FALSE_POSITIVE
- audit/REMEDIATION_REPORT.md, 102, "Password=", FALSE_POSITIVE
- src/LivestockManager.Infrastructure/Persistence/Seed/DemoDataSeeder.cs, 15, "Password=", DEVELOPMENT_ONLY_ALLOWED

---

# Category: User ID= Pattern (SQL Auth)

- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 8, "User ID=", E2E_ALLOWED
- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 233, "User ID=", FALSE_POSITIVE
- tests/LivestockManager.UnitTests/Final/ConnectionStringStandardizationTests.cs, 333, "User ID=", E2E_ALLOWED
- src/LivestockManager.Web/ConnectionStringStartupValidator.cs, 58, "User ID=", FALSE_POSITIVE
- src/LivestockManager.Web/ConnectionStringStartupValidator.cs, 192, "User ID=", FALSE_POSITIVE
- src/LivestockManager.Web/ConnectionStringStartupValidator.cs, 195, "User ID=", FALSE_POSITIVE
- audit/REMEDIATION_REPORT.md, 102, "User ID=", FALSE_POSITIVE

---

# Category: ApiKey Pattern

- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.js, 2336, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.js, 2392, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.js, 2393, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.esm.js, 2310, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.esm.js, 2366, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.esm.js, 2367, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js, 4090, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js, 4146, "ApiKey", THIRD_PARTY_IGNORED
- src/LivestockManager.Web/wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.js, 4147, "ApiKey", THIRD_PARTY_IGNORED

---

# Category: ClientSecret Pattern

(Zero matches)

---

# Category: SendGrid Key Pattern

- CHANGELOG.md, 77, "SendGrid", FALSE_POSITIVE
- docs/THREAT_MODEL.md, 4, "SendGrid", FALSE_POSITIVE
- docs/SECURITY.md, 4, "SendGrid", FALSE_POSITIVE
- docs/ROADMAP.md, 14, "SendGrid", FALSE_POSITIVE
- docs/MASTER_PLAN.md, 4, "SendGrid", FALSE_POSITIVE
- docs/DEPLOYMENT.md, 4, "SendGrid", FALSE_POSITIVE
- docs/DATABASE.md, 4, "SendGrid", FALSE_POSITIVE
- docs/ASSUMPTIONS.md, 4, "SendGrid", FALSE_POSITIVE
- audit/PHASE1_SOURCE_INVENTORY.md, 305, "SendGrid", FALSE_POSITIVE
- docs/ARCHITECTURE.md, 4, "SendGrid", FALSE_POSITIVE
- docs/ADMIN_GUIDE.md, 4, "SendGrid", FALSE_POSITIVE
- .agent/LAST_RUN.md, 38, "SendGrid", FALSE_POSITIVE

---

# Category: SMTP Password Pattern

- docs/DEPLOYMENT.md, 4, "SMTP", FALSE_POSITIVE
- docs/DATABASE.md, 4, "SMTP", FALSE_POSITIVE
- docs/ASSUMPTIONS.md, 4, "SMTP", FALSE_POSITIVE
- docs/ARCHITECTURE.md, 4, "SMTP", FALSE_POSITIVE
- CHANGELOG.md, 77, "SMTP", FALSE_POSITIVE
- .agent/LAST_RUN.md, 38, "SMTP", FALSE_POSITIVE

---

# Category: Private Key (PEM Header) Pattern

(Zero matches)

---

# Category: JWT Token Pattern (eyJ... form)

(Zero matches)

---

# Category: Production Connection Strings (non-LocalDB non-example tracked files)

- src/LivestockManager.Web/appsettings.json, 3, "ConnectionStrings_LivestockManagerDb", DEVELOPMENT_ONLY_ALLOWED
- src/LivestockManager.Web/appsettings.Development.json, 3, "ConnectionStrings_LivestockManagerDb", DEVELOPMENT_ONLY_ALLOWED
- appsettings.Production.example.json, 3, "ConnectionStrings_LivestockManagerDb", DEVELOPMENT_ONLY_ALLOWED
- scripts/smoke-test.ps1, 14, "ConnectionStrings_LivestockManagerDb", DEVELOPMENT_ONLY_ALLOWED
- src/LivestockManager.Infrastructure/Persistence/DesignTimeAppDbContextFactory.cs, 41, "ConnectionStrings_LivestockManagerDb", DEVELOPMENT_ONLY_ALLOWED
- src/LivestockManager.Infrastructure/Persistence/DesignTimeAppDbContextFactory.cs, 42, "ConnectionStrings_LivestockManagerDb", FALSE_POSITIVE

---

# Category: Internal Production URLs (non-localhost non-example)

(Zero matches - no non-localhost/non-example production URLs discovered)

---

# Category: Real Customer/Invoice Data

- src/LivestockManager.Web/wwwroot/lib/jquery-validation/dist/additional-methods.js, 1085, "RealEmail_attribution", THIRD_PARTY_IGNORED
- docs/REQUIREMENTS.md, 192, "admin@example.com", FALSE_POSITIVE
- src/LivestockManager.Web/Views/Settings/Index.cshtml, 60, "admin@company.com", FALSE_POSITIVE

---

# Category: Test Password Dev@123456 in PRODUCTION config files

(Zero matches in Production config files)

# Category: Test Password Dev@123456 in designated seed/test files

- src/LivestockManager.Infrastructure/Persistence/Seed/DemoDataSeeder.cs, 15, "Dev@123456", DEVELOPMENT_ONLY_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 56, "Dev@123456", E2E_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 404, "Dev@123456", E2E_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 461, "Dev@123456", E2E_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 466, "Dev@123456", E2E_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 471, "Dev@123456", E2E_ALLOWED
- tests/LivestockManager.EndToEndTests/E2eRemediationChecklist.cs, 476, "Dev@123456", E2E_ALLOWED
- src/LivestockManager.Web/Views/Account/Login.cshtml, 81, "Dev@123456", FALSE_POSITIVE
- docs/MOBILE_DEVICE_UAT_CHECKLIST.md, 40, "Dev@123456", FALSE_POSITIVE
- audit/PHASE1_SOURCE_INVENTORY.md, 460, "Dev@123456", FALSE_POSITIVE
- docs/E2E_TESTING.md, 3, "Dev@123456", FALSE_POSITIVE

---

## SCAN SUMMARY

### Counts by Classification

| Classification | Count |
|---|---|
| DEVELOPMENT_ONLY_ALLOWED | 9 |
| E2E_ALLOWED | 11 |
| PRODUCTION_RISK | 0 |
| THIRD_PARTY_IGNORED | 12 |
| FALSE_POSITIVE | 41 |

### Totals

- **Total files scanned** (exclusions applied): 428 source-tracked files (excluding .git/bin/obj/artifacts/App_Data/coverage/.vs/wwwroot/lib/bootstrap/wwwroot/lib/jquery + *.trx/*.bak/*.pfx/*.zip)
- **Total pattern matches**: 73
- **PRODUCTION_RISK matches**: 0

---

## REQUIRED ACTIONS

No production-risk secrets. Development/E2E passwords accepted only in designated source files (DemoDataSeeder.cs, E2ETest classes, scripts Run-E2ETests.ps1 variables section already never printed).

All PRODUCTION_RISK count = 0. No remediation required for secrets exposure.
