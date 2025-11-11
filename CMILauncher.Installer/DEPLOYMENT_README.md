# CMI Launcher - Instalační balíčky

Byly vytvořeny 2 instalační balíčky pro různé způsoby distribuce:

## 📁 Deployment_GPO/

**Pro:** IT administrátory  
**Účel:** Hromadná distribuce v doméně přes Group Policy

### Obsah:
- `START_HERE.txt` - Začněte zde! Rychlý návod
- `install-with-prerequisites.bat` - Skript pro GPO startup
- `CMILauncherSetup.msi` - MSI instalační balíček
- `NÁVOD_GPO.md` - Kompletní návod pro GPO nasazení
- `README.txt` - Přehled balíčku

### Použití:
1. Zkopírujte celou složku na síťový share (např. `\\server\NETLOGON\CMILauncher\`)
2. Nastavte oprávnění pro Domain Computers
3. Vytvořte GPO se startup scriptem
4. Propojte s OU
5. Detaily v `NÁVOD_GPO.md`

---

## 📁 Deployment_EndUser/

**Pro:** Koncové uživatele  
**Účel:** Ruční instalace na jednotlivém PC

### Obsah:
- `START_HERE.txt` - Začněte zde! Rychlý návod
- `INSTALUJ.bat` - Jednoduchý instalační skript
- `CMILauncherSetup.msi` - MSI instalační balíček
- `NÁVOD.md` - Uživatelský návod s řešením problémů
- `README.txt` - Přehled balíčku

### Použití:
1. Stáhněte/zkopírujte celou složku na PC uživatele
2. Pravým tlačítkem na `INSTALUJ.bat` → Spustit jako správce
3. Počkejte 2-5 minut
4. Hotovo!

---

## 🔄 Rozdíly mezi balíčky

| Vlastnost | GPO balíček | EndUser balíček |
|-----------|-------------|-----------------|
| **Cílová skupina** | IT administrátoři | Koncoví uživatelé |
| **Instalační skript** | `install-with-prerequisites.bat` | `INSTALUJ.bat` |
| **Návod** | Technický (GPO konfigurace) | Uživatelsky přívětivý |
| **Distribuce** | Centrální přes GPO | Ruční na každém PC |
| **Automatizace** | Plná (startup script) | Částečná (uživatel spustí) |
| **Počet instalací** | Hromadná (desítky/stovky PC) | Jednotlivé PC |

---

## ⚙️ Co oba balíčky dělají stejně

✅ Automaticky kontrolují a instalují .NET 8 Runtime  
✅ Automaticky kontrolují a instalují WebView2 Runtime  
✅ Instalují CMI Launcher do `C:\Program Files\CMI Launcher\`  
✅ Vytvoří zástupce v nabídce Start  
✅ Vyžadují administrátorská práva  
✅ Vyžadují internetové připojení

---

## 📊 Statistiky balíčků

### Deployment_GPO/
- **Počet souborů:** 5
- **Celková velikost:** ~602 KB
- **Cílová skupina:** Domain admins

### Deployment_EndUser/
- **Počet souborů:** 5
- **Celková velikost:** ~600 KB
- **Cílová skupina:** End users

---

## 🚀 Doporučené použití

### Pro hromadné nasazení v doméně:
→ Použijte **Deployment_GPO/**

### Pro jednotlivé instalace:
→ Použijte **Deployment_EndUser/**

### Pro testování před GPO nasazením:
1. Nejprve otestujte **Deployment_EndUser/** na jednom PC
2. Poté nasaďte **Deployment_GPO/** na malou pilotní skupinu
3. Po úspěšném testu rozšiřte na celou doménu

---

## 📞 Podpora

**Email:** support@cmi.cz  
**Web:** https://www.cmi.cz

---

**Verze:** 1.0.0  
**Datum:** Listopad 2025  
**Autor:** České metrologické institut
