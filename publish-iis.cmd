@echo off
setlocal
echo === LivestockManager: Publish for IIS ===
echo Notes:
echo   * Install ASP.NET Core Hosting Bundle 8.x on the target server
echo   * AppPool .NET CLR version = No Managed Code
echo   * AppPool identity (IIS AppPool\LivestockAppPool) needs Modify ACL on App_Data
call "%~dp0package-release.cmd"
if errorlevel 1 (exit /b 1)
set "OUT_DIR=%~dp0artifacts\publish"
set "APP_DATA=%OUT_DIR%\App_Data"
if not exist "%APP_DATA%" mkdir "%APP_DATA%"
if not exist "%APP_DATA%\files" mkdir "%APP_DATA%\files"
echo.
echo IIS publish folder ready: %OUT_DIR%
echo Copy contents of this folder to C:\inetpub\livestock\ on the target IIS server.
exit /b 0
