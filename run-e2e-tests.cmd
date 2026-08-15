@echo off
REM =========================================================================
REM AHK Livestock Manager Playwright E2E Runner (CMD wrapper for
REM scripts\Run-E2ETests.ps1).
REM
REM Default: automated/headless CI run.
REM
REM Common invocations:
REM   run-e2e-tests.cmd                             Automated/headless (Server=.)
REM   run-e2e-tests.cmd headed                       Local developer with visible
REM                                                  Chromium window
REM   run-e2e-tests.cmd ".\SQLEXPRESS" headed         Custom SQL instance + headed
REM   run-e2e-tests.cmd "." "" "6005"                Fixed app port 6005
REM   run-e2e-tests.cmd "." "" "" keepdatabase       Keep E2E DB after run
REM   set TEST_FILTER=FullyQualifiedName~Workflow_01
REM   run-e2e-tests.cmd                              Run a single workflow test
REM =========================================================================

setlocal
set PSScript=%~dp0scripts\Run-E2ETests.ps1

if NOT EXIST "%PSScript%" (
    echo [E2E][FAIL] Missing scripts\Run-E2ETests.ps1 at %PSScript%
    exit /b 98
)

set "ARG_SERVER=-ServerInstance ."
set "ARG_DB="
set "ARG_PORT="
set "ARG_KEEPDB="
set "ARG_HEADED="
set "ARG_SKIPBROWSER="
set "ARG_FILTER="

if /I "%~1"=="/?" goto HELP
if /I "%~1"=="-?" goto HELP
if /I "%~1"=="--help" goto HELP

REM Positional 1: ServerInstance
if NOT "%~1"=="" (
    if /I NOT "%~1"=="headed" (
        if /I NOT "%~1"=="keepdatabase" (
            if /I NOT "%~1"=="skipbrowserinstall" (
                set "ARG_SERVER=-ServerInstance %~1"
            )
        )
    )
)

REM Positional 2: DatabaseName (pass as-is; intentionally no default override)
if NOT "%~2"=="" (
    if /I NOT "%~2"=="headed" (
        if /I NOT "%~2"=="keepdatabase" (
            set "ARG_DB=-DatabaseName %~2"
        )
    )
)

REM Positional 3: AppPort
if NOT "%~3"=="" (
    set "ARG_PORT=-AppPort %~3"
)

REM Positional 4: keepdatabase
if /I "%~4"=="keepdatabase" set "ARG_KEEPDB=-KeepDatabase"
if /I "%~1"=="keepdatabase" set "ARG_KEEPDB=-KeepDatabase"
if /I "%~2"=="keepdatabase" set "ARG_KEEPDB=-KeepDatabase"
if /I "%~3"=="keepdatabase" set "ARG_KEEPDB=-KeepDatabase"

REM Named-like tokens: headed / skipbrowserinstall
for %%T in (%*) do (
    if /I "%%T"=="headed"               set "ARG_HEADED=-Headed"
    if /I "%%T"=="skipbrowserinstall"   set "ARG_SKIPBROWSER=-SkipBrowserInstall"
)

REM Optional TEST_FILTER environment variable maps to -TestFilter
if DEFINED TEST_FILTER (
    set "ARG_FILTER=-TestFilter %TEST_FILTER%"
)

REM NOTE: Script execution policy Bypass on the call only (machine policy untouched)
powershell -NoProfile -ExecutionPolicy Bypass -File "%PSScript%" %ARG_SERVER% %ARG_DB% %ARG_PORT% %ARG_KEEPDB% %ARG_HEADED% %ARG_SKIPBROWSER% %ARG_FILTER%
set "EC=%ERRORLEVEL%"
exit /b %EC%

:HELP
echo AHK Livestock Manager E2E runner (cmd wrapper for scripts\Run-E2ETests.ps1)
echo.
echo Usage:
echo   %~n0 [ServerInstance] [DatabaseName] [AppPort] [keepdatabase] [headed] [skipbrowserinstall]
echo.
echo Flags may appear in any order. TEST_FILTER env variable is mapped to -TestFilter.
echo.
echo Examples:
echo   %~n0                               ^| Automated/headless, Server=.
echo   %~n0 headed                        ^| Local dev + visible Chromium
echo   %~n0 ".\SQLEXPRESS" headed          ^| Custom instance + headed
echo   %~n0 . "" 6005 keepdatabase        ^| Fixed app port 6005, keep the E2E DB
echo   set TEST_FILTER=FullyQualifiedName~Workflow_01 ^& %~n0
exit /b 0
