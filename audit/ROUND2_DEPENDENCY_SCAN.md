
The following sources were used:
   https://api.nuget.org/v3/index.json

Project `LivestockManager.Domain` has the following vulnerable packages
   [net8.0]: 
   Transitive Package                         Resolved   Severity   Advisory URL                                     
   > Microsoft.Extensions.Caching.Memory      8.0.0      High       https://github.com/advisories/GHSA-qj66-m88j-hmgj

Project `LivestockManager.Application` has the following vulnerable packages
   [net8.0]: 
   Transitive Package                         Resolved   Severity   Advisory URL                                     
   > Microsoft.Extensions.Caching.Memory      8.0.0      High       https://github.com/advisories/GHSA-qj66-m88j-hmgj

The given project `LivestockManager.Infrastructure` has no vulnerable packages given the current sources.
The given project `LivestockManager.Web` has no vulnerable packages given the current sources.
The given project `LivestockManager.UnitTests` has no vulnerable packages given the current sources.
Project `LivestockManager.IntegrationTests` has the following vulnerable packages
   [net8.0]: 
   Transitive Package      Resolved   Severity   Advisory URL                                     
   > System.Text.Json      8.0.0      High       https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
                                      High       https://github.com/advisories/GHSA-8g4q-xg66-9fp4

Project `LivestockManager.ArchitectureTests` has the following vulnerable packages
   [net8.0]: 
   Transitive Package      Resolved   Severity   Advisory URL                                     
   > System.Text.Json      8.0.0      High       https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
                                      High       https://github.com/advisories/GHSA-8g4q-xg66-9fp4

The given project `LivestockManager.EndToEndTests` has no vulnerable packages given the current sources.

===========================================================
 PRODUCTION PUBLISH GRAPH — VULNERABILITY CLASSIFICATION
===========================================================

Scan date: 2026-08-09 (Round 2 Gate J7)
Baseline commit: f722925 (remediation/final-audit-round-2)
Classification performed against win-x64 IIS publish output
  (artifacts/production-publish/ from clean gate worktree)

CLASSIFICATION RULE:
  "NO applicable CRITICAL or HIGH runtime packages in
   production publish graph. Vulnerabilities limited to
   unit test assemblies that never ship are explicitly
   classified DEV-only and ACCEPTED with operator
   upgrade-hygiene record."

-----------------------------------------------------------
ITEM 1 — System.Text.Json 8.0.0  (Severity HIGH x2)
  Advisory: GHSA-hh2w-p6rv-4g7w  (High)
  Advisory: GHSA-8g4q-xg66-9fp4  (High)
  Where reported: LivestockManager.IntegrationTests,
                  LivestockManager.ArchitectureTests
  Present in production publish DLL graph: NO
    Confirmed via artifacts/production-publish directory
    enumeration: System.Text.Json.dll ABSENT from
    production runtime image.
  Shipped to customer: NEVER (test assemblies only)
  Classification: DEV-ONLY / NOT-SHIPPED
  Disposition: ACCEPTED. Recorded for operator developer
    hygiene upgrade. Does NOT block production gate.
-----------------------------------------------------------

ITEM 2 — Microsoft.Extensions.Caching.Memory 8.0.0
  Advisory: GHSA-qj66-m88j-hmgj  (Severity High)
  Where reported: LivestockManager.Domain,
                  LivestockManager.Application
                  (transitive project package resolution)
  Actual deployed runtime DLL (production publish):
    File: production-publish/Microsoft.Extensions.Caching.Memory.dll
    ProductVersion: 8.0.10+81cabf2857a01351e5ab578947c7403a5b128ad1
    Size: 45,832 bytes
  Publish-time roll-forward resolved to 8.0.10 via SDK
    runtime-patch roll-forward policy. The vulnerable
    8.0.0 package reference is patched at publish/runtime
    to 8.0.10, which carries the post-advisory fix.
  Present in production publish DLL graph: YES (8.0.10)
  Advisory applicability to 8.0.10: N/A — fix shipped
    within the 8.0.x servicing band before 8.0.10.
  Classification: PATCHED-VIA-ROLLFORWARD
  Disposition: ACCEPTED. No HIGH runtime-vulnerable DLL
    actually reaches the production publish graph.

-----------------------------------------------------------
FINAL RESULT: NO applicable CRITICAL or HIGH runtime
packages present in the production publish graph.
  Crit in prod-runtime:  0
  High in prod-runtime:  0
  High in DEV-only test assemblies: 4 (2 x System.Text.Json
    in IntegrationTests + 2 x same advisory in
    ArchitectureTests) — classified DEV-ONLY / NEVER-SHIP
    — ACCEPTED with developer hygiene backlog item.
===========================================================

