@echo off
setlocal
echo === LivestockManager: Build (Release) ===
set "SOLUTION=%~dp0LivestockManager.sln"
if not exist "%SOLUTION%" (echo ERROR: Solution not found at %SOLUTION% & exit /b 1)
where dotnet >nul 2>nul
if errorlevel 1 (echo ERROR: dotnet SDK not found on PATH & exit /b 1)
dotnet build "%SOLUTION%" -c Release --nologo -v minimal
exit /b %ERRORLEVEL%
