# CMI Launcher - Complete Setup Package

## 📦 Co bylo vytvořeno

### 1. **MSI Instalátor s AUTO-INSTALL Prerequisites** ⭐ DOPORUČENO
- **Umístění**: `CMILauncher.Installer\bin\x64\Release\CMILauncherSetup.msi`
- **Velikost**: ~524 KB
- **AUTOMATICKY nainstaluje**:
  - ✅ .NET 8 Desktop Runtime (pokud chybí - stáhne a nainstaluje)
  - ✅ Edge WebView2 Runtime (pokud chybí - stáhne a nainstaluje)
  - ✅ CMI Launcher
- **Použití**: 
  - ✅ Group Policy deployment v doméně
  - ✅ Manuální instalace
  - ✅ Síťová distribuce
  - ✅ SCCM/Intune deployment

**Žádné prerekvizity není potřeba instalovat zvlášť!** MSI vše vyřeší samo během instalace.

### 2. **PowerShell Instalátor** (alternativa)
- **Umístění**: `CMILauncher.Installer\Install-WithPrerequisites.ps1`
- **Wrapper**: `Install-WithPrerequisites.bat`
- **Stejná funkcionalita** jako MSI

## 🚀 Rychlý start

### Pro koncové uživatele:
**JEDNODUŠE SPUSTIT MSI:**
```cmd
1. Dvojklik na CMILauncherSetup.msi
2. Next → Install
3. MSI automaticky stáhne a nainstaluje .NET 8 a WebView2 (pokud chybí)
4. Spustit CMI Launcher ze Start Menu
```

**Nebo batch wrapper:**
```cmd
1. Pravý klik na Install-WithPrerequisites.bat
2. "Run as administrator"
3. Počkat na instalaci všech komponent
```

### Pro administrátory (GPO):
```cmd
1. Sestavit MSI: build-msi.bat
2. Zkopírovat CMILauncherSetup.msi na síťové umístění
3. Vytvořit GPO → Software installation → Assigned
4. HOTOVO! MSI samo nainstaluje .NET 8 a WebView2
```

**NENÍ potřeba distribuovat .NET 8 a WebView2 samostatně!**

## 📋 Build Instructions

### Sestavení MSI balíčku:
```cmd
cd CMILauncher.Installer
build-msi.bat
```

Výstup:
- `bin\x64\Release\CMILauncherSetup.msi` - MSI instalátor
- `Install-WithPrerequisites.ps1` - PowerShell skript s auto-install
- `Install-WithPrerequisites.bat` - Batch wrapper

### Testování instalace:
```cmd
cd CMILauncher.Installer
install-msi.bat

Možnosti:
[1] Interactive (GUI)
[2] Silent (no GUI)
[3] Silent with log
[4] Uninstall
```

## 📖 Deployment Guide

Podrobný návod najdete v: `README_DEPLOYMENT.md`

Obsahuje:
- Group Policy deployment
- PowerShell deployment
- Silent installation
- Monitoring a troubleshooting
- Update strategie

## 🔧 Požadavky

### Na vývojovém stroji (pro build):
- ✅ .NET 8 SDK
- ✅ WiX Toolset 5.0.2 (automaticky se nainstaluje)

### Na cílovém stroji (runtime):
- ✅ Windows 10/11 (x64)
- ✅ .NET 8 Desktop Runtime (instaluje se automaticky PowerShell skriptem)
- ✅ Edge WebView2 Runtime (instaluje se automaticky PowerShell skriptem)

## 📂 Struktura projektu

```
CMILauncher/                          # Hlavní WPF aplikace
├── MainWindow.xaml                   # UI
├── MainWindow.xaml.cs                # Logika + WebView2
├── ElectronBridge.cs                 # IPC bridge pro Electron kompatibilitu
└── CMILauncher.csproj                # .NET 8 projekt

CMILauncher.Installer/                # MSI instalátor
├── Package.wxs                       # WiX definice MSI
├── CMILauncher.Installer.wixproj     # WiX projekt
├── build-msi.bat                     # Build skript
├── install-msi.bat                   # Test instalace
├── Install-WithPrerequisites.ps1     # Auto-install s prerequisites
├── Install-WithPrerequisites.bat     # Batch wrapper
└── README_DEPLOYMENT.md              # Deployment guide
```

## ⚙️ Funkce aplikace

### CMI Launcher
- 🌐 Webové rozhraní (launcher.cmi.cz/app)
- 🖥️ Detekce desktopových aplikací
- ▶️ Spouštění lokálních aplikací
- 📥 Instalace aplikací z manifestu
- 🔄 Auto-refresh při síťových problémech
- 📦 Stream-based instalace (antivirus friendly)

### Desktop App Management
- **Detekce**: Kontrola instalovaných aplikací v `c:\iscmi`
- **Launch**: Spuštění .exe s parametry
- **Install**: Stažení + rozbalení + instalace (ZIP/TAR.GZ)
- **Manifest**: Parsování `manifest.json` pro metadata

### IPC Bridge (Electron kompatibilita)
- `window.require('electron')` API
- `ipcRenderer.send()` - odesílání zpráv
- `ipcRenderer.on()` - příjem zpráv
- Channels: `launch`, `canLaunch`, `install`, `openExternal`

## 🔐 Bezpečnost

### MSI balíček
- ✅ Podporuje code signing (signtool)
- ✅ Per-machine instalace
- ✅ Admin práva požadována
- ✅ Kontrola prerequisites před instalací

### Runtime
- ✅ WebView2 sandboxing
- ✅ HTTPS komunikace s serverem
- ✅ Stream-based instalace (žádné temp soubory)
- ✅ Validace manifest struktury

## 📊 Verze

- **CMI Launcher**: 1.0.0.0
- **.NET Target**: net8.0-windows (LTS)
- **WebView2**: 1.0.3595.46
- **System.Text.Json**: 9.0.10
- **Newtonsoft.Json**: 13.0.4

## 🐛 Troubleshooting

### MSI se neinstaluje
```powershell
# Zkontrolovat prerequisites
dotnet --list-runtimes | Select-String "WindowsDesktop"

# Zkontrolovat WebView2
Test-Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
```

### Aplikace nezobrazuje desktop apps
```
1. Zkontrolovat c:\iscmi\ adresář
2. Zkontrolovat apps.json na serveru
3. F12 DevTools v aplikaci → Console logy
```

### Bílá obrazovka při startu
- ✅ OPRAVENO: Explicitní navigace po inicializaci CoreWebView2
- ✅ Auto-retry při timeout (3 pokusy)

## 📞 Support

Pro detailní deployment guide viz: `README_DEPLOYMENT.md`
Pro development: Viz application logs v Debug Output nebo DebugView
