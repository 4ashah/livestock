# DATABASE

## Identity Role Model
Active identity roles in current runtime model:
- `DataEntry`
- `FarmManager`
- `Accounts`
- `OperationsManager`
- `CompanyAdministrator`
- `SystemAdministrator`

`Viewer` is not part of the active seed list.

## Existing Database Remediation
Existing deployments may still contain historical `Viewer` rows. Startup remediation:
- migrates Viewer-only users to disabled `DataEntry`
- removes Viewer from users who already hold another valid role
- deletes the Viewer role after assignments reach zero
- writes audit entries for the retirement action

## Seed Expectation
New development/testing databases create only the active six roles.
