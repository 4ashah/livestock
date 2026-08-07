@echo off
setlocal
echo === LivestockManager: Apply EF Migrations ===
set "INFRA_PROJ=%~dp0src\LivestockManager.Infrastructure\LivestockManager.Infrastructure.csproj"
set "STARTUP_PROJ=%~dp0src\LivestockManager.Web\LivestockManager.Web.csproj"
if not exist "%INFRA_PROJ%" (echo ERROR: Infrastructure project not found at %INFRA_PROJ% & exit /b 1)
if not exist "%STARTUP_PROJ%" (echo ERROR: Web/startup project not found at %STARTUP_PROJ% & exit /b 1)
where dotnet >nul 2>nul
if errorlevel 1 (echo ERROR: dotnet SDK not found on PATH & exit /b 1)
dotnet ef database update --project "%INFRA_PROJ%" --startup-project "%STARTUP_PROJ%" --no-build
exit /b %ERRORLEVEL%
