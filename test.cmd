@echo off
setlocal
echo === LivestockManager: Test (Release) ===
set "SOLUTION=%~dp0LivestockManager.sln"
if not exist "%SOLUTION%" (echo ERROR: Solution not found at %SOLUTION% & exit /b 1)
where dotnet >nul 2>nul
if errorlevel 1 (echo ERROR: dotnet SDK not found on PATH & exit /b 1)
dotnet test "%SOLUTION%" -c Release --nologo -v minimal --filter "FullyQualifiedName~UnitTests"
exit /b %ERRORLEVEL%
