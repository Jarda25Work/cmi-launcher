; CMI Launcher Inno Setup installer with prerequisites (.NET 8 Windows Desktop Runtime + WebView2)
#define MyAppName "CMI Launcher"
#define MyAppVersion "1.0.0.4"
#define MyAppPublisher "Ceske metrologicke institut"
#define MyMsiFile "CMILauncherSetup.msi"
#define DotNetInstaller "windowsdesktop-runtime-8.0-x64.exe"
#define WebView2Installer "MicrosoftEdgeWebView2RuntimeInstallerX64_BOOTSTRAPPER.exe"

; Online URLs for prerequisites (primary + backup)
#define DotNetUrlPrimary "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64"
#define DotNetUrlBackup  "https://dotnetcli.azureedge.net/dotnet/WindowsDesktop/8.0.11/windowsdesktop-runtime-8.0.11-win-x64.exe"
#define WebView2UrlPrimary "https://go.microsoft.com/fwlink/p/?LinkId=2124703" ; Evergreen bootstrapper

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={commonpf64}\CMI Launcher
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=CMILauncher_InnoSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\CMILauncher.exe
SetupIconFile=..\CMILauncher\Resources\icon.ico
LicenseFile=license.txt

[Files]
; Aplikační soubory - přímá instalace bez MSI
Source: "..\CMILauncher\bin\Release\net8.0-windows\*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CMILauncher\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CMILauncher\bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CMILauncher\bin\Release\net8.0-windows\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CMILauncher\bin\Release\net8.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
; Offline support files (embedded, extracted programově přes ExtractTemporaryFile)
Source: "prereq\windowsdesktop-runtime-8.0.11-win-x64.exe"; Flags: ignoreversion dontcopy; Check: FileExists(ExpandConstant('{src}\prereq\windowsdesktop-runtime-8.0.11-win-x64.exe'))
; WebView2 installers (prefer full offline if provided; else small setup bootstrapper)
Source: "prereq\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; Flags: ignoreversion dontcopy skipifsourcedoesntexist; Check: FileExists(ExpandConstant('{src}\prereq\MicrosoftEdgeWebView2RuntimeInstallerX64.exe'))
Source: "prereq\MicrosoftEdgeWebview2Setup.exe"; Flags: ignoreversion dontcopy skipifsourcedoesntexist; Check: FileExists(ExpandConstant('{src}\prereq\MicrosoftEdgeWebview2Setup.exe'))

[Code]
// Download support using UrlMon (no external plugins needed)
function URLDownloadToFile(Caller: Integer; URL, FileName: String; Reserved: Integer; StatusCB: Integer): Integer;
  external 'URLDownloadToFileA@urlmon.dll stdcall';

var
  DotNetPath: string;
  WebView2Path: string;

function FindLocalPrereq(const FileName: string): string;
var
  srcDir, cand: string;
begin
  srcDir := ExtractFileDir(ExpandConstant('{srcexe}'));
  // same folder as installer
  cand := srcDir + '\\' + FileName;
  if FileExists(cand) then begin Result := cand; exit; end;
  // Prereqs/prereq subfolder next to installer (both variants)
  cand := srcDir + '\\Prereqs\\' + FileName;
  if FileExists(cand) then begin Result := cand; exit; end;
  cand := srcDir + '\\prereq\\' + FileName;
  if FileExists(cand) then begin Result := cand; exit; end;
  Result := '';
end;

function EnsureDownloaded(const URL, FileName: string): string;
var
  Dest: string;
  R: Integer;
begin
  Dest := ExpandConstant('{tmp}') + '\\' + FileName;
  R := URLDownloadToFile(0, URL, Dest, 0, 0);
  if R <> 0 then
    Result := ''
  else
    Result := Dest;
end;

function EnsurePrereq(const FileName, Url1, Url2: string): string;
var
  srcDir, localPath: string;
begin
  // Prefer a file placed next to installer EXE or in Prereqs subfolder (offline package scenario)
  localPath := FindLocalPrereq(FileName);
  if localPath <> '' then begin Result := localPath; exit; end;
  // Try primary download
  Result := EnsureDownloaded(Url1, FileName);
  if (Result = '') and (Url2 <> '') then
  begin
    // Try backup URL
    Result := EnsureDownloaded(Url2, FileName);
  end;
end;

function IsDotNetRuntimeInstalled(): Boolean;
var
  val: string;
begin
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x64\\sharedhost', 'Version', val) then
  begin
    Result := (val <> '');
  end else
    Result := False;
end;

function IsWebView2Installed(): Boolean;
var
  val: string;
begin
  // Check for Evergreen WebView2 runtime (standalone install)
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\\Microsoft\\EdgeWebView2\\Evergreen', 'Version', val) then
  begin
    if val <> '' then begin Result := True; exit; end;
  end;
  
  // Check for WebView2 bundled with Microsoft Edge
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', val) then
  begin
    if val <> '' then begin Result := True; exit; end;
  end;
  
  // Check for WebView2 in WOW6432Node (32-bit on 64-bit systems)
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\\WOW6432Node\\Microsoft\\EdgeWebView2\\Evergreen', 'Version', val) then
  begin
    if val <> '' then begin Result := True; exit; end;
  end;
  
  // Check alternative location for Edge WebView2
  if RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SOFTWARE\\WOW6432Node\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', val) then
  begin
    if val <> '' then begin Result := True; exit; end;
  end;
  
  Result := False;
end;

function ShouldInstallDotNet(): Boolean;
begin
  if IsDotNetRuntimeInstalled() then
  begin
    Result := False;
    exit;
  end;
  
  // .NET chybí, pokusíme se získat instalátor
  if DotNetPath = '' then
  begin
    // Poslední pokus o získání cesty během Check funkce
    DotNetPath := EnsurePrereq('{#DotNetInstaller}', '{#DotNetUrlPrimary}', '{#DotNetUrlBackup}');
  end;
  
  Result := (DotNetPath <> '');
  if not Result then
    MsgBox('Kritická chyba: Nelze získat .NET 8 Desktop Runtime instalátor. Aplikace nebude fungovat správně!', mbCriticalError, MB_OK);
end;

function ShouldInstallWebView2(): Boolean;
begin
  if IsWebView2Installed() then
  begin
    Result := False;
    exit;
  end;
  
  // WebView2 chybí, pokusíme se získat instalátor
  if WebView2Path = '' then
  begin
    // Poslední pokus o získání cesty během Check funkce
    WebView2Path := EnsurePrereq('MicrosoftEdgeWebView2RuntimeInstallerX64_BOOTSTRAPPER.exe', '{#WebView2UrlPrimary}', '');
  end;
  
  Result := (WebView2Path <> '');
  if not Result then
    MsgBox('Varování: Nelze získat WebView2 runtime instalátor. Pokuste se nainstalovat WebView2 ručně.', mbError, MB_OK);
end;

function GetDotNetPathVar(Param: string): string;
begin
  if DotNetPath <> '' then
    Result := DotNetPath
  else begin
    // Fallback - pokus o online stažení během instalace
    Result := EnsurePrereq('{#DotNetInstaller}', '{#DotNetUrlPrimary}', '{#DotNetUrlBackup}');
    if Result = '' then
      MsgBox('Kritická chyba: Nelze získat .NET 8 Desktop Runtime instalátor. Aplikace nebude fungovat správně!', mbCriticalError, MB_OK);
  end;
end;

function GetWebView2PathVar(Param: string): string;
begin
  if WebView2Path <> '' then
    Result := WebView2Path
  else begin
    // Fallback - pokus o online stažení během instalace
    Result := EnsurePrereq('MicrosoftEdgeWebView2RuntimeInstallerX64_BOOTSTRAPPER.exe', '{#WebView2UrlPrimary}', '');
    if Result = '' then
      MsgBox('Varování: Nelze získat WebView2 runtime instalátor. Pokuste se nainstalovat WebView2 ručně.', mbError, MB_OK);
  end;
end;

procedure TryExtractSupportFile(const FileName: string);
begin
  try
    ExtractTemporaryFile(FileName);
  except
    // ignore if not embedded
  end;
end;

procedure InitializeWizard;
begin
  // Připrav offline soubory do {tmp}, pokud jsou embedded jako support files
  TryExtractSupportFile('windowsdesktop-runtime-8.0.11-win-x64.exe');
  TryExtractSupportFile('MicrosoftEdgeWebView2RuntimeInstallerX64.exe');
  TryExtractSupportFile('MicrosoftEdgeWebview2Setup.exe');

  // Debug informace o detekci prerequisitů (pouze při interaktivní instalaci)
  if not WizardSilent then
  begin
    if IsDotNetRuntimeInstalled() then
      WizardForm.StatusLabel.Caption := WizardForm.StatusLabel.Caption + #13#10 + '✓ .NET 8 Desktop Runtime je již nainstalován'
    else
      WizardForm.StatusLabel.Caption := WizardForm.StatusLabel.Caption + #13#10 + '• Bude instalován .NET 8 Desktop Runtime';
      
    if IsWebView2Installed() then
      WizardForm.StatusLabel.Caption := WizardForm.StatusLabel.Caption + #13#10 + '✓ WebView2 runtime je již nainstalován'
    else
      WizardForm.StatusLabel.Caption := WizardForm.StatusLabel.Caption + #13#10 + '• Bude instalován WebView2 runtime';
  end;

  if not IsDotNetRuntimeInstalled() then
  begin
    // 1) Offline embedded (if included)
    if FileExists(ExpandConstant('{tmp}\\windowsdesktop-runtime-8.0.11-win-x64.exe')) then
      DotNetPath := ExpandConstant('{tmp}\\windowsdesktop-runtime-8.0.11-win-x64.exe')
    else
    begin
      // 2) Lokální soubor vedle setupu / v Prereqs
      DotNetPath := FindLocalPrereq('windowsdesktop-runtime-8.0.11-win-x64.exe');
      if DotNetPath = '' then
        // 3) Online stažení (aka.ms -> aktuální patch; záložní pevná verze)
        DotNetPath := EnsurePrereq('{#DotNetInstaller}', '{#DotNetUrlPrimary}', '{#DotNetUrlBackup}');
    end;
    if (DotNetPath = '') and (not WizardSilent) then
      MsgBox('Nelze stáhnout .NET 8 Desktop Runtime – zkontrolujte připojení k internetu nebo zkuste znovu. Pokračuji bez instalace runtime.', mbError, MB_OK);
  end;
  if not IsWebView2Installed() then
  begin
    // 1) Offline embedded plný instalátor (pokud je přiložen)
    if FileExists(ExpandConstant('{tmp}\\MicrosoftEdgeWebView2RuntimeInstallerX64.exe')) then
      WebView2Path := ExpandConstant('{tmp}\\MicrosoftEdgeWebView2RuntimeInstallerX64.exe')
    else if FileExists(ExpandConstant('{tmp}\\MicrosoftEdgeWebview2Setup.exe')) then
      // 2) Embedded bootstrapper (vyžaduje internet během instalace)
      WebView2Path := ExpandConstant('{tmp}\\MicrosoftEdgeWebview2Setup.exe')
    else
    begin
      // 3) Lokální plný instalátor vedle setupu / v Prereqs
      WebView2Path := FindLocalPrereq('MicrosoftEdgeWebView2RuntimeInstallerX64.exe');
      if WebView2Path = '' then
      begin
        // 4) Lokální bootstrapper vedle setupu / v Prereqs
        WebView2Path := FindLocalPrereq('MicrosoftEdgeWebview2Setup.exe');
        if WebView2Path = '' then
        begin
          // 5) Online stažení bootstrapperu z oficiálního fwlinku
          WebView2Path := EnsurePrereq('MicrosoftEdgeWebView2RuntimeInstallerX64_BOOTSTRAPPER.exe', '{#WebView2UrlPrimary}', '');
        end;
      end;
    end;
    if (WebView2Path = '') and (not WizardSilent) then
      MsgBox('Nelze stáhnout WebView2 runtime – zkontrolujte připojení k internetu nebo přiložte offline instalátor do stejné složky (nebo do podadresáře Prereqs/prereq) a zkuste znovu. Pokračuji bez instalace WebView2.', mbError, MB_OK);
  end;
end;

[Run]
; .NET Desktop Runtime (pokud chybí) – stáhne se do {tmp} nebo použije soubor vedle setupu
Filename: "{code:GetDotNetPathVar}"; Parameters: "/install /quiet /norestart"; Description: ".NET 8 Desktop Runtime"; StatusMsg: "Instaluji .NET 8 Desktop Runtime..."; Flags: runhidden waituntilterminated; Check: ShouldInstallDotNet

; WebView2 runtime (pokud chybí) – evergreen bootstrapper (online)
Filename: "{code:GetWebView2PathVar}"; Parameters: "/silent /install"; Description: "WebView2 runtime"; StatusMsg: "Instaluji WebView2 runtime..."; Flags: runhidden waituntilterminated; Check: ShouldInstallWebView2

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\CMILauncher.exe"; IconFilename: "{app}\CMILauncher.exe"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\CMILauncher.exe"; IconFilename: "{app}\CMILauncher.exe"

[UninstallRun]
; Tichá odinstalace MSI - vyžaduje ProductCode (nyní používáme soubor, raději vynecháme nebo doplníme ručně po zjištění GUID)
;Filename: "msiexec.exe"; Parameters: "/x {PRODUCT-CODE-GUID} /qn"; RunOnceId: "UninstallCMILauncherMsi"; Flags: runhidden waituntilterminated

[Messages]
SetupAppTitle=Instalace {#MyAppName}
SetupWindowTitle=Instalace {#MyAppName}

[Languages]
Name: "czech"; MessagesFile: "compiler:Languages\\Czech.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
