# CMI Launcher - Instalace pro koncové uživatele

## 🚀 Rychlý start

### Instalace v 3 krocích:

1. **Stáhněte** instalační soubory
2. **Pravým tlačítkem** na `INSTALUJ.bat`
3. **Spustit jako správce**

✅ Hotovo! Aplikace je nainstalována v nabídce Start.

---

## Co tento balíček obsahuje

- `INSTALUJ.bat` - Instalační skript (doporučeno)
- `CMILauncherSetup.msi` - MSI instalační balíček
- `NÁVOD.md` - Tento návod

---

## Podrobný postup instalace

### Varianta A: Automatická instalace (DOPORUČENO)

Tato metoda automaticky nainstaluje všechny potřebné komponenty.

1. **Rozbalte** všechny soubory do složky (např. na Plochu)

2. **Pravým tlačítkem** na soubor `INSTALUJ.bat`

3. Vyberte **"Spustit jako správce"**

4. Počkejte, až se objeví zelené potvrzení instalace

5. Aplikaci najdete v **nabídce Start** → **CMI Launcher**

**Co se stane během instalace:**
- ✓ Zkontroluje se, zda máte administrátorská práva
- ✓ Zkontroluje se .NET 8 Desktop Runtime (nainstaluje pokud chybí)
- ✓ Zkontroluje se WebView2 Runtime (nainstaluje pokud chybí)
- ✓ Nainstaluje se CMI Launcher
- ✓ Vytvoří se zástupce v nabídce Start

**Čas instalace:** 2-5 minut (závisí na rychlosti internetu)

---

### Varianta B: Manuální instalace

Pokud už máte .NET 8 a WebView2 nainstalované.

1. Dvojklik na `CMILauncherSetup.msi`

2. Postupujte podle průvodce instalací

3. Klikněte na **Nainstalovat**

**⚠️ POZOR:** Tato varianta vyžaduje, aby byly předinstalované:
- .NET 8 Desktop Runtime (x64)
- Microsoft Edge WebView2 Runtime

Pokud tyto komponenty chybí, instalace selže. V tom případě použijte **Variantu A**.

---

## Požadavky na systém

| Požadavek | Minimum |
|-----------|---------|
| **Operační systém** | Windows 10 (64-bit) |
| **Procesor** | Dual-core 1 GHz |
| **RAM** | 2 GB (doporučeno 4 GB) |
| **Místo na disku** | 500 MB |
| **Internet** | Vyžadován pro stažení komponent a desktop aplikací |
| **Oprávnění** | Administrátorská práva (pouze při instalaci) |

---

## První spuštění

1. Otevřete **nabídku Start**

2. Najděte **CMI Launcher**

3. Klikněte na ikonu

4. Aplikace se otevře a přihlásíte se svými přihlašovacími údaji

---

## Jak aplikace funguje

CMI Launcher je webový launcher, který vám umožní:

- 🚀 **Spouštět desktop aplikace** přímo z webového rozhraní
- 📥 **Automaticky stahovat a instalovat** aplikace, které nemáte
- 🔄 **Aktualizovat aplikace** na nejnovější verze
- 🖥️ **Pracovat offline** s nainstalovanými aplikacemi

**Desktop aplikace se instalují do:** `C:\iscmi\`

---

## Časté dotazy (FAQ)

### ❓ Potřebuji administrátorská práva?

**Při instalaci:** Ano, jednou při první instalaci  
**Při používání:** Ne, běžná práce nepotřebuje admin práva  
**Při instalaci desktop aplikací:** Ne, desktop aplikace se instalují do vaší složky

### ❓ Jak poznám, že je instalace dokončena?

Objeví se zpráva:
```
================================================
  CMI Launcher byl uspesne nainstalovan
  Aplikaci naleznete v nabidce Start
================================================
```

### ❓ Co dělat, když instalace selže?

1. **Zkontrolujte internetové připojení** - stahují se komponenty z internetu

2. **Vypněte antivirus dočasně** - některé antiviry blokují instalaci

3. **Spusťte znovu jako správce** - ujistěte se, že máte admin práva

4. **Zkontrolujte místo na disku** - potřebujete alespoň 500 MB volného místa

5. **Kontaktujte IT podporu** - pošlete screenshot chybové hlášky

### ❓ Aplikace hlásí chybu WebView2 po instalaci?

**Problém:** Po instalaci se zobrazí: "Chyba při inicializaci WebView2: Přístup byl odepřen."

**Řešení:** 
1. **RESTARTUJTE počítač** - WebView2 Runtime vyžaduje restart pro aktivaci
2. Po restartu spusťte CMI Launcher znovu
3. Aplikace by měla fungovat normálně

**Důvod:** WebView2 Runtime se nainstaloval, ale některé systémové komponenty se aktivují až po restartu.

### ❓ Musím být připojený k internetu?

**Při instalaci:** Ano - stahují se .NET 8 a WebView2 Runtime (~200 MB)  
**Při prvním spuštění:** Ano - aplikace potřebuje načíst webové rozhraní  
**Při běžném používání:** Ano pro načítání webového rozhraní, ale desktop aplikace fungují i offline

### ❓ Kam se aplikace instaluje?

- **CMI Launcher:** `C:\Program Files\CMI Launcher\`
- **Desktop aplikace:** `C:\iscmi\`
- **Zástupce:** Nabídka Start → CMI Launcher

### ❓ Mohu instalovat na více počítačů?

Ano, můžete nainstalovat na všechny vaše pracovní počítače. Licence není omezena počtem instalací.

### ❓ Jak se odhlásím?

Jednoduše zavřete okno aplikace. Při příštím spuštění budete vyzváni k přihlášení.

---

## Odinstalace

### Postup odinstalace:

**Metoda 1: Přes Nastavení Windows**
1. Stiskněte `Win + I` (otevře Nastavení)
2. Klikněte na **Aplikace**
3. Najděte **CMI Launcher** v seznamu
4. Klikněte na **Odinstalovat**
5. Potvrďte odinstalaci

**Metoda 2: Přes Ovládací panely**
1. Otevřete **Ovládací panely**
2. Klikněte na **Programy a funkce**
3. Najděte **CMI Launcher**
4. Pravým tlačítkem → **Odinstalovat**

**Co se smaže:**
- ✓ CMI Launcher z `C:\Program Files\CMI Launcher\`
- ✓ Zástupce z nabídky Start

**Co zůstane:**
- Desktop aplikace v `C:\iscmi\` (můžete smazat ručně, pokud chcete)

---

## Řešení problémů

### 🔴 "Tento program vyžaduje administrátorská práva"

**Řešení:**
1. Pravým tlačítkem na `INSTALUJ.bat`
2. Vyberte **"Spustit jako správce"**
3. Klikněte **Ano** v UAC dialogu

---

### 🔴 "DLL required for this install to complete could not be run"

**Příčina:** Chybí .NET 8 Runtime nebo WebView2 Runtime

**Řešení:**
1. Použijte `INSTALUJ.bat` místo přímé instalace MSI
2. Nebo ručně nainstalujte:
   - .NET 8 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
   - WebView2 Runtime: https://developer.microsoft.com/microsoft-edge/webview2/

---

### 🔴 Aplikace se nespustí po instalaci

**Kontrola 1: Je nainstalovaný .NET 8?**
```cmd
dotnet --list-runtimes
```
Měli byste vidět: `Microsoft.WindowsDesktop.App 8.x.x`

**Kontrola 2: Je nainstalovaný WebView2?**
- Otevřete `edge://settings/help` v Microsoft Edge
- Pokud Edge funguje, WebView2 je nainstalován

**Řešení:**
1. Odinstalujte CMI Launcher
2. Spusťte znovu `INSTALUJ.bat` jako správce

---

### 🔴 Antivirus blokuje instalaci

**Řešení:**
1. Dočasně vypněte real-time protection v antivirovém programu
2. Spusťte `INSTALUJ.bat` znovu
3. Po instalaci zapněte antivirus
4. Přidejte výjimku pro složku: `C:\Program Files\CMI Launcher\`

**Běžné antiviry:**
- **Windows Defender:** Nastavení → Ochrana před viry a hrozbami → Spravovat nastavení → Real-time protection (vypnout)
- **Avast/AVG:** Nastavení → Protection → Core Shields → File Shield (vypnout)
- **ESET:** Setup → Computer → Real-time file system protection (vypnout)

---

### 🔴 Instalace trvá příliš dlouho

**Normální čas:**
- .NET 8 download: 1-3 minuty (~50 MB)
- WebView2 download: 2-4 minuty (~120 MB)
- Instalace: 1-2 minuty
- **Celkem: 4-9 minut**

**Pokud trvá déle:**
- Zkontrolujte rychlost internetu
- Zkuste zavřít jiné programy
- Restartujte počítač a zkuste znovu

---

## Aktualizace na novou verzi

1. Odinstalujte starou verzi (viz sekce Odinstalace)
2. Stáhněte nový instalační balíček
3. Spusťte `INSTALUJ.bat` jako správce

**Desktop aplikace zůstanou** a nemusíte je stahovat znovu.

---

## Podpora a kontakt

### 📧 Technická podpora

**Email:** support@cmi.cz  
**Web:** https://www.cmi.cz

### 📝 Co poslat při hlášení problému:

1. **Screenshot chybové hlášky**
2. **Verze Windows** (Nastavení → Systém → O systému)
3. **Popis problému** (co jste dělali před chybou)

### ⏰ Odpověď na dotazy:

Obvykle do 24 hodin v pracovních dnech (Po-Pá, 8:00-16:00)

---

## Licenční informace

CMI Launcher je vlastnictvím **Český metrologický institut**.

Používáním této aplikace souhlasíte s podmínkami použití uvedenými na webu www.cmi.cz.

---

**Verze návodu:** 1.0  
**Datum:** Listopad 2025  
**Pro:** CMI Launcher v1.0.0

---

✅ **Děkujeme, že používáte CMI Launcher!**
