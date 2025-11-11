# CMI Launcher - MSI Deployment Guide

## Vytvoření MSI balíčku

### Prerekvizity
- .NET 8 SDK
- WiX Toolset 5.0.2 (instaluje se automaticky přes `build-msi.bat`)

### Build
```cmd
cd CMILauncher.Installer
build-msi.bat
```

MSI soubor bude vytvořen v:
```
CMILauncher.Installer\bin\x64\Release\CMILauncherSetup.msi
```

## Distribuce - Možnosti

### 1. Instalace s Prerequisites (Doporučeno pro end-users)

**Použití instalačního skriptu** který automaticky nainstaluje všechny požadavky:

```cmd
REM Jako Administrator
Install-WithPrerequisites.bat
```

Tento skript automaticky:
1. ✅ Zkontroluje a nainstaluje .NET 8 Desktop Runtime
2. ✅ Zkontroluje a nainstaluje Edge WebView2 Runtime
3. ✅ Nainstaluje CMI Launcher

**PowerShell verze** (pro automatizaci):
```powershell
# Silent installation
.\Install-WithPrerequisites.ps1 -Silent

# Interactive installation
.\Install-WithPrerequisites.ps1
```

### 2. Distribuce přes Group Policy (GPO)

MSI balíček má vestavěné kontroly prerequisites - pokud .NET 8 nebo WebView2 chybí, instalace selže s chybovou hláškou.

**Doporučená strategie:**
1. Vytvořit GPO pro .NET 8 Runtime (vyšší priorita)
2. Vytvořit GPO pro WebView2 Runtime (vyšší priorita)  
3. Vytvořit GPO pro CMI Launcher MSI

Nebo použít PowerShell skript přes GPO Startup Script.

### 1. Příprava síťového umístění

```cmd
REM Zkopírovat MSI na síťové umístění přístupné všem počítačům
copy bin\Release\net8.0-windows\en-US\CMILauncherSetup.msi \\your-server\software$\CMILauncher\
```

### 2. Vytvoření GPO

1. Otevřít **Group Policy Management Console** (gpmc.msc)
2. Pravý klik na OU (Organizational Unit) → **Create a GPO in this domain**
3. Pojmenovat GPO (např. "Deploy CMI Launcher")
4. Pravý klik na nové GPO → **Edit**

### 3. Konfigurace Computer Configuration (Počítačové nasazení)

```
Computer Configuration
  └─ Policies
      └─ Software Settings
          └─ Software installation
```

1. Pravý klik → **New** → **Package**
2. Vybrat `\\your-server\software$\CMILauncher\CMILauncherSetup.msi`
3. V dialogu vybrat: **Assigned** (doporučeno)

**Assigned vs Published:**
- **Assigned**: Instaluje se automaticky při startu počítače
- **Published**: Dostupné v Control Panel pro manuální instalaci

### 4. Konfigurace User Configuration (Uživatelské nasazení)

Alternativně pro instalaci per-user:

```
User Configuration
  └─ Policies
      └─ Software Settings
          └─ Software installation
```

Stejný postup jako u Computer Configuration.

### 5. Pokročilé nastavení

Pravý klik na balíček → **Properties**:

**Deployment tab:**
- ☑ **Uninstall this application when it falls out of the scope of management** (odinstalovat při odstranění z GPO)
- ☑ **Install this application at logon** (pouze pro User Configuration)

**Upgrades tab:**
- Lze nakonfigurovat automatické upgrady starších verzí

**Security tab:**
- **Authenticated Users**: Read & Apply Group Policy
- Lze omezit na konkrétní security groups

## Tiché odinstalování (pro starší verze)

```cmd
msiexec /x {PRODUCT-CODE} /quiet /norestart
```

Product code najdete v registru:
```
HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\
```

## Verifikace instalace

### PowerShell skript pro kontrolu
```powershell
# Zkontrolovat zda je CMI Launcher nainstalován
$installed = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "ČMI Launcher" }
if ($installed) {
    Write-Host "CMI Launcher je nainstalován - verze $($installed.Version)"
} else {
    Write-Host "CMI Launcher NENÍ nainstalován"
}
```

### Manuální kontrola
```
C:\Program Files\CMI Launcher\CMILauncher\CMILauncher.exe
```

Start Menu:
```
ČMI Launcher → ČMI Launcher
```

## Logování GPO instalace

Logy najdete v Event Vieweru:
```
Event Viewer
  └─ Windows Logs
      └─ Application
          └─ Source: MsiInstaller
```

Podrobné MSI logování:
```cmd
REM Zapnout verbose MSI logging
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Installer" /v Logging /t REG_SZ /d voicewarmupx /f

REM Logy se ukládají do:
%TEMP%\MSI*.LOG
```

## Troubleshooting

### MSI se neinstaluje
1. Zkontrolovat GPO scope (Security Filtering)
2. Zkontrolovat network path přístup (`\\server\share`)
3. Zkontrolovat Event Viewer pro chyby
4. Zkontrolovat `gpresult /h report.html` zda se GPO aplikuje

### Konflikt s existující instalací
```cmd
REM Vynutit reinstalaci
msiexec /i "\\server\software$\CMILauncher\CMILauncherSetup.msi" REINSTALL=ALL REINSTALLMODE=vomus /quiet
```

### Požadavky runtime
MSI automaticky **NEINSTALUJE**:
- .NET 8 Runtime (musí být nainstalován samostatně nebo přes GPO)
- Edge WebView2 Runtime (MSI detekuje a varuje, ale neinstaluje automaticky)

#### Distribuce .NET 8 Runtime přes GPO
```
1. Stáhnout: https://dotnet.microsoft.com/download/dotnet/8.0
   - Windows x64 Desktop Runtime (installer .exe)
2. Vytvořit nové GPO pro .NET Runtime
3. Computer Configuration → Preferences → Control Panel Settings → Scheduled Tasks
   - Spustit installer při startu pokud .NET 8 není nainstalován
```

#### Distribuce WebView2 Runtime přes GPO
```
1. Stáhnout: https://developer.microsoft.com/microsoft-edge/webview2/
   - Evergreen Bootstrapper nebo Standalone Installer
2. Přidat do stejného GPO jako CMI Launcher (vyšší priorita)
```

## Automatické podepisování MSI (Code Signing)

Pro enterprise deployment doporučuji podepsat MSI certifikátem:

```cmd
signtool sign /f "certificate.pfx" /p password /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 CMILauncherSetup.msi
```

## Update strategie

### Automatický upgrade při nové verzi:
1. Zvýšit verzi v `Package.wxs` (Version="1.0.1.0")
2. Rebuild MSI
3. Přepsat MSI na síťovém umístění
4. GPO automaticky detekuje novou verzi a upgraduje

### Force upgrade všech klientů:
```powershell
# Vynutit GPO update na klientech
Invoke-GPUpdate -Computer "PC-NAME" -Force
```

## Monitoring deployment

```powershell
# Zkontrolovat status instalace na všech počítačích v doméně
$computers = Get-ADComputer -Filter * -SearchBase "OU=Workstations,DC=domain,DC=com"
$results = @()

foreach ($computer in $computers) {
    $session = New-PSSession -ComputerName $computer.Name -ErrorAction SilentlyContinue
    if ($session) {
        $installed = Invoke-Command -Session $session -ScriptBlock {
            Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "ČMI Launcher" }
        }
        $results += [PSCustomObject]@{
            ComputerName = $computer.Name
            Installed = if ($installed) { "Yes - v$($installed.Version)" } else { "No" }
        }
        Remove-PSSession $session
    }
}

$results | Export-Csv -Path "CMILauncher-Deployment-Status.csv" -NoTypeInformation
```

