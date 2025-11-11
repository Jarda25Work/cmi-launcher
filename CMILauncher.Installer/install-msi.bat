@echo off
echo ====================================
echo CMI Launcher - MSI Installation
echo ====================================
echo.

set MSI_PATH=%~dp0bin\x64\Release\CMILauncherSetup.msi

if not exist "%MSI_PATH%" (
    echo ERROR: MSI file not found!
    echo Expected location: %MSI_PATH%
    echo.
    echo Please build the MSI first using build-msi.bat
    pause
    exit /b 1
)

echo MSI file: %MSI_PATH%
echo Size: 
dir "%MSI_PATH%" | findstr /C:"CMILauncherSetup.msi"
echo.

echo Choose installation type:
echo [1] Interactive (GUI)
echo [2] Silent (no GUI)
echo [3] Silent with log
echo [4] Uninstall
echo [5] Exit
echo.

choice /C 12345 /N /M "Enter your choice: "

if errorlevel 5 goto :EOF
if errorlevel 4 goto uninstall
if errorlevel 3 goto silent_log
if errorlevel 2 goto silent
if errorlevel 1 goto interactive

:interactive
echo.
echo Starting interactive installation...
msiexec /i "%MSI_PATH%"
goto end

:silent
echo.
echo Starting silent installation...
msiexec /i "%MSI_PATH%" /quiet /norestart
echo Installation completed silently.
goto end

:silent_log
echo.
set LOG_FILE=%TEMP%\CMILauncher_install_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%.log
set LOG_FILE=%LOG_FILE: =0%
echo Starting silent installation with logging...
echo Log file: %LOG_FILE%
msiexec /i "%MSI_PATH%" /quiet /norestart /l*v "%LOG_FILE%"
echo.
echo Installation completed.
echo Opening log file...
notepad "%LOG_FILE%"
goto end

:uninstall
echo.
echo Uninstalling CMI Launcher...
msiexec /x "%MSI_PATH%" /quiet /norestart
echo Uninstall completed.
goto end

:end
echo.
echo ====================================
pause
