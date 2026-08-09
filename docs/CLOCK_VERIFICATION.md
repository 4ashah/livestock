# Mandatory Machine Clock Verification
Before deploy or first-run, run (PowerShell admin or deployment script):
1. Get-Date -Format o
2. w32tm /query /status
3. sqlcmd -S "." -d master -Q "SET NOCOUNT ON; SELECT SYSDATETIMEOFFSET() AS SQLOffset, SYSUTCDATETIME() AS SQLUtc;" -s "|" -W -h -1
Expected: server local clock within ±5s of UTC reference. DO NOT deploy with stale or future-dated clock (August 2026 artifacts were generated on an out-of-sync machine).
Audit timestamps use UTC; company/user display timezone via IDateTime/UtcNow; sequence year uses document business date rule; backup timestamps server-local.
Document numbers once issued must NOT be renumbered by clock correction.
