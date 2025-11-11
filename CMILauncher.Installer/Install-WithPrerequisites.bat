@echo off
REM Simple batch wrapper for PowerShell installation script

echo.
echo Starting CMI Launcher installation with prerequisites...
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This installer must be run as Administrator
    echo.
    echo Please right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

REM Run PowerShell installation script
PowerShell.exe -ExecutionPolicy Bypass -File "%~dp0Install-WithPrerequisites.ps1"

exit /b %errorlevel%
