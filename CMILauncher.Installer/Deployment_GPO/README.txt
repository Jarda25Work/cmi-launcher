# CMI Launcher - Instalační balíček pro GPO distribuci

Tento balíček je určen pro IT administrátory k hromadné distribuci CMI Launcher v doméně.

## 📦 Obsah balíčku

- `CMILauncherSetup.msi` (585 KB) - MSI instalační balíček
- `cab1.cab` - Instalační data (NUTNÉ - musí být ve stejné složce jako MSI!)
- `install-with-prerequisites.bat` (3.5 KB) - Instalační skript s automatickou instalací .NET 8 a WebView2
- `NÁVOD_GPO.md` - Podrobný návod pro nasazení přes Group Policy
- `README.txt` - Tento soubor

⚠️ **DŮLEŽITÉ:** Při kopírování na síťovou složku zkopírujte VŠECHNY soubory včetně cab1.cab!

## 🚀 Rychlý návod

1. Zkopírujte obsah tohoto balíčku na síťovou složku dostupnou všem počítačům
   Příklad: `\\server\NETLOGON\CMILauncher\` nebo `\\server\Software\CMILauncher\`

2. Vytvořte nové Group Policy Object (GPO)
   - Otevřete Group Policy Management Console (gpmc.msc)
   - Vytvořte nové GPO: "CMI Launcher - Deploy"

3. Přidejte startup script
   - Computer Configuration → Policies → Windows Settings → Scripts → Startup
   - Script Name: `\\server\Software\CMILauncher\install-with-prerequisites.bat`

4. Propojte GPO s organizational unit (OU) s cílovými počítači

5. Testujte na pilotní skupině před plným nasazením

## 📋 Požadavky

- Active Directory doméně s Windows Server 2012 R2+
- Síťová složka přístupná všem Domain Computers
- Klientské počítače: Windows 10 (64-bit) nebo novější
- Internet připojení na klientských počítačích (pro stažení .NET 8 a WebView2)

## ⚙️ Co se nainstaluje

1. **.NET 8 Desktop Runtime** (pokud chybí)
   - Automaticky staženo z: https://download.visualstudio.microsoft.com/...
   - Velikost: ~50 MB
   - Čas instalace: 1-2 minuty

2. **Microsoft Edge WebView2 Runtime** (pokud chybí)
   - Automaticky staženo z: https://go.microsoft.com/fwlink/p/?LinkId=2124703
   - Velikost: ~120 MB
   - Čas instalace: 1-2 minuty

3. **CMI Launcher** (vždy)
   - Instalační složka: `C:\Program Files\CMI Launcher\`
   - Desktop aplikace: `C:\iscmi\`
   - Zástupce: Start Menu → CMI Launcher

## 🔍 Monitoring instalace

Po nasazení GPO můžete kontrolovat stav instalace:

```powershell
# Zkontrolovat aplikaci GPO
gpresult /r

# Zkontrolovat instalaci na počítači
Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "CMI Launcher" }

# Event log
Get-EventLog -LogName Application -Source "MsiInstaller" -Newest 10
```

## 📖 Podrobný návod

Otevřete soubor `NÁVOD_GPO.md` pro kompletní návod včetně:
- Detailní kroky konfigurace GPO
- Nastavení oprávnění na síťové složce
- WMI filtry pro časování instalace
- Monitoring a reporting
- Odstraňování problémů
- PowerShell skripty pro hromadné operace

## ⚠️ Důležité poznámky

- Instalace vyžaduje **administrátorská práva** (GPO startup script běží jako SYSTEM)
- Počítače musí mít **přístup k internetu** pro stažení prerequisites
- **Testujte** na malé skupině počítačů před plným nasazením
- **Zálohujte** GPO před prováděním změn

## 🆘 Podpora

Technická podpora:
- Email: support@cmi.cz
- Web: https://www.cmi.cz

Pro hlášení problémů pošlete:
- Screenshot chybové hlášky
- Event log z problémového počítače
- Výstup `gpresult /h report.html`

---

**Verze:** 1.0.0  
**Datum:** Listopad 2025  
**Pro:** CMI Launcher
