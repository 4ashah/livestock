# REQUIREMENTS

## Final Six-Role Authorization Requirements
The application must expose exactly these six active business roles:
- `DataEntry` -> Employee
- `FarmManager` -> Farm Manager
- `Accounts` -> Accounting
- `OperationsManager` -> Manager
- `CompanyAdministrator` -> Admin
- `SystemAdministrator` -> System Admin

`Viewer` is retired from the active model. Historical references may remain only in clearly marked audit material.

## Role Requirements
### Employee / DataEntry
Must have:
- View farms and farm details
- View livestock and livestock details
- Register livestock
- Record newborns
- Add weight
- Add comments / activities
- View operational report: Active Livestock

Must not have:
- Farm create/edit/archive
- Edit existing livestock beyond approved entry actions
- Discharge or record loss
- Sales creation or reversal
- Purchase-invoice management
- Invoice management
- Payments or receipts
- Expense management
- Financial reports
- User management
- Settings, audit logs, or company management

### Farm Manager
Employee rights plus:
- Edit livestock in assigned farms
- Record purchases / intake
- Record newborns
- Discharge livestock
- Record losses
- Create sales
- View operational reports
- View/create suppliers needed for purchases
- View/create customers needed for sales where approved
- Upload operational documents

### Accounting
Must have:
- View/create/edit customers and suppliers
- Manage purchase invoices
- View/create/confirm sales according to policy
- Manage invoices and PDF downloads
- Record and reverse payments according to policy
- Generate and download receipts
- Manage expenses
- View financial reports and Profit & Loss
- View/upload authorized financial documents

Must not have:
- Farm management
- Company-user management
- Company management
- Global settings
- Cross-company access

### Manager / OperationsManager
Company-wide access to operational modules plus approved financial visibility, without cross-company authority.

### Admin / CompanyAdministrator
Full company-scoped operational and financial rights, company-user management, company settings, company audit logs, and authorized reversals.

### System Admin / SystemAdministrator
Cross-company access, company management, global user and role management, global settings, diagnostics, and full audit visibility.

## Migration Requirement
Before deleting Viewer from an existing database:
1. Query all Viewer assignments.
2. Remove Viewer from users who already have another valid role.
3. Migrate Viewer-only users to disabled `DataEntry` and add the required audit entry.
4. Delete the Viewer role only when no assignments remain.
5. Keep the remediation idempotent.

## Documentation Requirement
Current documentation must describe only the final six active roles. The original seven-role workbook/source may be preserved as historical input with a note that the Viewer column is retired.
