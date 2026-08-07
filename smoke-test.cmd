@echo off
setlocal EnableDelayedExpansion
echo === LivestockManager: Smoke Test ===
call "%~dp0test.cmd"
if errorlevel 1 (exit /b %ERRORLEVEL%)
set "WEB_PROJ=%~dp0src\LivestockManager.Web\LivestockManager.Web.csproj"
set "PORT=5199"
set "ASPNETCORE_URLS=http://localhost:%PORT%"
set "ASPNETCORE_ENVIRONMENT=Production"
set "SeedDemoData=0"
set "EnableDevSeed=false"
start "" /b dotnet run --project "%WEB_PROJ%" -c Release --no-build --no-restore > "%TEMP%\livestock-smoke.log" 2>&1
set "PID_CMD="
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%PORT% ^| findstr LISTENING') do set "PID_CMD=%%a"
set /a tries=0
:waitloop
set /a tries+=1
if %tries% GTR 30 goto stopserver
timeout /t 2 /nobreak >nul
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :%PORT% ^| findstr LISTENING') do set "PID_CMD=%%a"
if not defined PID_CMD goto waitloop
echo Server listening on :%PORT% (PID=!PID_CMD!)
curl -s -o "%TEMP%\livestock-health.txt" -w "%%{http_code}" "http://localhost:%PORT%/health" > "%TEMP%\livestock-health-code.txt"
set /p HTTPCODE=<"%TEMP%\livestock-health-code.txt"
echo Health endpoint HTTP=!HTTPCODE!
:stopserver
if defined PID_CMD (taskkill /PID !PID_CMD! /F /T >nul 2>nul & timeout /t 2 /nobreak >nul)
if /i "%HTTPCODE%"=="200" (echo SMOKE OK: /health returned 200 & exit /b 0) else (echo SMOKE FAIL: /health did not return 200 (got %HTTPCODE%) & exit /b 1)
