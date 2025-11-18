# ČMI Launcher - Dokumentace

## Přehled

ČMI Launcher je desktopová Windows aplikace, která poskytuje přístup k webovému aplikačnímu portálu ČMI (launcher.cmi.cz). Jedná se o **port webové aplikace do Windows** - aplikace slouží jako moderní "prohlížeč" zobrazující existující webovou aplikaci.

## Verze

- **Aktuální verze**: 1.0.0.4
- **Datum vydání**: Listopad 2025
- **Framework**: .NET 8.0
- **Platforma**: Windows 10/11 (x64)

## Architektura

### Hlavní komponenty

```
┌─────────────────────────────────────────┐
│      ČMI Launcher (WPF Aplikace)       │
│         (.NET 8.0 Windows)              │
├─────────────────────────────────────────┤
│                                         │
│  ┌───────────────────────────────┐     │
│  │   Microsoft Edge WebView2     │     │
│  │   (Chromium Engine)           │     │
│  └───────────────────────────────┘     │
│              ↓                          │
│  ┌───────────────────────────────┐     │
│  │   launcher.cmi.cz/app         │     │
│  │   (Webová aplikace)           │     │
│  └───────────────────────────────┘     │
│                                         │
└─────────────────────────────────────────┘
```

### 1. .NET 8.0 Framework

**Důvody použití:**
- **Dlouhodobá podpora (LTS)**: .NET 8.0 má podporu do listopadu 2026
- **Bezpečnost**: Pravidelné bezpečnostní aktualizace od Microsoftu
- **Výkon**: Výrazná vylepšení výkonu oproti předchozím verzím
- **Moderní runtime**: Aktuální bezpečnostní prvky a optimalizace

**Bezpečnostní aspekty:**
- Automatické bezpečnostní aktualizace přes Windows Update
- Integrovaná ochrana proti běžným útokům (buffer overflow, injection)
- Silná typová kontrola a paměťová bezpečnost
- Sandboxing aplikací

### 2. Microsoft Edge WebView2

**Co je WebView2:**
- Moderní embedded browser control postavený na Chromium enginu
- Stejný engine jako Microsoft Edge prohlížeč
- Pravidelně aktualizovaný Microsoftem

**Výhody:**
- **Bezpečnost**: Automatické bezpečnostní aktualizace přes Windows Update
- **Moderní web standardy**: Plná podpora HTML5, CSS3, JavaScript ES2022+
- **Výkon**: Optimalizovaný Chromium engine s hardware akcelerací
- **Kompatibilita**: Stejné chování jako Microsoft Edge

**Bezpečnostní prvky:**
- Site Isolation - izolace jednotlivých domén
- Sandbox pro běh webového kódu
- Same-origin policy
- Content Security Policy (CSP)
- Automatická aktualizace bezpečnostních záplat

### 3. WPF (Windows Presentation Foundation)

**Účel:**
- Nativní Windows UI framework
- Poskytuje okno a kontejner pro WebView2
- Minimální vlastní kód - aplikace je především "wrapper" pro web

**Funkce:**
- Inicializace a správa WebView2
- Zobrazení úvodní obrazovky při startu
- Dialogy pro výběr certifikátů
- Error handling a retry mechanismy

## Bezpečnostní model

### Princip fungování

ČMI Launcher je **čistý port webové aplikace** - to znamená:

1. **Žádná business logika v aplikaci**: Veškerá aplikační logika běží na webovém serveru launcher.cmi.cz
2. **Žádné lokální zpracování dat**: Aplikace pouze zobrazuje webový obsah
3. **Standardní webové bezpečnostní mechanismy**: Stejné jako v prohlížeči

```
┌──────────────────────────────────────────────────────┐
│  ČMI Launcher                                        │
│  ┌────────────────────────────────────────────┐     │
│  │  WebView2 (Edge Chromium)                  │     │
│  │  ┌──────────────────────────────────────┐  │     │
│  │  │  launcher.cmi.cz/app                 │  │     │
│  │  │  (Běží na serveru, ne v aplikaci)    │  │     │
│  │  └──────────────────────────────────────┘  │     │
│  └────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────┘
```

### Bezpečnostní vrstvy

#### 1. .NET 8.0 Runtime
- **Aktualizace**: Automaticky přes Windows Update
- **Podpora**: Microsoft LTS do 11/2026
- **Bezpečnost**: Code Access Security, Strong Name Assembly
- **Izolace**: AppDomain sandboxing

#### 2. WebView2 Engine
- **Aktualizace**: Automaticky přes Windows Update (součást Edge)
- **Frekvence**: Každé 4-6 týdnů (stejně jako Edge)
- **Sandbox**: Multi-process architecture s site isolation
- **HTTPS**: Vynucené šifrované spojení na launcher.cmi.cz

#### 3. Webová aplikace
- **Backend**: Běží na zabezpečených serverech ČMI
- **HTTPS**: Veškerá komunikace šifrována TLS 1.3
- **Autentizace**: Komerční certifikáty, eID
- **Autorizace**: Server-side kontroly přístupových práv

### Co aplikace NEOBSAHUJE

**Žádná citlivá data lokálně:**
- ❌ Uživatelská hesla nebo tokeny
- ❌ Obchodní data nebo dokumenty
- ❌ Přístupové klíče
- ❌ Osobní údaje

**Žádná business logika:**
- ❌ Zpracování objednávek
- ❌ Správa uživatelů
- ❌ Validace dat
- ❌ Cenové kalkulace

**Vše běží na serveru - aplikace je pouze "okno" k webu.**

## Autentizace a certifikáty

### Komerční certifikáty

Aplikace podporuje přihlášení pomocí komerčních certifikátů:

1. **Certifikát je v Windows Certificate Store**
2. **Aplikace detekuje dostupné certifikáty**
3. **Uživatel vybere certifikát**
4. **WebView2 použije certifikát pro HTTPS client authentication**
5. **Server ověří certifikát a vytvoří session**

**Bezpečnost:**
- Privátní klíč certifikátu nikdy neopouští Windows Certificate Store
- Certifikát je chráněn PIN kódem (zadává uživatel)
- TLS handshake s mutual authentication

### eID (elektronická občanka)

Podpora pro eID přes standardní webové API:
- JavaScript Web Crypto API
- PKCS#11 interface pro čtečku
- Stejná bezpečnost jako ve webovém prohlížeči

## Síťová komunikace

### Připojení

```
CMI Launcher → HTTPS (TLS 1.3) → launcher.cmi.cz
                                    ↓
                              [Load Balancer]
                                    ↓
                              [Web Servery]
                                    ↓
                              [API Backend]
```

**Protokoly:**
- HTTPS (TLS 1.3) - veškerá komunikace
- WebSocket Secure (WSS) - real-time komunikace
- HTTP/2 nebo HTTP/3 - rychlá komunikace

**Bezpečnostní mechanismy:**
- Certificate pinning na straně serveru
- HSTS (HTTP Strict Transport Security)
- CSP (Content Security Policy)
- CORS (Cross-Origin Resource Sharing)

### Error handling a retry

Aplikace obsahuje **automatický retry mechanismus** pro síťové chyby:

1. **Detekce výpadku**: Connection timeout, DNS error, network change
2. **Automatické pokusy**: 3× s progresivním zpožděním (500ms, 1s, 2s)
3. **Uživatelské rozhraní**: Dialog s možností manuálního retry
4. **Skrytí WebView**: Během error dialogu je WebView skrytý

**Bezpečnost:**
- Žádné lokální cache citlivých dat
- Při výpadku se ztratí pouze UI state (server drží session)
- Po obnovení připojení se aplikace reconnectuje

## Instalace a deployment

### Požadavky

**Operační systém:**
- Windows 10 version 1809 nebo novější
- Windows 11 (všechny verze)

**Runtime komponenty:**
- .NET 8.0 Desktop Runtime (x64) - instaluje se automaticky
- Microsoft Edge WebView2 Runtime - instaluje se automaticky

**Hardware:**
- 2 GB RAM (doporučeno 4 GB)
- 500 MB volného místa na disku
- Internetové připojení

### Instalační balíčky

#### 1. Inno Setup (.exe)
- **Soubor**: `CMILauncher_InnoSetup.exe`
- **Typ**: Samostatný instalátor pro jednotlivé stanice
- **Velikost**: ~150 MB (obsahuje dependencies)
- **Použití**: Manuální instalace, testování

**Součást instalace:**
- CMILauncher.exe aplikace
- .NET 8.0 Desktop Runtime (pokud chybí)
- Edge WebView2 Runtime (pokud chybí)
- Desktop zástupce
- Start Menu položka
- Uninstaller

#### 2. MSI Installer (.msi)
- **Soubor**: `CMILauncherSetup.msi`
- **Typ**: Windows Installer pro GPO deployment
- **Velikost**: ~5 MB (bez dependencies)
- **Použití**: Automatický rollout přes Active Directory

**GPO Deployment:**
```
Group Policy Management
  └─ Forest: domain.local
     └─ Domains
        └─ domain.local
           └─ Group Policy Objects
              └─ [New GPO: CMI Launcher Deploy]
                 └─ Computer Configuration
                    └─ Policies
                       └─ Software Settings
                          └─ Software installation
                             └─ New → Package
                                └─ CMILauncherSetup.msi
```

**Výhody MSI:**
- Centralizovaný deployment
- Automatická instalace dependencies
- Upgrade management
- Reporting a monitoring

### Automatické aktualizace

**Runtime komponenty:**
- ✅ .NET 8.0 Runtime - Windows Update
- ✅ Edge WebView2 - Windows Update (každých 4-6 týdnů)

**CMI Launcher aplikace:**
- ⚠️ Zatím manuální update (nová instalace)
- 🔄 Plánováno: Auto-update mechanismus v budoucí verzi

## Konfigurace

### Výchozí nastavení

Aplikace používá **zero-configuration** přístup:
- Žádný konfigurační soubor
- Žádné nastavení v registrech
- Všechna konfigurace je na straně serveru

### Startup URL

Aplikace se připojuje na: `https://launcher.cmi.cz/app`

URL je hardcoded v aplikaci z bezpečnostních důvodů (nelze přesměrovat na jiný server).

### User Data Directory

WebView2 ukládá data do:
```
%LocalAppData%\CMILauncher\WebView2
```

**Obsahuje:**
- Browser cache
- Cookies (session cookies pro launcher.cmi.cz)
- Local Storage (pouze pro launcher.cmi.cz)
- IndexedDB (pouze pro launcher.cmi.cz)

**Bezpečnost:**
- Isolated storage - pouze pro CMI Launcher
- Šifrováno Windows Data Protection API (DPAPI)
- Automaticky čištěno při uninstall

## Monitoring a diagnostika

### Logy

**Debug output:**
Aplikace loguje do Debug konzole:
```
Debug.WriteLine("WebView2 initialized successfully");
Debug.WriteLine($"Navigating to: {url}");
Debug.WriteLine($"Certificate selected: {cert.Subject}");
```

**Produkční logy:**
- Aktuálně pouze Debug output
- Plánováno: Strukturované logy do souboru

### Error reporting

**Uživatelské chyby:**
- Dialog s popisem problému
- Možnost retry
- Informace pro podporu

**Technické chyby:**
- Zachyceny v Debug konzoli
- Žádné automatické reporting (zatím)

## Upgrade z Electron verze

### Rozdíly

| Aspekt | Electron (stará verze) | .NET 8 + WebView2 (nová) |
|--------|------------------------|--------------------------|
| **Runtime** | Node.js + Chromium | .NET 8 + Edge |
| **Velikost** | ~200 MB | ~150 MB |
| **Aktualizace** | Manuální | Windows Update |
| **Bezpečnost** | Custom updates | Microsoft managed |
| **Výkon** | Dobrý | Lepší (native) |
| **Integrace** | Omezená | Plná Windows integrace |

### Migrace

**Automatická:**
- Obě verze mohou běžet vedle sebe
- Doporučeno: Odinstalovat starou verzi před instalací nové

**Data:**
- Webová aplikace používá server-side session
- Žádná lokální data k migraci
- Po přihlášení novou verzí vše funguje stejně

## Řešení problémů

### WebView2 Runtime chybí

**Problém:** Aplikace nenaběhne, chyba "WebView2 Runtime not found"

**Řešení:**
1. Stáhnout WebView2 Runtime: https://go.microsoft.com/fwlink/p/?LinkId=2124703
2. Nebo použít Inno Setup installer (obsahuje runtime)

### .NET 8 Runtime chybí

**Problém:** Aplikace nenaběhne, chyba ".NET Runtime not found"

**Řešení:**
1. Stáhnout .NET 8 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
2. Nebo použít Inno Setup installer (obsahuje runtime)

### Síťové problémy

**Problém:** "Nelze se připojit k serveru"

**Řešení:**
1. Zkontrolovat internetové připojení
2. Zkontrolovat firewall (povolit CMILauncher.exe)
3. Zkontrolovat proxy nastavení (aplikace používá Windows proxy)
4. Kliknout "Zkusit znovu"

### Certifikát není detekován

**Problém:** "Žádný certifikát nalezen"

**Řešení:**
1. Ověřit, že certifikát je v Windows Certificate Store (Current User → Personal)
2. Zkontrolovat, že certifikát má privátní klíč
3. Zkontrolovat, že certifikát není expirovaný
4. Restartovat aplikaci

## Vývoj a building

### Požadavky pro build

**Software:**
- Visual Studio 2022 (17.8+) nebo Rider
- .NET 8.0 SDK
- Windows 10/11 SDK
- WiX Toolset 4.x (pro MSI)
- Inno Setup 6.x (pro .exe installer)

### Build z příkazové řádky

**Debug build:**
```powershell
cd CMILauncher
dotnet build
```

**Release build:**
```powershell
cd CMILauncher
dotnet build -c Release
```

**Inno Setup installer:**
```powershell
cd InnoSetup
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" CMILauncherInstaller.iss
```

**MSI installer:**
```powershell
cd CMILauncher.Installer
dotnet build -c Release
```

### Struktura projektu

```
migration_test/
├─ CMILauncher/                    # Hlavní WPF aplikace
│  ├─ MainWindow.xaml              # UI definice
│  ├─ MainWindow.xaml.cs           # Code-behind
│  ├─ ElectronBridge.cs            # Legacy bridge (pro kompatibilitu)
│  └─ Resources/
│     └─ icon.ico                  # Ikona aplikace
├─ CMILauncher.Installer/          # MSI installer projekt
│  ├─ Package.wxs                  # WiX definice
│  └─ CustomActions/               # Instalační skripty
├─ CMILauncher.Installer.Prerequisites/  # Prerequisite installer
└─ InnoSetup/                      # Inno Setup installer
   ├─ CMILauncherInstaller.iss     # Inno script
   └─ prereq/                      # Runtime prerequisites
      ├─ windowsdesktop-runtime-8.0.11-win-x64.exe
      └─ MicrosoftEdgeWebview2Setup.exe
```

## Compliance a regulace

### GDPR

**Zpracování osobních údajů:**
- ❌ Aplikace NEUKLÁDÁ žádná osobní data lokálně
- ✅ Veškerá data jsou na serveru (launcher.cmi.cz)
- ✅ Session cookies jsou temporary (smazány po zavření)
- ✅ WebView2 cache je lokální, šifrovaný, smazatelný

**Práva uživatelů:**
- Právo na výmaz: Odinstalace aplikace smaže veškerá lokální data
- Právo na přenositelnost: Aplikace nedrží žádná proprietární data
- Právo na přístup: Všechna data jsou na serveru, přístupná přes web

### Bezpečnostní standardy

**Dodržované standardy:**
- ✅ OWASP Top 10 (webová aplikace na serveru)
- ✅ Microsoft Security Development Lifecycle
- ✅ CWE/SANS Top 25

**Certifikace:**
- .NET 8.0: Microsoft Supported, FIPS 140-2 compliant
- Edge WebView2: Same as Microsoft Edge (regular security audits)

## Kontakt a podpora

### Technická podpora

**Interní:**
- IT oddělení ČMI
- Kontakt: it@cmi.cz

**Developer:**
- GitHub: https://github.com/Jarda25Work/cmi-launcher
- Issues: https://github.com/Jarda25Work/cmi-launcher/issues

### Reporting bezpečnostních problémů

**Security Issues:**
- NE na GitHub Issues (veřejné)
- Email: security@cmi.cz
- Responsible disclosure policy

---

**Dokumentace verze:** 1.0
**Poslední aktualizace:** Listopad 2025
**Autor:** ČMI Development Team
