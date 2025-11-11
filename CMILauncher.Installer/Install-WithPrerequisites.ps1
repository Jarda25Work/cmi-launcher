# CMI Launcher - Installation Script with Prerequisites
# This script checks and installs required components before installing CMI Launcher

param(
    [switch]$Silent = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  CMI Launcher Installation" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$msiPath = Join-Path $scriptDir "..\CMILauncher.Installer\bin\x64\Release\CMILauncherSetup.msi"
$tempDir = Join-Path $env:TEMP "CMILauncherSetup"

# Create temp directory
if (!(Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

# Function to check if .NET 8 Runtime is installed
function Test-DotNet8Runtime {
    try {
        $dotnetList = & dotnet --list-runtimes 2>$null
        if ($dotnetList -match "Microsoft\.WindowsDesktop\.App 8\.") {
            return $true
        }
        
        # Fallback - check registry
        $regPath = "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost"
        if (Test-Path $regPath) {
            $version = Get-ItemProperty -Path $regPath -Name "Version" -ErrorAction SilentlyContinue
            if ($version -and $version.Version -match "^8\.") {
                return $true
            }
        }
    }
    catch { }
    return $false
}

# Function to check if WebView2 Runtime is installed
function Test-WebView2Runtime {
    $regPaths = @(
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
        "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
    )
    
    foreach ($path in $regPaths) {
        if (Test-Path $path) {
            $pv = Get-ItemProperty -Path $path -Name "pv" -ErrorAction SilentlyContinue
            if ($pv -and $pv.pv) {
                return $true
            }
        }
    }
    return $false
}

# Function to download file with progress
function Download-File {
    param(
        [string]$Url,
        [string]$OutputPath,
        [string]$Description
    )
    
    Write-Host "Downloading $Description..." -ForegroundColor Yellow
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($Url, $OutputPath)
        Write-Host "  Downloaded successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  ERROR: Failed to download: $_" -ForegroundColor Red
        return $false
    }
}

# Check and install .NET 8 Desktop Runtime
Write-Host "[1/3] Checking .NET 8 Desktop Runtime..." -ForegroundColor Cyan
if (Test-DotNet8Runtime) {
    Write-Host "  .NET 8 Desktop Runtime is already installed" -ForegroundColor Green
}
else {
    Write-Host "  .NET 8 Desktop Runtime is NOT installed" -ForegroundColor Yellow
    
    $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/9d6b6b34-44b5-4cf4-b924-79a00deb9795/2f17f5643d45b7a9b1e5b8edd1e7a6d1/windowsdesktop-runtime-8.0.11-win-x64.exe"
    $dotnetInstaller = Join-Path $tempDir "windowsdesktop-runtime-8.0.11-win-x64.exe"
    
    if (Download-File -Url $dotnetUrl -OutputPath $dotnetInstaller -Description ".NET 8 Desktop Runtime") {
        Write-Host "  Installing .NET 8 Desktop Runtime..." -ForegroundColor Yellow
        
        $installArgs = "/install /quiet /norestart"
        $process = Start-Process -FilePath $dotnetInstaller -ArgumentList $installArgs -Wait -PassThru
        
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
            Write-Host "  .NET 8 Desktop Runtime installed successfully" -ForegroundColor Green
        }
        else {
            Write-Host "  WARNING: .NET 8 installation returned exit code: $($process.ExitCode)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""

# Check and install WebView2 Runtime
Write-Host "[2/3] Checking Microsoft Edge WebView2 Runtime..." -ForegroundColor Cyan
if (Test-WebView2Runtime) {
    Write-Host "  WebView2 Runtime is already installed" -ForegroundColor Green
}
else {
    Write-Host "  WebView2 Runtime is NOT installed" -ForegroundColor Yellow
    
    $webview2Url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    $webview2Installer = Join-Path $tempDir "MicrosoftEdgeWebview2Setup.exe"
    
    if (Download-File -Url $webview2Url -OutputPath $webview2Installer -Description "WebView2 Runtime") {
        Write-Host "  Installing WebView2 Runtime..." -ForegroundColor Yellow
        
        $installArgs = "/silent /install"
        $process = Start-Process -FilePath $webview2Installer -ArgumentList $installArgs -Wait -PassThru
        
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
            Write-Host "  WebView2 Runtime installed successfully" -ForegroundColor Green
        }
        else {
            Write-Host "  WARNING: WebView2 installation returned exit code: $($process.ExitCode)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""

# Install CMI Launcher MSI
Write-Host "[3/3] Installing CMI Launcher..." -ForegroundColor Cyan

if (!(Test-Path $msiPath)) {
    Write-Host "  ERROR: MSI file not found at: $msiPath" -ForegroundColor Red
    Write-Host "  Please build the MSI first using build-msi.bat" -ForegroundColor Yellow
    exit 1
}

Write-Host "  MSI file: $msiPath" -ForegroundColor Gray

if ($Silent) {
    Write-Host "  Installing silently..." -ForegroundColor Yellow
    $msiArgs = "/i `"$msiPath`" /quiet /norestart"
}
else {
    Write-Host "  Starting interactive installation..." -ForegroundColor Yellow
    $msiArgs = "/i `"$msiPath`""
}

$process = Start-Process -FilePath "msiexec.exe" -ArgumentList $msiArgs -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "  CMI Launcher installed successfully!" -ForegroundColor Green
}
elseif ($process.ExitCode -eq 3010) {
    Write-Host "  CMI Launcher installed successfully (reboot required)" -ForegroundColor Yellow
}
elseif ($process.ExitCode -eq 1602) {
    Write-Host "  Installation cancelled by user" -ForegroundColor Yellow
}
else {
    Write-Host "  Installation completed with exit code: $($process.ExitCode)" -ForegroundColor Yellow
}

# Cleanup
Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Gray
Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Installation Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now launch CMI Launcher from Start Menu" -ForegroundColor Green
Write-Host ""

if (!$Silent) {
    pause
}
