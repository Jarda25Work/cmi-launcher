@echo off
cd /d "%~dp0"
set TRACE_FILE=%TEMP%\CMILauncher_debug.log
echo Starting CMI Launcher with debug logging...
echo Log file: %TRACE_FILE%
bin\Debug\net8.0-windows\CMILauncher.exe > "%TRACE_FILE%" 2>&1
echo.
echo Application closed. Opening log file...
notepad "%TRACE_FILE%"
