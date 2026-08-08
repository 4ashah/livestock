@echo off
setlocal EnableDelayedExpansion
echo === LivestockManager: Restore Database ===
echo WARNING: This operation can overwrite the target database destructively.
echo          Set CONFIRM_RESTORE_OVERWRITE=1 to permit overwriting an existing database.
if not defined SQL_SERVER set "SQL_SERVER=."
if not defined SQL_DATABASE set "SQL_DATABASE=LivestockManager"
if not defined BACKUP_FILE_PATH (
  echo ERROR: BACKUP_FILE_PATH env var must point to a .bak file.
  exit /b 2
)

set "CONFIRM_SWITCH="
if defined CONFIRM_RESTORE_OVERWRITE (
  if /i "!CONFIRM_RESTORE_OVERWRITE!"=="1" set "CONFIRM_SWITCH=-ConfirmDestructiveOverwrite"
  if /i "!CONFIRM_RESTORE_OVERWRITE!"=="true" set "CONFIRM_SWITCH=-ConfirmDestructiveOverwrite"
)

set "DATA_DIR_ARG="
if defined DATA_DIR (
  set "DATA_DIR_ARG=-DataFileDirectory "!DATA_DIR!""
)

set "LOG_DIR=%~dp0artifacts\logs"
set "PS_ARGS=-NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Restore-Database.ps1" -ServerInstance "%SQL_SERVER%" -BackupFile "%BACKUP_FILE_PATH%" -TargetDatabase "%SQL_DATABASE%" !DATA_DIR_ARG! !CONFIRM_SWITCH!"

call PowerShell %PS_ARGS%
exit /b %ERRORLEVEL%
