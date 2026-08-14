# Defects

## FAD-001 — High — Dependency / Security

High transitive runtime advisories were reported for `Microsoft.Extensions.Caching.Memory` 8.0.0 (GHSA-qj66-m88j-hmgj) and `System.Text.Json` 8.0.0 (GHSA-hh2w-p6rv-4g7w, GHSA-8g4q-xg66-9fp4). Reproduce with `dotnet list LivestockManager.sln package --vulnerable --include-transitive`. Upgrade/remediate and rerun the scan.

## FAD-002 — High — Purchase workflow / Test integrity

`PurchaseServiceTests.PostPurchase_CreatesLivestockIntakeIdempotently` and `PostPurchase_LivestockInitialWeightLinkedIfProvided` fail because `PurchaseService.PostPurchaseAsync` throws that Purchase Invoices cannot create livestock automatically. Reconcile requirement, implementation, and tests; add an executed regression test.

## FAD-003 — High — E2E authentication / core workflows

The prescribed `Run-E2ETests.ps1` run discovered 39 tests: 21 passed, 18 failed, 0 skipped. All six desktop seeded-role login checks and SysAdmin mobile login checks failed, blocking page, PDF, and authorization workflows. Reproduce with the runner; correct the seed/login contract and rerun all 39.

## FAD-004 — High — Production package hygiene

The clean publish contains `appsettings.Development.json`. Remove development-only settings from the IIS release artifact and add a package-content regression test.

## FAD-005 — Medium — EF model warnings

EF Core logs optional owned Address/table-sharing warnings for Company, Customer, and Farm. Define an identifying required property or make the owned navigation required as appropriate; verify migration/model behavior.
