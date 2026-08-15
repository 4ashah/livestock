# TEST PLAN

## Authorization Matrix Coverage
Current verification targets these principals:
- Anonymous
- DataEntry / Employee
- FarmManager
- Accounts
- OperationsManager
- CompanyAdministrator
- SystemAdministrator

## Role-Retirement Checks
- Viewer absent from `RoleNames`
- Viewer absent from active policy definitions
- Viewer absent from active role selectors and navigation
- Viewer absent from new seed data
- Existing Viewer-only user migrates to disabled DataEntry
- Existing Viewer-plus-valid-role user retains valid role
- No user remains assigned to Viewer
- CompanyAdministrator cannot assign SystemAdministrator
- Manipulated role submission is rejected by the server

## Current Results
- Build: pass
- Unit: `324/324` pass
- Integration: `15/15` pass
- Architecture: `60/60` pass
- Playwright/E2E: pending in this pass
