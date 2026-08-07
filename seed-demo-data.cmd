@echo off
setlocal enabledelayedexpansion

set "SCRIPT_NAME=seed-demo-data"
set "EXIT_CODE=0"
set "TIMESTAMP=%DATE:/=-%_%TIME::=-%"
set "TIMESTAMP=!TIMESTAMP: =0!"
set "SCRIPT_DIR=%~dp0"
set "ARTIFACTS_DIR=%SCRIPT_DIR%artifacts"
set "LOG_DIR=%ARTIFACTS_DIR%\logs"
set "LOG_FILE=%LOG_DIR%\%SCRIPT_NAME%-!TIMESTAMP!.log"

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%" >nul 2>&1

call :LOG "=== seed-demo-data.cmd started ==="
call :LOG "Timestamp: !TIMESTAMP!"
call :LOG "Working directory: %CD%"

where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    call :LOG "ERROR: dotnet CLI not found in PATH"
    echo ERROR: dotnet CLI is required but not installed or not in PATH
    set "EXIT_CODE=1"
    goto :END
)

for /f "delims=" %%i in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%i"
call :LOG "dotnet version: !DOTNET_VER!"

set "WEB_PROJECT=%SCRIPT_DIR%src\LivestockManager.Web\LivestockManager.Web.csproj"
if not exist "%WEB_PROJECT%" (
    call :LOG "ERROR: Web project not found at %WEB_PROJECT%"
    echo ERROR: Expected Web project at %WEB_PROJECT%
    set "EXIT_CODE=1"
    goto :END
)

set "SEED_DEMO_DATA=1"
set "ASPNETCORE_ENVIRONMENT=Development"
call :LOG "SEED_DEMO_DATA=1"
call :LOG "Starting web project to seed demo data..."
call :LOG "Web project will seed data on startup and listen for shutdown..."

dotnet run --project "%WEB_PROJECT%"
set "RUN_EXIT=%ERRORLEVEL%"

if %RUN_EXIT% neq 0 (
    call :LOG "WARNING: dotnet run exited with code %RUN_EXIT%"
)

call :LOG "Demo data seed process completed"
echo Demo data seed process completed. Log: %LOG_FILE%

:END
call :LOG "=== seed-demo-data.cmd finished with exit code !EXIT_CODE! ==="
exit /b !EXIT_CODE!

:LOG
echo [%DATE% %TIME%] %~1 >> "%LOG_FILE%"
echo [%DATE% %TIME%] %~1
exit /b 0
