# AUTOMATED TEST RESULTS & COVERAGE GAP AUDIT

**Audit Date**: August 7, 2026  
**Git Checkpoint**: `ef36f34`  
**Test Runner**: xUnit.net VSTest Adapter v2.5.3.1 (.NET 8.0)  

---

## Executive Test Summary

| Test Project | Reported Count | Executed Count | Passed | Failed | Skipped | Actual Assertion Quality |
|---|---|---|---|---|---|---|
| `LivestockManager.UnitTests` | 175 | 175 | 175 | 0 | 0 | **High**: Real domain, service, and validation rules tested. |
| `LivestockManager.IntegrationTests` | 1 | 1 | 1 | 0 | 0 | **INVALID**: Empty stub method (`public void Test1() {}`), 0 assertions. |
| `LivestockManager.ArchitectureTests` | 1 | 1 | 1 | 0 | 0 | **INVALID**: Empty stub method (`public void Test1() {}`), 0 assertions. |
| `LivestockManager.EndToEndTests` | 1 | 1 | 1 | 0 | 0 | **INVALID**: Empty stub method (`public void Test1() {}`), 0 assertions. |
| **TOTAL** | **178** | **178** | **178** | **0** | **0** | **175 Real / 3 Invalid Empty Stubs** |

---

## Detailed Test Verification Logs

### 1. `LivestockManager.UnitTests.csproj`
- **Command Executed**: `dotnet test tests\LivestockManager.UnitTests\LivestockManager.UnitTests.csproj -c Release --no-build --logger "console;verbosity=normal"`
- **Result**: **175 Passed, 0 Failed, 0 Skipped** (Execution time: 2.40s)
- **Breakdown by Module**:
  - `Roles.RoleNamesTests`: 5 tests pass (Role constants, spelling, permission ordering).
  - `RoleAuthorizationMatrixTests`: 5 tests pass (Auth attributes & role matrix).
  - `LivestockDomainTests`: 10 tests pass (Sequential IDs, registration rules, purchase amount rules).
  - `DischargeLogicTests`: 8 tests pass (5 discharge conditions, double-discharge prevention, P&L calc).
  - `WeightValidationTests`: 6 tests pass (Weight history, chronological order, non-zero weight).
  - `InvoiceCalculationTests`: 8 tests pass (Subtotal, discount, tax, charges, grand total calculations).
  - `Finance.PaymentReversalTests`: 5 tests pass (Partial/full payment, overpayment exception, reversal).
  - `Finance.ReceiptAndBalanceTests`: 8 tests pass (Receipt generation, customer balance, 5 aging buckets).
  - `Expenses.ExpenseServiceTests`: 8 tests pass (Expense creation, tax rate clamping, category filtering, CSV export).
  - `Purchases.PurchaseServiceTests`: 14 tests pass (Purchase draft, line items, totals, posting, livestock intake idempotency, void rules).
  - `Reports.DashboardAndProfitabilityTests`: 8 tests pass (14 KPI card DTO shape, farm & livestock profitability, mortality rate, CSV export).
  - `Storage.ProtectedDocumentStorageTests`: 5 tests pass (File upload, magic byte verification, extension validation, path traversal guard, cross-company leakage guard).
  - `CsvExportTests`: 4 tests pass (RFC4180 escaping, UTF-8 BOM byte header).
  - `Final.Phase2SmokeTests`: 10 tests pass (DbSet existence, migration presence, DI registration, enum values).

---

### 2. `LivestockManager.IntegrationTests.csproj`
- **Command Executed**: `dotnet test tests\LivestockManager.IntegrationTests\LivestockManager.IntegrationTests.csproj -c Release --no-build --logger "console;verbosity=normal"`
- **Result**: **1 Passed, 0 Failed, 0 Skipped** (Execution time: 0.67s)
- **Defect Identified**: The single test `UnitTest1.Test1` contains **NO ASSERTIONS AND NO TEST CODE**.
- **File Source Code** (`tests/LivestockManager.IntegrationTests/UnitTest1.cs`):
```csharp
namespace LivestockManager.IntegrationTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
```
- **Audit Assessment**: **CRITICAL COVERAGE GAP**. Zero real database integration tests exist.

---

### 3. `LivestockManager.ArchitectureTests.csproj`
- **Command Executed**: `dotnet test tests\LivestockManager.ArchitectureTests\LivestockManager.ArchitectureTests.csproj -c Release --no-build --logger "console;verbosity=normal"`
- **Result**: **1 Passed, 0 Failed, 0 Skipped** (Execution time: 0.66s)
- **Defect Identified**: The single test `UnitTest1.Test1` is an empty stub.
- **File Source Code** (`tests/LivestockManager.ArchitectureTests/UnitTest1.cs`):
```csharp
namespace LivestockManager.ArchitectureTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
```
- **Audit Assessment**: **HIGH COVERAGE GAP**. Zero Clean Architecture dependency rules (e.g. Domain layer has no dependencies on Infrastructure/Web) are verified by automated tests.

---

### 4. `LivestockManager.EndToEndTests.csproj`
- **Command Executed**: `dotnet test tests\LivestockManager.EndToEndTests\LivestockManager.EndToEndTests.csproj -c Release --no-build --logger "console;verbosity=normal"`
- **Result**: **1 Passed, 0 Failed, 0 Skipped** (Execution time: 0.71s)
- **Defect Identified**: The single test `UnitTest1.Test1` is an empty stub.
- **File Source Code** (`tests/LivestockManager.EndToEndTests/UnitTest1.cs`):
```csharp
namespace LivestockManager.EndToEndTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
```
- **Audit Assessment**: **HIGH COVERAGE GAP**. Zero end-to-end browser user flows (Login -> Register Animal -> Create Sale -> Confirm Invoice -> Record Payment -> Download PDF) are executed by automated tests.

---

## Test Coverage Gap Audit & Risk Classification

| Uncovered / Under-Tested Module | Indirect Coverage | Direct Coverage | Risk Level | High-Value Test Cases Required |
|---|---|---|---|---|
| **Supplier CRUD & Index** (`SuppliersController`, `SupplierService`) | None | 0 tests | **Critical** | Test supplier creation, duplicate supplier code validation, soft-delete archive, company isolation. |
| **Audit Log UI** (`AuditController`) | None | 0 tests | **High** | Test GET `/audit` with pagination, entity filtering, date ranges, and non-admin access rejection (`403`). |
| **Settings UI** (`SettingsController`) | None | 0 tests | **High** | Test GET/POST `/settings` allow-list validation, non-admin rejection, tax rate validation, and cross-company edit block. |
| **Sequential Numbering Rollover** (`EfSequenceGenerator`) | Partial | 1 test (Prefixes only) | **High** | Test year rollover (e.g. 2026 -> 2027), concurrency locking, and `SqlException` retry loop. |
| **Custom PDF Layout & Writer** (`FormattedPdfWriter`) | Stub check | 1 test (Interface check) | **High** | Test multi-page invoice layout, table row wrapping, Unicode string escaping, and totals alignment. |
| **EF Core Mappings & FK Constraints** (`EntityTypeConfigurations`) | Migration test | 1 test (DbSets exist) | **Medium** | Test FK cascade/delete behaviors, unique filtered indexes (`IsDeleted = 0`), decimal precision limits. |
| **DI Registration & Service Lifetimes** (`ServiceCollectionExtensions`) | Smoke test | 1 test (Resolve services) | **Medium** | Test service lifetime consistency (Transient/Scoped/Singleton) to prevent captive dependencies. |
| **Deployment & Recovery Scripts** (`.cmd` / `.ps1` scripts) | None | 0 tests | **High** | Test `backup-database.cmd` output pathing and `restore-database.cmd` overwrite safeguards against test DB. |
