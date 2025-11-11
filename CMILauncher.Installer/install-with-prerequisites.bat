@echo off
REM Instalacni skript pro CMI Launcher s automatickou instalaci prerequisites
REM Tento skript automaticky nainstaluje .NET 8 Runtime a WebView2, pokud chybi

echo ================================================
echo   CMI Launcher - Instalace s prerequisites
echo ================================================
echo.

REM Kontrola admin prav
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo CHYBA: Tento skript vyzaduje spusteni jako Administrator.
    echo Kliknete pravym tlacitkem a zvolte "Spustit jako spravce"
    echo.
    pause
    exit /b 1
)

echo [1/4] Kontrola .NET 8 Desktop Runtime...
dotnet --list-runtimes | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ .NET 8 Desktop Runtime je jiz nainstalovany
) else (
    echo   ✗ .NET 8 Desktop Runtime neni nainstalovany
    echo   → Stahuji .NET 8 Desktop Runtime...
    
    REM Stazeni .NET 8 Desktop Runtime
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://download.visualstudio.microsoft.com/download/pr/b280d97f-25a9-4ab7-8a12-b2eb207dd7a2/bf6e0c9087ace030e0c5f1129a92d0b0/windowsdesktop-runtime-8.0.11-win-x64.exe' -OutFile '%TEMP%\dotnet8-runtime.exe'}"
    
    if not exist "%TEMP%\dotnet8-runtime.exe" (
        echo   CHYBA: Nepodarilo se stahnout .NET 8 Runtime
        pause
        exit /b 1
    )
    
    echo   → Instaluji .NET 8 Desktop Runtime...
    "%TEMP%\dotnet8-runtime.exe" /install /quiet /norestart
    
    if %errorLevel% neq 0 (
        echo   CHYBA: Instalace .NET 8 Runtime selhala
        pause
        exit /b 1
    )
    
    del "%TEMP%\dotnet8-runtime.exe"
    echo   ✓ .NET 8 Desktop Runtime uspesne nainstalovan
)

echo.
echo [2/4] Kontrola WebView2 Runtime...
reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv >nul 2>&1
if %errorLevel% equ 0 (
    echo   ✓ WebView2 Runtime je jiz nainstalovany
) else (
    echo   ✗ WebView2 Runtime neni nainstalovany
    echo   → Stahuji WebView2 Runtime...
    
    REM Stazeni WebView2 Runtime
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile '%TEMP%\webview2-runtime.exe'}"
    
    if not exist "%TEMP%\webview2-runtime.exe" (
        echo   CHYBA: Nepodarilo se stahnout WebView2 Runtime
        pause
        exit /b 1
    )
    
    echo   → Instaluji WebView2 Runtime...
    "%TEMP%\webview2-runtime.exe" /silent /install
    
    if %errorLevel% neq 0 (
        echo   CHYBA: Instalace WebView2 Runtime selhala
        pause
        exit /b 1
    )
    
    del "%TEMP%\webview2-runtime.exe"
    echo   ✓ WebView2 Runtime uspesne nainstalovan
)

echo.
echo [3/4] Instalace CMI Launcher...
if not exist "bin\x64\Release\CMILauncherSetup.msi" (
    echo CHYBA: MSI soubor nebyl nalezen: bin\x64\Release\CMILauncherSetup.msi
    pause
    exit /b 1
)

msiexec /i "bin\x64\Release\CMILauncherSetup.msi" /qb
if %errorLevel% neq 0 (
    echo CHYBA: Instalace CMI Launcher selhala
    pause
    exit /b 1
)

echo.
echo [4/4] Instalace dokoncena!
echo ================================================
echo   CMI Launcher byl uspesne nainstalovan
echo   Aplikaci naleznete v nabidce Start
echo ================================================
echo.
pause
