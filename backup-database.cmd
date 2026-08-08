@echo off
setlocal
if not defined SQL_SERVER set "SQL_SERVER=."
if not defined SQL_DATABASE set "SQL_DATABASE=LivestockManager"
if not defined BACKUP_DIR set "BACKUP_DIR=%~dp0artifacts\backups"
set LOG_DIR=%~dp0artifacts\logs

set "PS_ARGS=-NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Backup-Database.ps1" -ServerInstance "%SQL_SERVER%" -DatabaseName "%SQL_DATABASE%" -BackupDirectory "%BACKUP_DIR%" -LogDirectory "%LOG_DIR%""
if defined RETENTION_DAYS (
  set "PS_ARGS=%PS_ARGS% -RetentionDays %RETENTION_DAYS%"
)

call PowerShell %PS_ARGS%
exit /b %ERRORLEVEL%
