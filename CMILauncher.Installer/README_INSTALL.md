# CMI Launcher - Instalační průvodce

## Rychlý start

### Instalace s automatickými prerequisites (DOPORUČENO)

1. Stáhněte celou složku `CMILauncher.Installer\bin\x64\Release\`
2. Pravým tlačítkem na `install-with-prerequisites.bat`
3. **Spustit jako správce**
4. Skript automaticky:
   - Zkontroluje .NET 8 Desktop Runtime (nainstaluje pokud chybí)
   - Zkontroluje WebView2 Runtime (nainstaluje pokud chybí)
   - Nainstaluje CMI Launcher

### Manuální instalace MSI

Pokud už máte .NET 8 Desktop Runtime a WebView2 Runtime nainstalované:

```powershell
msiexec /i CMILauncherSetup.msi
```

## Prerequisites

Aplikace vyžaduje:

- **.NET 8 Desktop Runtime (x64)** 
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Staženo automaticky během instalace: `windowsdesktop-runtime-8.0.11-win-x64.exe`

- **Microsoft Edge WebView2 Runtime**
  - Download: https://developer.microsoft.com/microsoft-edge/webview2/
  - Staženo automaticky během instalace: `MicrosoftEdgeWebview2Setup.exe`

## Hromadná distribuce v doméně

### Pomocí Group Policy

1. Zkopírujte `install-with-prerequisites.bat` a `CMILauncherSetup.msi` na síťovou složku
2. Vytvořte GPO (Group Policy Object)
3. Computer Configuration → Policies → Windows Settings → Scripts → Startup
4. Přidejte batch soubor: `\\server\share\install-with-prerequisites.bat`

### Pomocí SCCM/Intune

Použijte MSI soubor s detekčními pravidly:

**Detection rules:**
- .NET 8: Registry `HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost`
- WebView2: Registry `HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`

**Install command:**
```cmd
install-with-prerequisites.bat
```

nebo manuálně:
```cmd
msiexec /i CMILauncherSetup.msi /qn /l*v install.log
```

### Pomocí PowerShell remoting

```powershell
# Instalace na více počítačích
$computers = "PC001", "PC002", "PC003"
$sourcePath = "\\server\share\CMILauncher"

foreach ($pc in $computers) {
    Invoke-Command -ComputerName $pc -ScriptBlock {
        param($source)
        Start-Process -FilePath "$source\install-with-prerequisites.bat" -Verb RunAs -Wait
    } -ArgumentList $sourcePath
}
```

## Odinstalace

### Lokálně

**Option 1:** Přes Nastavení Windows
- Nastavení → Aplikace → Aplikace a funkce → CMI Launcher → Odinstalovat

**Option 2:** Přes MSI
```cmd
msiexec /x CMILauncherSetup.msi
```

### Hromadně v doméně

```powershell
$computers = Get-ADComputer -Filter * | Select-Object -ExpandProperty Name

Invoke-Command -ComputerName $computers -ScriptBlock {
    $app = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "CMI Launcher" }
    if ($app) {
        $app.Uninstall()
    }
}
```

## Instalační složka

- **Program Files:** `C:\Program Files\CMI Launcher\CMILauncher\`
- **Desktop aplikace:** `C:\iscmi\` (ukládány automaticky při instalaci z webu)
- **Start Menu:** Nabídka Start → CMI Launcher

## Logování

Pro podrobné logování instalace:

```cmd
msiexec /i CMILauncherSetup.msi /l*v install.log
```

Log soubor obsahuje:
- Detekci prerequisites
- Průběh instalace
- Případné chyby

## Požadavky na systém

- **OS:** Windows 10 (64-bit) nebo novější
- **RAM:** 2 GB (doporučeno 4 GB)
- **Místo na disku:** 200 MB pro aplikaci + místo pro desktop aplikace
- **Oprávnění:** Administrátorská práva pro první instalaci
- **Internet:** Vyžadován pro stažení prerequisites a desktop aplikací

## Řešení problémů

### "DLL required for this install to complete could not be run"

- **Příčina:** Chybí .NET 8 Runtime nebo WebView2
- **Řešení:** Použijte `install-with-prerequisites.bat` místo přímé instalace MSI

### Aplikace se nespustí po instalaci

1. Zkontrolujte instalaci .NET 8:
   ```cmd
   dotnet --list-runtimes
   ```
   Měli byste vidět: `Microsoft.WindowsDesktop.App 8.x.x`

2. Zkontrolujte WebView2:
   ```cmd
   reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv
   ```

3. Přeinstalujte manuálně prerequisites a pak MSI

### Antivirus blokuje instalaci

- Přidejte výjimku pro složku `C:\Program Files\CMI Launcher\`
- Dočasně vypněte real-time protection během instalace

## Podpora

Pro technickou podporu kontaktujte:
- Email: support@cmi.cz
- Web: https://www.cmi.cz
