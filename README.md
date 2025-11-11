# ČMI Launcher Migration# ČMI Launcher - .NET WPF Migration Test



## PřehledToto je testovací implementace ČMI Launcheru v .NET WPF jako náhrada za stávající Electron aplikaci.

Modernizace původního Electron-based ČMI Launcheru na .NET 8 WPF aplikaci s WebView2 komponentou. Aplikace zachovává plnou kompatibilitu s existující webovou částí a přidává robustní desktop funkcionalitu.

## Funkce

## Klíčové funkce

- **WebView2 integrace**: Plná kompatibilita s původní web aplikací- ✅ **Přihlášení** přes launcher.cmi.cz pomocí WebView2

- **Custom certificate handling**: Elegantní dialog pro výběr klientských certifikátů- ✅ **Zobrazení aplikací** v hezké kartové mřížce  

- **Professional welcome screen**: Moderní UI s ČMI brandingem a animated progressem- ✅ **Spouštění webových aplikací** v browseru

- **Robust error handling**: Automatické retry logika s exponential backoff- ✅ **Detekce desktop aplikací** a jejich stavu

- **Professional installer**: Inno Setup s embedded prerequisites- ✅ **Automatické obnovování** dat

- ✅ **Status indikátory** pro připojení a operace

## Struktura projektu

- `CMILauncher/` - Hlavní WPF aplikace (.NET 8)## Požadavky

- `InnoSetup/` - Installer konfigurace a skripty

- `Downloads/` - Prerequisites pro offline instalaci- .NET 8.0 nebo novější

- Windows 10/11

## Technické detaily- WebView2 Runtime (automaticky se nainstaluje)



### WebView2 konfigurace## Sestavení a spuštění

- Transparentní pozadí pro seamless integraci

- Custom certificate request handling```bash

- Potlačení kontextového menu a F12cd migration_test/CMILauncher

dotnet restore

### Certificate workflowdotnet run

- Automatická detekce commercial certifikátů```

- Custom WPF dialog s Material Design styling

- Synchronní handling pomocí TaskCompletionSourceNebo v Debug režimu:

- Fallback na pokračování bez certifikátu```bash

dotnet run --configuration Debug

### Welcome screen```

- Profesionální overlay s branding

- Animated progress indikátor## Sestavení distribučního balíčku

- Intelligent timing based on application state

- Z-index management pro správné layering```bash

dotnet publish -c Release -r win-x64 --self-contained true

## Build a deployment```



### Development## Architektura

```bash

cd CMILauncher```

dotnet restoreCMILauncher/

dotnet build├── Models/           # Datové modely (UserInfo, AppInfo)

dotnet run├── Services/         # Služby (ApiService, AuthService)  

```├── Views/           # XAML pohledy a code-behind

├── MainWindow.xaml  # Hlavní okno aplikace

### Release build└── App.xaml        # Aplikační vstupní bod

```bash```

dotnet publish -c Release -r win-x64 --self-contained true

```## API Endpointy



### InstallerAplikace komunikuje s těmito endpointy:

1. Build aplikaci v Release módu

2. Otevři `InnoSetup/CMILauncherInstaller.iss` v Inno Setup- `GET /app` - Hlavní launcher stránka (pro přihlášení)

3. Compile installer- `GET /userInfo` - Informace o přihlášeném uživateli

- `GET /appInfo` - Seznam dostupných aplikací

## Prerekvizity pro development- `GET /launch/{id}/{env}` - Spuštění aplikace

- .NET 8 SDK

- Visual Studio 2022 nebo VS Code## Testování

- Windows 10/11 (pro WebView2)

1. **Spustit aplikaci**

## Prerekvizity pro end-user2. **Kliknout "Přihlásit se"** → otevře se WebView2 okno

- Windows 10 version 1903+ nebo Windows 113. **Přihlásit se** na launcher.cmi.cz

- WebView2 Runtime (includován v installeru)4. **Zavřít WebView2** po úspěšném přihlášení

- .NET 8 Runtime (includován v installeru)5. **Aplikace automaticky načte** seznam dostupných aplikací

6. **Kliknout na aplikaci** pro spuštění

## Konfigurace

## Výhody oproti Electron verzi

### URLs

- Production: `https://launcher.cmi.cz/app`- 🚀 **Rychlejší start** (žádné načítání Chromium enginu)

- Development: Lze změnit v `MainWindow.xaml.cs`- 💾 **Menší paměťové nároky** 

- 🔧 **Lepší integrace s Windows**

### Certificate detection- 🛡️ **Bezpečnější** (žádný Node.js v UI)

Automaticky detekuje certifikáty obsahující:- 📦 **Menší distribuční balíček**

- "commercial" nebo "komer" v Subject/Issuer/DisplayName

## Známé limitace

## Known issues

- Scope issues in MainWindow.xaml.cs after recent refactoring (funkční, ale s warnings)- Vyžaduje WebView2 runtime

- Custom certificate dialog implementation needs cleanup- Desktop aplikace zatím jen detekce, ne instalace

- Chybí automatické updaty

## Changelog- Chybí system tray integrace



### v1.0.0 (Latest)## Další kroky

- ✅ Úspěšné potlačení systémového certificate dialogu

- ✅ Custom certificate handling s MessageBox1. Implementovat instalaci desktop aplikací

- ✅ Transparent WebView2 background2. Přidat automatické aktualizace  

- ✅ Professional welcome screen3. Implementovat system tray

- ✅ Comprehensive Inno Setup installer4. Přidat offline režim

- ✅ Robustní error handling a retry logika5. Lepší error handling a retry logika



### Planned v1.1.0## Konfigurace

- 🔄 Dokončení custom WPF certificate dialogu

- 🔄 Code cleanup a refaktoringPro testování s jiným serverem změňte URL v `ApiService` konstruktoru:

- 🔄 Unit testy

- 🔄 Logging framework```csharp

// Pro localhost development

## License_apiService = new ApiService("http://localhost:9000");

Internal ČMI project
// Pro staging
_apiService = new ApiService("https://launcher-staging.cmi.cz");
```