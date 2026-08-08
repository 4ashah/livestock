# RE-AUDIT SECURITY & MULTI-TENANCY RESULTS

**Re-Audit Date**: August 8, 2026  
**Target Commit**: `75933c8`  
**Security Focus Areas**: Multi-Tenant Data Isolation (DEF-002), Role Authorization Matrix (DEF-001)  

---

## 1. Multi-Tenant Company Isolation Verification (DEF-002 - CLOSED)

### Code Boundary & Interface Refactoring
All 23 domain service interfaces and implementations were updated to require `Guid companyId` on entity lookups, list queries, state changes, and document downloads:
- `IInvoiceService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `ISaleService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `ILivestockService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `ICustomerService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `ISupplierService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `IPurchaseService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `IExpenseService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `IReceiptService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`
- `IFarmService.GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)`

### Database Scoping Pattern
Service methods enforce company scoping in LINQ queries:
```csharp
var invoice = await _db.Invoices
    .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId, ct)
    ?? throw new DomainException("Invoice not found.");
```
If an authenticated user in Company A attempts to access an entity GUID belonging to Company B:
1. The LINQ query evaluates to `null`.
2. The service throws a generic `DomainException("X not found.")`.
3. The Web controller maps `DomainException` to HTTP `404 Not Found`.
4. **Result**: Cross-company access is blocked, and entity existence is not leaked to unauthorized tenants.

### Test Verification
Executing `CompanyIsolationTests.cs` (12 automated unit tests `A` through `L`):
- `A_CrossCompanyInvoice_GetById_FailsWithGenericNotFound` -> **PASSED**
- `B_CrossCompanyInvoice_Confirm_FailsWithGenericNotFound` -> **PASSED**
- `C_CrossCompanyInvoice_CancelOrVoid_FailsWithGenericNotFound` -> **PASSED**
- `D_CrossCompanyInvoice_DownloadPdf_FailsWithGenericNotFound` -> **PASSED**
- `E_CrossCompanyPayment_Post_FailsBecauseFiltersRejectForeignInvoice` -> **PASSED**
- `F_CrossCompanyReceipt_GenerateForPayment_FailsNotFound` -> **PASSED**
- `G_CrossCompany_DischargeLivestock_FailsGenericNotFound` -> **PASSED**
- `H_CrossCompany_CustomerAndSupplier_GetByIdAndDelete_FailGenericNotFound` -> **PASSED**
- `I_CrossCompany_PurchasePost_FailsGenericNotFound` -> **PASSED**
- `J_SystemAdministratorScope_Placeholder_PassthroughRespectsFilter` -> **PASSED**
- `K_ListMethods_ReturnOnlyScopedCompanyData` -> **PASSED**
- `L_FarmAndLivestockScopedAccess_HonorsCompanyIdForeignKey` -> **PASSED**

---

## 2. Authorization & Policy Enforcement Verification (DEF-001 - CLOSED)

### Centralized Policy Architecture (`Program.cs`)
`Program.cs` registers 7 fine-grained authorization policies mapping to the official 6 Phase-2 roles (`RoleNames.cs`):
1. `CanViewOperationalData`: `Viewer`, `DataEntry`, `FarmManager`, `Accounts`, `CompanyAdministrator`, `SystemAdministrator`
2. `CanManageLivestock`: `DataEntry`, `FarmManager`, `CompanyAdministrator`, `SystemAdministrator`
3. `CanManageSales`: `FarmManager`, `Accounts`, `CompanyAdministrator`, `SystemAdministrator`
4. `CanManageAccounting`: `Accounts`, `CompanyAdministrator`, `SystemAdministrator`
5. `CanManageCompany`: `CompanyAdministrator`, `SystemAdministrator`
6. `CanManageSystem`: `SystemAdministrator`
7. `CanViewFinancialData`: `Accounts`, `FarmManager`, `CompanyAdministrator`, `SystemAdministrator`

### Controller Re-Authorization
All 14 Web controllers (`SalesController`, `InvoicesController`, `PaymentsController`, `SuppliersController`, `LivestockController`, `FarmsController`, `CustomersController`, `ReportsController`, `AuditController`, `SettingsController`, etc.) were refactored to consume these authorization policies.
- Legacy string literals (`[Authorize(Roles = "Administrator,Manager")]`) were **100% removed**.
- Users with role `Accounts` can successfully perform accounting operations (invoice confirmation, payment posting, receipt generation, financial reporting).
- Users with role `FarmManager` can manage livestock, log weights, register discharges, and create sales.
- Anonymous requests to protected endpoints are challenged and redirected to `/Account/Login`.

---

## 3. Protected Document File Storage (VERIFIED SECURE)

- **Magic Byte Validation**: Uploads verify binary file headers (`JPEG`, `PNG`, `PDF`). Executables renamed with `.jpg` extension are rejected.
- **Path Traversal Protection**: Internal paths are normalized and verified (`fullPath.StartsWith(resolvedRoot)`).
- **Physical Isolation**: Documents are stored outside `wwwroot` in `./App_Data/ProtectedDocuments/{companyId}/{yyyy-MM}/{guid}.ext`.
