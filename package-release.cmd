@echo off
setlocal
echo === LivestockManager: Package Release ===
set "WEB_PROJ=%~dp0src\LivestockManager.Web\LivestockManager.Web.csproj"
set "OUT_DIR=%~dp0artifacts\publish"
if not exist "%WEB_PROJ%" (echo ERROR: Web project not found at %WEB_PROJ% & exit /b 1)
where dotnet >nul 2>nul
if errorlevel 1 (echo ERROR: dotnet SDK not found on PATH & exit /b 1)
if exist "%OUT_DIR%" (rmdir /s /q "%OUT_DIR%" 2>nul)
dotnet publish "%WEB_PROJ%" -c Release -r win-x64 --self-contained false -o "%OUT_DIR%" --nologo -v minimal
if errorlevel 1 (exit /b 1)
echo Publish complete: %OUT_DIR%
if not exist "%~dp0artifacts\backups" mkdir "%~dp0artifacts\backups" >nul 2>nul
exit /b 0
