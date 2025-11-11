@echo off
setlocal

echo ====================================
echo Building CMI Launcher Setup Package
echo ====================================
echo.

REM Build the main application first
echo [1/4] Building CMI Launcher application...
cd ..\CMILauncher
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Application build failed!
    exit /b %errorlevel%
)
echo.

REM Build Custom Actions
echo [2/4] Building Custom Actions (prerequisites installer)...
cd ..\CMILauncher.Installer.CustomActions
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Custom Actions build failed!
    exit /b %errorlevel%
)
echo.

REM Install WiX tools if not present
echo [3/4] Checking WiX Toolset...
dotnet tool list -g | findstr /C:"wix" >nul
if %errorlevel% neq 0 (
    echo Installing WiX Toolset...
    dotnet tool install --global wix --version 5.0.2
    if %errorlevel% neq 0 (
        echo ERROR: Failed to install WiX Toolset!
        exit /b %errorlevel%
    )
)
echo.

REM Build the MSI installer
echo [4/4] Building MSI installer with embedded prerequisites...
cd ..\CMILauncher.Installer
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: MSI build failed!
    exit /b %errorlevel%
)
echo.

echo ====================================
echo Build completed successfully!
echo ====================================
echo.
echo MSI package with embedded prerequisites:
cd
echo bin\x64\Release\CMILauncherSetup.msi
echo.
echo This MSI will automatically:
echo  - Check for .NET 8 Desktop Runtime
echo  - Download and install if missing
echo  - Check for WebView2 Runtime
echo  - Download and install if missing
echo  - Install CMI Launcher
echo.
echo Perfect for both GPO deployment and manual installation!
echo.

pause
