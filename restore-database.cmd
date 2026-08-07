@echo off
setlocal
echo === LivestockManager: Restore Database (sqlcmd) ===
echo WARNING: This overwrites the target database. Set CONFIRM_RESTORE_OVERWRITE=1 to proceed.
if /i not "%CONFIRM_RESTORE_OVERWRITE%"=="1" (echo ERROR: CONFIRM_RESTORE_OVERWRITE not set to 1. Aborting. & exit /b 2)
if not defined SQL_SERVER set "SQL_SERVER=.\SQLEXPRESS"
if not defined SQL_DATABASE set "SQL_DATABASE=LivestockManager"
if not defined BACKUP_FILE_PATH (echo ERROR: BACKUP_FILE_PATH env var must point to a .bak file. & exit /b 2)
if not exist "%BACKUP_FILE_PATH%" (echo ERROR: BACKUP_FILE_PATH not found: %BACKUP_FILE_PATH% & exit /b 2)
where sqlcmd >nul 2>nul
if errorlevel 1 (echo ERROR: sqlcmd not found on PATH. & exit /b 1)
echo SQL_SERVER=%SQL_SERVER%
echo SQL_DATABASE=%SQL_DATABASE%
echo BACKUP_FILE_PATH=%BACKUP_FILE_PATH%
sqlcmd -S "%SQL_SERVER%" -E -Q "ALTER DATABASE [%SQL_DATABASE%] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [%SQL_DATABASE%] FROM DISK='%BACKUP_FILE_PATH%' WITH REPLACE, RECOVERY, STATS=10; ALTER DATABASE [%SQL_DATABASE%] SET MULTI_USER;" -b
if errorlevel 1 (echo RESTORE FAILED & exit /b 1)
echo RESTORE OK: %SQL_DATABASE% restored from %BACKUP_FILE_PATH%
exit /b 0
