@echo off
echo Building CMI Launcher (.NET WPF)...
echo.

cd CMILauncher

echo Restoring packages...
dotnet restore
if %errorlevel% neq 0 (
    echo Failed to restore packages
    pause
    exit /b 1
)

echo Building in Debug mode...
dotnet build -c Debug
if %errorlevel% neq 0 (
    echo Build failed
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo To run the application:
echo   dotnet run --project CMILauncher
echo.
echo To create release build:
echo   dotnet publish -c Release -r win-x64 --self-contained true
echo.
pause