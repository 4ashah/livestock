@echo off
setlocal
echo === LivestockManager: Run Dev (port 5100) ===
set "WEB_PROJ=%~dp0src\LivestockManager.Web\LivestockManager.Web.csproj"
if not exist "%WEB_PROJ%" (echo ERROR: Web project not found at %WEB_PROJ% & exit /b 1)
where dotnet >nul 2>nul
if errorlevel 1 (echo ERROR: dotnet SDK not found on PATH & exit /b 1)
set ASPNETCORE_URLS=http://localhost:5100
set ASPNETCORE_ENVIRONMENT=Development
set EnableDevSeed=true
dotnet run --project "%WEB_PROJ%" -c Debug
exit /b %ERRORLEVEL%
