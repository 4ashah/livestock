# ADMIN GUIDE

## Active Roles
Administrators work with exactly six active roles:
- Employee (`DataEntry`)
- Farm Manager (`FarmManager`)
- Accounting (`Accounts`)
- Manager (`OperationsManager`)
- Admin (`CompanyAdministrator`)
- System Admin (`SystemAdministrator`)

`Viewer` is retired and must not be assigned.

## User Administration Rules
- `CompanyAdministrator` may assign only company-safe roles:
  - Employee
  - Farm Manager
  - Accounting
  - Manager
  - Admin
- `CompanyAdministrator` must not assign `SystemAdministrator`.
- `SystemAdministrator` may assign any active role.
- Server-side validation enforces these rules even if a client submits a manipulated role value.

## Viewer Retirement Administration
If an existing database still contains Viewer assignments:
- Viewer-plus-valid-role users lose Viewer and keep the valid role.
- Viewer-only users migrate to disabled Employee/DataEntry.
- The required audit note is written during migration.
- Administrators must review and re-enable migrated accounts before use.

## Scope Rules
- Company administrators manage only their own company.
- System administrators may work across companies.
- Company and farm isolation remain enforced by endpoint authorization and scoped queries.
