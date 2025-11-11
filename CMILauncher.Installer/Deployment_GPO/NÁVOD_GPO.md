# CMI Launcher - Instalace přes Group Policy (GPO)

## Přehled

Tento balíček obsahuje vše potřebné pro automatickou distribuci CMI Launcher na počítače v doméně pomocí Group Policy.

## Obsah balíčku

- `CMILauncherSetup.msi` - MSI instalační balíček (524 KB)
- `install-with-prerequisites.bat` - Instalační skript s automatickou instalací prerequisites
- `NÁVOD_GPO.md` - Tento návod

## Požadavky

- **Active Directory doména** s Windows Server 2012 R2 nebo novější
- **Oprávnění:** Domain Admin nebo Group Policy Creator Owners
- **Sdílená síťová složka** přístupná všem počítačům v doméně
- **Klientské počítače:** Windows 10 (64-bit) nebo novější

---

## Krok 1: Příprava síťové složky

### 1.1 Vytvoření sdílené složky

1. Na souborovém serveru vytvořte složku, např. `\\server\NETLOGON\CMILauncher` nebo `\\server\Software\CMILauncher`

2. Zkopírujte do ní obsah tohoto balíčku:
   ```
   \\server\Software\CMILauncher\
   ├── CMILauncherSetup.msi
   └── install-with-prerequisites.bat
   ```

### 1.2 Nastavení oprávnění

**NTFS oprávnění:**
- `Domain Computers` - Read & Execute
- `Authenticated Users` - Read
- `Administrators` - Full Control

**Share oprávnění:**
- `Everyone` - Read

**PowerShell příklad:**
```powershell
$path = "C:\Shares\CMILauncher"
New-Item -Path $path -ItemType Directory -Force
Copy-Item "CMILauncherSetup.msi" -Destination $path
Copy-Item "install-with-prerequisites.bat" -Destination $path

# Sdílení
New-SmbShare -Name "CMILauncher" -Path $path -ReadAccess "Everyone"

# NTFS oprávnění
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Domain Computers","ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow")
$acl.AddAccessRule($rule)
Set-Acl $path $acl
```

---

## Krok 2: Vytvoření Group Policy Object (GPO)

### Metoda A: Startup Script (DOPORUČENO)

Instalace proběhne při startu počítače před přihlášením uživatele.

#### 2.1 Otevření Group Policy Management

1. Na Domain Controlleru otevřete **Group Policy Management Console** (gpmc.msc)
2. Rozbalte doménu → **Group Policy Objects**
3. Pravým tlačítkem → **New**
4. Název: `CMI Launcher - Deploy`

#### 2.2 Úprava GPO

1. Pravým tlačítkem na vytvořené GPO → **Edit**
2. Navigujte na:
   ```
   Computer Configuration
   └── Policies
       └── Windows Settings
           └── Scripts (Startup/Shutdown)
               └── Startup
   ```

3. Dvojklik na **Startup** → **Add**

4. Vyplňte:
   - **Script Name:** `\\server\Software\CMILauncher\install-with-prerequisites.bat`
   - **Script Parameters:** (nechte prázdné)

5. Klikněte **OK** → **Apply** → **OK**

#### 2.3 Propojení GPO s OU

1. V Group Policy Management Console rozbalte doménu
2. Vyberte Organizational Unit (OU) s cílovými počítači
3. Pravým tlačítkem na OU → **Link an Existing GPO**
4. Vyberte `CMI Launcher - Deploy`

#### 2.4 Filtrování (volitelné)

Pokud chcete instalovat pouze na vybrané počítače:

1. Vyberte GPO `CMI Launcher - Deploy`
2. Záložka **Scope**
3. **Security Filtering** → **Remove** "Authenticated Users"
4. **Add** → vyberte bezpečnostní skupinu (např. `CMI-Launcher-Install`)
5. Přidejte počítače do této skupiny

---

### Metoda B: Software Installation

Alternativa pomocí MSI distribuce (bez automatických prerequisites).

**⚠️ UPOZORNĚNÍ:** Tato metoda vyžaduje, aby .NET 8 a WebView2 Runtime byly již nainstalovány!

1. V Group Policy Editor navigujte na:
   ```
   Computer Configuration
   └── Policies
       └── Software Settings
           └── Software installation
   ```

2. Pravým tlačítkem → **New** → **Package**

3. Zadejte UNC cestu: `\\server\Software\CMILauncher\CMILauncherSetup.msi`

4. Vyberte **Assigned** (ne Published)

5. **Properties:**
   - Záložka **Deployment**
   - Zaškrtněte: ☑ **Install this application at logon**
   - Deployment options: **Basic**

---

## Krok 3: Testování

### 3.1 Testovací počítač

1. Přidejte testovací počítač do OU nebo skupiny
2. Na testovacím počítači spusťte jako admin:
   ```cmd
   gpupdate /force
   ```

3. Restartujte počítač

4. Po restartu zkontrolujte:
   - Programy a funkce → měla by být vidět "CMI Launcher"
   - Nabídka Start → měla by být ikona "CMI Launcher"
   - `C:\Program Files\CMI Launcher\CMILauncher\CMILauncher.exe`

### 3.2 Kontrola logu

Na klientském počítači zkontrolujte event log:
```powershell
Get-EventLog -LogName Application -Source "MsiInstaller" -Newest 10
```

Nebo zkontrolujte GPO startup log:
```
C:\Windows\debug\usermode\gpsvc.log
```

---

## Krok 4: Nasazení do produkce

### 4.1 Postupné nasazení

**Fáze 1: Pilotní skupina**
- Propojte GPO s malou OU (5-10 počítačů)
- Testujte 1-2 dny
- Kontrolujte Event Logy

**Fáze 2: Rozšíření**
- Postupně přidávejte další OU
- Monitorujte instalace

**Fáze 3: Plné nasazení**
- Propojte GPO s celou doménou nebo všemi OU

### 4.2 Časování

Nastavte WMI Filter pro instalaci mimo pracovní dobu:

```wql
SELECT * FROM Win32_LocalTime WHERE Hour >= 18 OR Hour <= 6
```

1. V Group Policy Management → **WMI Filters** → **New**
2. Název: `Outside Business Hours`
3. Query: (výše uvedený)
4. U GPO → **Scope** → **WMI Filtering** → vyberte vytvořený filtr

---

## Krok 5: Monitoring a reporting

### 5.1 PowerShell skript pro monitoring

```powershell
# Zkontroluj instalaci CMI Launcher na všech počítačích
$computers = Get-ADComputer -Filter * -SearchBase "OU=Workstations,DC=domain,DC=com"

$results = foreach ($pc in $computers) {
    $name = $pc.Name
    
    try {
        $installed = Invoke-Command -ComputerName $name -ScriptBlock {
            Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "CMI Launcher" }
        } -ErrorAction Stop
        
        [PSCustomObject]@{
            Computer = $name
            Status = if ($installed) { "Installed" } else { "Not Installed" }
            Version = if ($installed) { $installed.Version } else { "N/A" }
        }
    }
    catch {
        [PSCustomObject]@{
            Computer = $name
            Status = "Error"
            Version = $_.Exception.Message
        }
    }
}

$results | Export-Csv -Path "CMI-Launcher-Status.csv" -NoTypeInformation
$results | Out-GridView
```

### 5.2 Group Policy Results

Zkontrolujte, zda se GPO aplikovalo:

```powershell
# Na klientském počítači
gpresult /h gpresult.html

# Nebo vzdáleně
gpresult /s COMPUTERNAME /h gpresult.html
```

---

## Odstraňování problémů

### Instalace se nespustí

**Kontrola 1: GPO se aplikuje?**
```cmd
gpresult /r | findstr "CMI Launcher"
```

**Kontrola 2: Přístup k síťové složce?**
```cmd
dir \\server\Software\CMILauncher
```

**Kontrola 3: Oprávnění?**
- Počítačový účet musí mít Read přístup ke sdílené složce
- Zkontrolujte: `\\server\Software\CMILauncher` → Properties → Security

### Instalace selhává

**Zkontrolujte log:**
```powershell
# Event log
Get-EventLog -LogName Application -Source "MsiInstaller" -EntryType Error -Newest 5

# MSI log (pokud jste povolili)
notepad C:\Windows\Temp\CMILauncher-Install.log
```

**Časté problémy:**
- Chybí .NET 8 Runtime → Použijte `install-with-prerequisites.bat`
- Chybí WebView2 → Použijte `install-with-prerequisites.bat`
- **WebView2 E_ACCESSDENIED po instalaci** → RESTART počítače (WebView2 vyžaduje restart pro aktivaci)
- Antivirus blokuje → Přidejte výjimku

**Poznámka k WebView2:**
Po instalaci WebView2 Runtime je vyžadován restart počítače. Pokud se uživatel pokusí spustit aplikaci před restartem, zobrazí se chyba "Přístup byl odepřen (E_ACCESSDENIED)". Po restartu aplikace funguje normálně.

### GPO se neaplikuje

```powershell
# Vynucení aktualizace GPO
gpupdate /force /boot

# Restart počítače
Restart-Computer -Force
```

---

## Odinstalace přes GPO

### Metoda 1: Startup Script

1. Vytvořte `uninstall-cmi-launcher.bat`:
```bat
@echo off
msiexec /x {ProductCode} /qn /l*v C:\Windows\Temp\CMI-Uninstall.log
```

2. Přidejte jako Startup Script v nové nebo existující GPO

### Metoda 2: Software Installation

1. V původním GPO → Software Installation
2. Pravým tlačítkem na "CMI Launcher" → **All Tasks** → **Remove**
3. Vyberte: **Immediately uninstall the software from users and computers**

### Metoda 3: PowerShell

```powershell
$computers = Get-ADComputer -Filter * | Select-Object -ExpandProperty Name

Invoke-Command -ComputerName $computers -ScriptBlock {
    $app = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "CMI Launcher" }
    if ($app) {
        $app.Uninstall()
    }
}
```

---

## Aktualizace na novou verzi

1. Nahraďte soubory na síťové složce novými verzemi
2. GPO automaticky nainstaluje novou verzi při příštím restartu
3. Nebo vynuťte aktualizaci:
   ```cmd
   gpupdate /force
   shutdown /r /t 0
   ```

---

## Best Practices

✅ **DO:**
- Testujte na pilotní skupině před plným nasazením
- Používejte `install-with-prerequisites.bat` pro automatickou instalaci prerequisites
- Monitorujte instalace pomocí Event Logs
- Dokumentujte změny v GPO
- Zálohujte GPO před změnami

❌ **DON'T:**
- Neaplikujte GPO na celou doménu najednou bez testování
- Nepoužívejte MSI bez prerequisites (vyžaduje .NET 8 a WebView2)
- Neměňte GPO během pracovní doby v produkci

---

## Podpora

**Technické dotazy:**
- Email: support@cmi.cz
- Web: https://www.cmi.cz

**Problém s instalací:**
1. Zkontrolujte Event Log na klientském počítači
2. Ověřte přístup k síťové složce
3. Zkontrolujte GPO aplikaci pomocí `gpresult`
4. Kontaktujte IT podporu s log soubory
