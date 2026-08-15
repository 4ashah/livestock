# IMPLEMENTED FEATURES

## Current Authorization Model
- Active runtime roles: `DataEntry`, `FarmManager`, `Accounts`, `OperationsManager`, `CompanyAdministrator`, `SystemAdministrator`
- Display names: Employee, Farm Manager, Accounting, Manager, Admin, System Admin
- `Viewer` retired from active runtime source, UI selectors, navigation, and seed data

## Current Security Behavior
- Centralized role constants in `RoleNames`
- Centralized authorization constants in `PolicyNames`
- Server-side role-assignment validation in user administration
- Company/farm scope preserved through endpoint checks and scoped queries
- Controlled startup retirement logic for existing databases that still contain Viewer assignments

## Current Navigation Behavior
- Desktop and mobile navigation are policy-driven
- Employee sees useful operational pages only
- Financial pages are shown only when the final policy matrix allows them
- `System Admin` role option is visible only to SystemAdministrator

## Current Identity / Seed Behavior
- New demo/dev databases seed only the six active roles
- Existing databases can retire Viewer safely through the controlled remediation path
- No new Viewer users are created in Development, Testing, or E2E seed data
