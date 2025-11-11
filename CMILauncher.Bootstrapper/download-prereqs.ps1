<#!
Skript: download-prereqs.ps1
Účel: Stáhne offline .NET 8 Desktop Runtime x64 a WebView2 Evergreen Standalone runtime
Umístění: Spouštět z kořenové složky bootstrapperu (tam kde je tento skript)
Výstup: Soubory uložené v .\Prereqs\
Použití:
  powershell.exe -ExecutionPolicy Bypass -File .\download-prereqs.ps1
#>

param(
  [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PrereqsDir = Join-Path $PSScriptRoot 'Prereqs'
if (!(Test-Path $PrereqsDir)) {
  New-Item -ItemType Directory -Path $PrereqsDir | Out-Null
}

$PackagesDir = Join-Path $PSScriptRoot 'packages'
if (!(Test-Path $PackagesDir)) {
  New-Item -ItemType Directory -Path $PackagesDir | Out-Null
}

# Definice URL – Windows Desktop Runtime (pro WPF)
$dotnetPrimaryUrl = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64'
$dotnetBackupUrl = 'https://dotnetcli.azureedge.net/dotnet/WindowsDesktop/8.0.11/windowsdesktop-runtime-8.0.11-win-x64.exe'
$dotnetTarget = Join-Path $PrereqsDir 'windowsdesktop-runtime-8.0-x64.exe'

# WebView2 Evergreen Standalone x64 – oficiální Microsoft CDN (URL se může měnit, lze nahradit při změně verze)
$webview2Url = 'https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/1f4b2c2e-0c9a-4b0d-9a5e-0c3c1d705c3a/MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
$webview2BootstrapUrl = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' # Evergreen bootstrapper fallback
$webview2Target = Join-Path $PrereqsDir 'MicrosoftEdgeWebView2RuntimeInstallerX64.exe'

function Enable-Tls12 {
  try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13 -bor [Net.SecurityProtocolType]::Tls11 -bor [Net.SecurityProtocolType]::Tls
  } catch {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  }
}

function Download-File($Url, $Target) {
  if ((Test-Path $Target) -and -not $Force) {
    Write-Host "[SKIP] $Target již existuje (použijte -Force pro přepsání)." -ForegroundColor Yellow
    return
  }
  Enable-Tls12
  $ProgressPreference = 'SilentlyContinue'
  Write-Host "[DOWNLOADING] $Url" -ForegroundColor Cyan
  try {
    Invoke-WebRequest -Uri $Url -OutFile $Target -UseBasicParsing -MaximumRedirection 10 -UserAgent 'Mozilla/5.0' -ProxyUseDefaultCredentials
  } catch {
    Write-Host "[IWR FAIL] Zkouším BITS..." -ForegroundColor Yellow
    try {
      Start-BitsTransfer -Source $Url -Destination $Target -ErrorAction Stop
    } catch {
      Write-Host "[BITS FAIL] Zkouším WebClient..." -ForegroundColor Yellow
      try {
        $wc = New-Object System.Net.WebClient
        $wc.UseDefaultCredentials = $true
        $wc.DownloadFile($Url, $Target)
      } catch {
        throw $_
      }
    }
  }
  Write-Host "[OK] Uloženo: $Target" -ForegroundColor Green
}

try {
  Download-File -Url $dotnetPrimaryUrl -Target $dotnetTarget
  $dotnetInfo = Get-Item $dotnetTarget -ErrorAction SilentlyContinue
  if (-not $dotnetInfo -or $dotnetInfo.Length -lt 5000000) { # < ~5MB je podezřelé
    Write-Host "[WARN] Stažený .NET soubor je podezřele malý ($($dotnetInfo.Length) B). Zkouším záložní URL." -ForegroundColor Yellow
    Download-File -Url $dotnetBackupUrl -Target $dotnetTarget
  }
} catch {
  Write-Host "[Fallback] Prima URL pro .NET selhala, zkouším záložní odkaz." -ForegroundColor Yellow
  Download-File -Url $dotnetBackupUrl -Target $dotnetTarget
}
try {
  Download-File -Url $webview2Url -Target $webview2Target
} catch {
  Write-Host "[Fallback] WebView2 offline nelze stáhnout, zkouším bootstrapper (online)." -ForegroundColor Yellow
  $bootstrapTarget = Join-Path $PrereqsDir 'MicrosoftEdgeWebView2RuntimeInstallerX64_BOOTSTRAPPER.exe'
  Download-File -Url $webview2BootstrapUrl -Target $bootstrapTarget
  Write-Host "Poznámka: Bundle nyní očekává offline soubor $([System.IO.Path]::GetFileName($webview2Target)). Pokud chcete použít bootstrapper, můžeme upravit Bundle.wxs na tento soubor." -ForegroundColor Yellow
}

Write-Host "Hotovo. Obsah složky Prereqs:" -ForegroundColor Magenta
Get-ChildItem $PrereqsDir | Select-Object Name, Length, LastWriteTime | Format-Table

Write-Host "Stahuji WiX extensions (nuget balíčky) pro offline build bundle..." -ForegroundColor Magenta
$balPkgUrl = 'https://www.nuget.org/api/v2/package/WixToolset.Bal.wixext/5.0.2'
$utilPkgUrl = 'https://www.nuget.org/api/v2/package/WixToolset.Util.wixext/5.0.2'
Download-File -Url $balPkgUrl -Target (Join-Path $PackagesDir 'WixToolset.Bal.wixext.5.0.2.nupkg')
Download-File -Url $utilPkgUrl -Target (Join-Path $PackagesDir 'WixToolset.Util.wixext.5.0.2.nupkg')

Write-Host "Obsah složky packages:" -ForegroundColor Magenta
Get-ChildItem $PackagesDir | Select-Object Name, Length, LastWriteTime | Format-Table

Write-Host "Pokračujte: dotnet build -c Release (bootstrapper)" -ForegroundColor Magenta