# DATABASE SCHEMAS, MIGRATIONS & CONCURRENCY FINDINGS

**Audit Date**: August 7, 2026  
**Target Database**: SQL Server / EF Core 8  
**Audit Database Instance**: `LivestockManager_Audit`  

---

## Schema & Migration Inspection

The solution contains 2 EF Core migrations:
1. `20260806191024_InitialMvp` (21 tables: Companies, Farms, Customers, Livestock, Weights, Activities, Sales, Items, Invoices, Items, Payments, SequenceCounters, AuditLogs, AspNet*)
2. `20260807133025_Phase2Entities` (7 tables added: Suppliers, Purchases, PurchaseItems, Expenses, Receipts, InvoiceAdditionalCharges, Documents)

### Database Verification Summary
- **Migration Application**: Applied cleanly against `LivestockManager_Audit` database using `dotnet ef database update`. Total 28 base tables created.
- **Foreign Key Constraints**: FK relationships defined with explicit delete behavior (`CASCADE` for dependent line items, `RESTRICT`/`NO ACTION` for core financial references).
- **Precision & Data Types**:
  - Currency/Money columns: `decimal(18,2)`.
  - Weight columns: `decimal(18,4)`.
  - Tax/Discount Percentages: `decimal(5,2)`.
- **Soft-Delete Filters**: Query filters `HasQueryFilter(e => !e.IsDeleted)` defined across domain entities.
- **UTC Timestamps**: `CreatedAt` and `ModifiedAt` configured as `datetimeoffset(7)` with `sysdatetimeoffset()` defaults.

---

## Critical & High Database Findings

### 1. Document Numbering Sequence Generator Non-Compliance (DEF-004)

#### Issue Description
The system specification (`docs/DATABASE.md` and audit criteria) mandates year-scoped sequential document numbers:
- `PUR-YYYY-NNNNN`
- `PAY-YYYY-NNNNN`
- `INV-YYYY-NNNNN`
- `RCP-YYYY-NNNNN`

#### Code Analysis (`EfSequenceGenerator.cs`)
```csharp
public async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
{
    var company = await _dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
    var prefix = string.IsNullOrWhiteSpace(company.InvoicePrefix) ? "INV" : company.InvoicePrefix;
    var lastValue = await GenerateNumberAsync(companyId, prefix);
    return prefix + lastValue.ToString("D5");
}
```
- **Finding**: Output is `INV00001`, `INV00002` (missing year scoping and hyphens).
- **Workaround Exposure in `PurchaseService.cs`**:
  To achieve `PUR-YYYY-NNNNN`, `PurchaseService` passed `$"PUR:{year}"` as a custom prefix to `GenerateDocumentNumberAsync`, then manually parsed out numbers using string extraction methods. `InvoiceService` and `ReceiptService` did not implement this workaround, generating invalid sequence strings `INV00001` and `RCT00001`.

---

### 2. Unhandled `SqlException` Under High Concurrency in `EfSequenceGenerator` (DEF-005)

#### Issue Description
`EfSequenceGenerator` executes raw SQL commands against `SequenceCounters` using `ROWLOCK, UPDLOCK, HOLDLOCK` table hints:
```sql
UPDATE SequenceCounters WITH (ROWLOCK, UPDLOCK, HOLDLOCK)
SET LastValue = LastValue + 1, LastUpdatedAt = GETUTCDATE()
OUTPUT INSERTED.LastValue
WHERE CompanyId = @CompanyId AND Prefix = @Prefix;
```
If no counter row exists for a prefix, `ExecuteScalarAsync` returns 0, and the generator executes an `INSERT INTO SequenceCounters`.

#### Concurrency Failure Mechanics
- When two concurrent requests simultaneously execute sequence generation for a new prefix:
  1. Both execute `UPDATE` -> 0 rows updated.
  2. Both attempt `INSERT INTO SequenceCounters (CompanyId, Prefix, ...)`.
  3. One transaction succeeds; the second transaction fails with SQL Server Error 2627 (Primary Key / Unique Constraint Violation).
  4. ADO.NET throws `Microsoft.Data.SqlClient.SqlException`.
- **Catch Block Analysis** (`EfSequenceGenerator.cs` L139-L150):
  ```csharp
  catch (DbUpdateConcurrencyException) { ... }
  catch (DbUpdateException) { ... }
  ```
- Because raw `DbCommand` bypasses EF Core, ADO.NET throws `SqlException`, which is NOT derived from `DbUpdateConcurrencyException` or `DbUpdateException`.
- **Result**: The exception escapes the retry loop and crashes the calling HTTP request.

---

### 3. Concurrency Protection & Soft-Delete Cascades

- **RowVersion Tokens**: Domain entities declare `byte[] Version` rowversion concurrency tokens. EF Core entity configurations define `.IsRowVersion()`. Concurrency conflicts raise `DbUpdateConcurrencyException`.
- **Soft-Delete Interception**: `AppDbContext` overrides `SaveChangesAsync`, intercepting deleted entries and setting `State = Modified`, `IsDeleted = true`.
- **Audit Log Pipeline Ordering Fix**: Verification confirmed fix `D-007` captures audit log action state (`Create`/`Update`/`Delete`) BEFORE soft-delete handlers transform `State` from `Deleted` to `Modified`.

---

## Controlled Concurrency Test Results against `LivestockManager_Audit`

1. **Sequential Livestock ID Generation**: 100 sequential registration requests executed against `LivestockManager_Audit` for Company 1 generated contiguous IDs `Ah00001` through `Ah00100` without gaps or duplicates.
2. **Duplicate Tag/Identifier Protection**: Unique index `UK_Livestock_CompanyId_LivestockId_IsDeleted` on `(CompanyId, LivestockId, IsDeleted)` successfully blocked duplicate inserts when duplicate IDs were forced.
3. **Sequence Counter Deadlock Resilience**: Raw concurrent inserts without pre-existing counter rows raised `SqlException 2627`, verifying DEF-005.
