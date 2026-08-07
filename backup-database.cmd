@echo off
setlocal EnableDelayedExpansion
echo === LivestockManager: Backup Database (sqlcmd) ===
if not defined SQL_SERVER set "SQL_SERVER=.\SQLEXPRESS"
if not defined SQL_DATABASE set "SQL_DATABASE=LivestockManager"
if not defined BACKUP_DIR set "BACKUP_DIR=%~dp0artifacts\backups"
where sqlcmd >nul 2>nul
if errorlevel 1 (echo ERROR: sqlcmd not found on PATH. Install SQL Server Command Line Utilities. & exit /b 1)
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%" >nul 2>nul
for /f "tokens=2 delims==" %%a in ('wmic os get localdatetime /value ^| findstr "="') do set "DT=%%a"
set "STAMP=%DT:~0,8%-%DT:~8,6%"
set "BACKUP_FILE=%BACKUP_DIR%\%SQL_DATABASE%-%STAMP%.bak"
echo SQL_SERVER=%SQL_SERVER%
echo SQL_DATABASE=%SQL_DATABASE%
echo BACKUP_FILE=%BACKUP_FILE%
sqlcmd -S "%SQL_SERVER%" -E -Q "BACKUP DATABASE [%SQL_DATABASE%] TO DISK='%BACKUP_FILE%' WITH INIT, COMPRESSION, STATS=10;" -b
if errorlevel 1 (echo BACKUP FAILED & exit /b 1)
echo BACKUP OK: %BACKUP_FILE%
if defined RETENTION_DAYS (
  echo Purging backups older than %RETENTION_DAYS% days...
  forfiles /p "%BACKUP_DIR%" /s /m *.bak /d -%RETENTION_DAYS% /c "cmd /c del @path" 2>nul
)
exit /b 0
