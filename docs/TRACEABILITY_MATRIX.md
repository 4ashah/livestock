# TRACEABILITY MATRIX

## Final Six-Role Model Traceability

| Requirement | Source | Implementation |
|---|---|---|
| Exactly six active roles | User-approved final role model | `RoleNames.cs`, `Login.cshtml`, user-role selectors |
| Centralized policies | User-approved policy list | `PolicyNames.cs`, `Program.cs`, active controllers |
| No Viewer in active runtime model | User-approved retirement decision | `RoleNames.cs`, `DemoDataSeeder.cs`, navigation, role selectors |
| Safe migration of existing Viewer users | User-approved migration rules | `ViewerRoleRetirementService.cs`, startup execution in `Program.cs` |
| Company/farm isolation preserved | Existing security constraints + final scope rules | Company-scoped services/controllers, policy enforcement, audit checks |
| Employee read/basic-entry access | Final Employee rights | `Program.cs`, `LivestockController.cs`, `ReportsController.cs`, navigation |
| Farm Manager operational extension | Final FarmManager rights | `StockAdditionController.cs`, sales/loss/document/customer/supplier flows |
| Accounting financial workflows | Final Accounts rights | invoices, payments, receipts, expenses, reports controllers |
| CompanyAdministrator cannot assign SystemAdministrator | Final admin-assignment rule | `UserManagementController.cs`, `RoleNames.CompanySafeAssignable` |
| Desktop/mobile navigation reflects policies | Final navigation rule | `_Layout.cshtml`, `_MobileLayout.cshtml` |
| Viewer absent from new seed data | Final identity requirement | `DemoDataSeeder.cs`, test seeds |
| Viewer removed from active test matrix | Final test requirement | unit/integration test updates and current verification pass |

## Verification Trace
- Build: `dotnet build LivestockManager.sln -c Release` -> pass
- Unit: `324/324` pass
- Integration: `15/15` pass
- Architecture: `60/60` pass
- Development DB retirement evidence: Viewer assignment count `1 -> 0`

## Historical Note
The original seven-role source included `Viewer`. The current active implementation does not.
