
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace CMILauncher
{
    public enum CertificateDialogResult
    {
        UseCertificate,
        ContinueWithoutCertificate,
        Cancel
    }

    public partial class MainWindow : Window
    {
        private const string BaseDir = "c:/iscmi";
        private const string LauncherUrl = "https://launcher.cmi.cz/app";
        private int navigationRetries = 0;
        private const int MaxRetries = 5;
        private int backoffIndex = 0;
        private readonly int[] backoffSeconds = new[] { 2, 5, 10, 15, 30 };
        private CancellationTokenSource? retryCts;
        private Storyboard? progressAnimation;
        private System.Threading.Timer? welcomeTimer;
        private bool certificateWorkflowCompleted = false;
        private bool fallbackTimeoutStarted = false;
        
        // Certificate dialog result tracking
        private TaskCompletionSource<CertificateDialogResult>? certificateDialogTcs = null;
        
        // Network change handling
        private int networkChangeRetries = 0;
        private const int MaxNetworkChangeRetries = 3;
        private System.Threading.Timer? networkRetryTimer;
        
        public MainWindow()
        {
            Debug.WriteLine("=== STARTING CMI LAUNCHER ===");
            InitializeComponent();
            Debug.WriteLine("InitializeComponent completed");
            
            // Adjust window size based on screen resolution
            AdjustWindowSize();
            
            StartWelcomeAnimation();
            Debug.WriteLine("StartWelcomeAnimation completed");
            InitializeWebView();
            Debug.WriteLine("InitializeWebView completed");
            // Povolit vlastní klávesové zkratky
            this.Focusable = true;
            this.Focus();
            Debug.WriteLine("=== CMI LAUNCHER INITIALIZATION COMPLETED ===");
        }

        private void AdjustWindowSize()
        {
            // Get the working area of the primary screen (excluding taskbar)
            var workingArea = SystemParameters.WorkArea;
            
            // Set maximum window size to 90% of screen height/width to ensure it fits
            double maxHeight = workingArea.Height * 0.9;
            double maxWidth = workingArea.Width * 0.9;
            
            // If desired height (800) is larger than max, reduce it
            if (this.Height > maxHeight)
            {
                this.Height = maxHeight;
            }
            
            // If desired width (800) is larger than max, reduce it
            if (this.Width > maxWidth)
            {
                this.Width = maxWidth;
            }
            
            // Update MinHeight/MinWidth if they exceed screen size
            if (this.MinHeight > maxHeight)
            {
                this.MinHeight = Math.Min(600, maxHeight);
            }
            
            if (this.MinWidth > maxWidth)
            {
                this.MinWidth = Math.Min(600, maxWidth);
            }
            
            Debug.WriteLine($"Screen working area: {workingArea.Width}x{workingArea.Height}");
            Debug.WriteLine($"Window size adjusted to: {this.Width}x{this.Height}");
        }

        private string? FindWebView2Runtime()
        {
            try
            {
                // Zkusit najít WebView2 Runtime v registru
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"))
                {
                    if (key != null)
                    {
                        var version = key.GetValue("pv") as string;
                        if (!string.IsNullOrEmpty(version))
                        {
                            var runtimePath = $@"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\{version}";
                            if (Directory.Exists(runtimePath))
                            {
                                Debug.WriteLine($"Found WebView2 Runtime: {runtimePath}");
                                return runtimePath;
                            }
                        }
                    }
                }
                
                // Zkusit 64-bit registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"))
                {
                    if (key != null)
                    {
                        var version = key.GetValue("pv") as string;
                        if (!string.IsNullOrEmpty(version))
                        {
                            var runtimePath = $@"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\{version}";
                            if (Directory.Exists(runtimePath))
                            {
                                Debug.WriteLine($"Found WebView2 Runtime: {runtimePath}");
                                return runtimePath;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding WebView2 Runtime: {ex.Message}");
            }
            
            return null;
        }

        private async void InitializeWebView()
        {
            try
            {
                // KRITICKÉ: Musíme vytvořit environment PŘED zavoláním EnsureCoreWebView2Async
                // jinak WebView2Loader.dll selže s "WebView2 Runtime není nainstalován"
                
                var webView2RuntimePath = FindWebView2Runtime();
                
                // DŮLEŽITÉ: Nastavit userDataFolder do složky s write přístupem
                // Defaultní umístění může způsobit E_ACCESSDENIED
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CMILauncher",
                    "WebView2"
                );
                
                // Vytvořit složku, pokud neexistuje
                Directory.CreateDirectory(userDataFolder);
                Debug.WriteLine($"Using userDataFolder: {userDataFolder}");
                
                CoreWebView2Environment? environment = null;
                
                if (!string.IsNullOrEmpty(webView2RuntimePath))
                {
                    Debug.WriteLine($"Using WebView2 Runtime from: {webView2RuntimePath}");
                    try
                    {
                        environment = await CoreWebView2Environment.CreateAsync(
                            webView2RuntimePath,
                            userDataFolder
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create environment with explicit path: {ex.Message}");
                        // Zkusit fallback na default s user data folder
                        environment = null;
                    }
                }
                
                if (environment == null)
                {
                    Debug.WriteLine("Using default WebView2 Runtime detection with custom userDataFolder");
                    environment = await CoreWebView2Environment.CreateAsync(
                        null,
                        userDataFolder
                    );
                }
                
                // Přidat timeout pro WebView2 inicializaci (30 sekund)
                var webViewInitTask = webView.EnsureCoreWebView2Async(environment);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var completedTask = await Task.WhenAny(webViewInitTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("WebView2 initialization timed out after 30 seconds");
                }
                
                await webViewInitTask; // Zajistit, že skutečně dokončíme inicializaci
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                
                // Povolit DevTools pouze pro programové otevření (blokneme F12)
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // F12 zablokujeme ručně, okno otevře Ctrl+D
                
                // KRITICKÉ: Zakázat Chromium error pages - chceme vlastní handling
                webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                
                // Vytvořit adresář pro aplikace, pokud neexistuje
                Directory.CreateDirectory(BaseDir);
                
                // Přidat JavaScript bridge pro IPC komunikaci
                webView.CoreWebView2.AddHostObjectToScript("electronBridge", new ElectronBridge(this));
                
                // DŮLEŽITÉ: Inject electron API PŘED načtením jakéhokoli scriptu
                // Použijeme AddScriptToExecuteOnDocumentCreatedAsync místo NavigationCompleted
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function() {
                        // Callback registry pro IPC odpovědi
                        window.ipcCallbacks = {};
                        window.canLaunchCallbacks = {};
                        
                        // Simulace electron API - musí být k dispozici PŘED načtením app scriptu
                        window.require = function(module) {
                            if (module === 'electron') {
                                return {
                                    ipcRenderer: {
                                        send: function(channel, ...args) {
                                            console.log('IPC send:', channel, args);
                                            
                                            // Pro canLaunch zaregistrovat callback
                                            if (channel === 'canLaunch' && args.length >= 2) {
                                                const appId = args[0];
                                                const env = args[1];
                                                window.canLaunchCallbacks[appId + '_' + env] = function(status) {
                                                    // Emulovat event callback
                                                    const callbacks = window.ipcCallbacks['canLaunchResult'] || [];
                                                    callbacks.forEach(cb => {
                                                        try {
                                                            cb({}, appId, env, status);
                                                        } catch (e) {
                                                            console.error('Error in canLaunchResult callback:', e);
                                                        }
                                                    });
                                                };
                                            }
                                            
                                            window.chrome.webview.hostObjects.electronBridge.Send(channel, JSON.stringify(args));
                                        },
                                        on: function(channel, callback) {
                                            console.log('IPC on:', channel);
                                            if (!window.ipcCallbacks[channel]) {
                                                window.ipcCallbacks[channel] = [];
                                            }
                                            window.ipcCallbacks[channel].push(callback);
                                        },
                                        sendSync: function(channel, ...args) {
                                            console.log('IPC sendSync:', channel, args);
                                            return window.chrome.webview.hostObjects.sync.electronBridge.SendSync(channel, JSON.stringify(args));
                                        }
                                    }
                                };
                            }
                            return null;
                        };
                        
                        console.log('✓ Electron API injected - require is available');
                    })();
                ");

                // Klientský certifikát s možností volby "bez certifikátu"
                webView.CoreWebView2.ClientCertificateRequested += (s, e) =>
                {
                    try
                    {
                        // **KRITICKÉ: Ihned nastavit Handled = true aby WebView2 nezobrazoval svůj dialog**
                        e.Handled = true;
                        Debug.WriteLine("Certificate request handled - zabráněno WebView2 dialogu");
                        
                        var certs = e.MutuallyTrustedCertificates;
                        Debug.WriteLine($"Client certificate requested. Found {certs.Count} certificates.");

                        // **VYLEPŠENO: Automaticky pokračovat bez certifikátu pokud není USB token**
                        if (certs.Count == 0)
                        {
                            e.Cancel = false; // Pokračovat bez certifikátu
                            Debug.WriteLine("No client certificates available (USB token not inserted) - automatically continuing without certificate.");
                            
                            certificateWorkflowCompleted = true;
                            Dispatcher.Invoke(() => UpdateWelcomeMessage("USB token není vložen - pokračuji bez certifikátu..."));
                            return;
                        }

                        // **VŽDY ZOBRAZIT DIALOG: Nechať užiťateľa rozhodnúť o certifikáte**
                        bool hasAccessibleCerts = true; // Vždy true - zobrazíme dialog
                        var accessibleCerts = new List<Microsoft.Web.WebView2.Core.CoreWebView2ClientCertificate>();
                        
                        Debug.WriteLine($"Found {certs.Count} certificates in Windows store, will always show dialog for user choice");
                        
                        // **JEDNODUCHÉ RIEŠENIE: Pridáme všetky nájdené certifikáty**
                        foreach (var cert in certs)
                        {
                            try
                            {
                                var subject = cert.Subject ?? string.Empty;
                                var name = cert.DisplayName ?? string.Empty;
                                var kind = cert.Kind;
                                
                                Debug.WriteLine($"Adding certificate: {name}");
                                Debug.WriteLine($"Subject: {subject}");
                                Debug.WriteLine($"Kind: {kind}");
                                
                                // Pridáme každý certifikát - užívateľ si vyberie
                                accessibleCerts.Add(cert);
                                Debug.WriteLine($"Certificate {name} added to list");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Certificate processing error: {ex.Message}");
                            }
                        }

                        // Pokud žádný certifikát není skutečně dostupný, pokračovat bez něj
                        if (!hasAccessibleCerts || accessibleCerts.Count == 0)
                        {
                            e.Cancel = false;
                            Debug.WriteLine("No accessible certificates found (USB token not available) - automatically continuing without certificate.");
                            certificateWorkflowCompleted = true;
                            Dispatcher.Invoke(() => UpdateWelcomeMessage("USB token není dostupný - pokračuji bez certifikátu..."));
                            return;
                        }
                        
                        Debug.WriteLine($"Found {accessibleCerts.Count} accessible certificates, proceeding with dialog");

                        // **VYLEPŠENO: Najít commercial certifikát pouze z dostupných certifikátů**
                        Microsoft.Web.WebView2.Core.CoreWebView2ClientCertificate? commercialCert = null;
                        foreach (var c in accessibleCerts) // Použít pouze dostupné certifikáty
                        {
                            var subj = c.Subject ?? string.Empty;
                            var iss = c.Issuer ?? string.Empty;
                            var name = c.DisplayName ?? string.Empty;
                            if (subj.Contains("commercial", StringComparison.OrdinalIgnoreCase) ||
                                subj.Contains("komer", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("commercial", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("komer", StringComparison.OrdinalIgnoreCase) ||
                                iss.Contains("commercial", StringComparison.OrdinalIgnoreCase) ||
                                iss.Contains("komer", StringComparison.OrdinalIgnoreCase))
                            {
                                commercialCert = c;
                                break;
                            }
                        }

                        // SPRÁVNÝ ASYNCHRONNÍ PATTERN - bez Task.Run()
                        try 
                        {
                            Debug.WriteLine("Starting async certificate dialog...");
                            
                            // **KLÍČOVÉ ŘEŠENÍ: Skrýt WebView2 před zobrazením dialogu**
                            if (webView != null)
                            {
                                webView.Visibility = Visibility.Collapsed;
                                Debug.WriteLine("WebView2 skryto pro certificate dialog");
                            }
                            
                            // Nastavit informace o certifikátu
                            if (commercialCert != null)
                            {
                                CertDialogMessage.Text = $"Nalezený certifikát: {commercialCert.DisplayName}";
                                CertDialogSubtitle.Text = $"Komerční certifikát pro přihlášení ({commercialCert.Subject})";
                            }
                            else
                            {
                                CertDialogMessage.Text = $"Nalezeno {accessibleCerts.Count} dostupných certifikátů";
                                CertDialogSubtitle.Text = "Žádný komerční certifikát nenalezen";
                            }
                            
                            // Reset TaskCompletionSource
                            certificateDialogTcs = new TaskCompletionSource<CertificateDialogResult>();
                            
                            // DŮLEŽITÉ: Skrýt WelcomeScreen aby byl vidět dialog!
                            WelcomeScreen.Visibility = Visibility.Collapsed;
                            
                            // Show dialog immediately
                            CertificateDialog.Visibility = Visibility.Visible;
                            Debug.WriteLine("WPF dialog shown, WelcomeScreen hidden, using async pattern");
                            
                            // Create deferral to handle async response ON UI THREAD
                            var deferral = e.GetDeferral();
                            
                            // Handle dialog result asynchronously but complete on UI thread
                            _ = Task.Run(async () => 
                            {
                                try 
                                {
                                    var dialogResult = await certificateDialogTcs.Task;
                                    Debug.WriteLine($"Async dialog result: {dialogResult}");
                                    
                                    // Use Dispatcher.BeginInvoke to avoid blocking
                                    Dispatcher.BeginInvoke(() => {
                                        try
                                        {
                                            switch (dialogResult)
                                            {
                                                case CertificateDialogResult.UseCertificate:
                                                    if (commercialCert != null)
                                                    {
                                                        e.SelectedCertificate = commercialCert;
                                                        e.Cancel = false;
                                                        Debug.WriteLine($"Selected certificate: {commercialCert.DisplayName}");
                                                    }
                                                    else if (accessibleCerts.Count > 0)
                                                    {
                                                        e.SelectedCertificate = accessibleCerts[0];
                                                        e.Cancel = false;
                                                        Debug.WriteLine($"Using fallback certificate: {accessibleCerts[0].DisplayName}");
                                                    }
                                                    else
                                                    {
                                                        e.Cancel = false;
                                                        Debug.WriteLine("No accessible certificates available, continuing without certificate");
                                                    }
                                                    break;
                                                    
                                                case CertificateDialogResult.ContinueWithoutCertificate:
                                                    e.Cancel = false;
                                                    Debug.WriteLine("Continue without certificate");
                                                    break;
                                                    
                                                case CertificateDialogResult.Cancel:
                                                    e.Cancel = true;
                                                    Debug.WriteLine("Certificate selection cancelled");
                                                    break;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Deferral completion error: {ex.Message}");
                                            e.Cancel = false; // Safe fallback
                                        }
                                        finally
                                        {
                                            try
                                            {
                                                // **KRITICKÉ: Obnovit WebView2 po dokončení certificate dialogu**
                                                if (webView != null)
                                                {
                                                    webView.Visibility = Visibility.Visible;
                                                    Debug.WriteLine("WebView2 obnoven po certificate dialogu");
                                                }
                                                
                                                deferral.Complete();
                                                Debug.WriteLine("Deferral completed successfully");
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Deferral.Complete() error: {ex.Message}");
                                            }
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Task.Run dialog error: {ex.Message}");
                                    Dispatcher.BeginInvoke(() => {
                                        // **KRITICKÉ: Obnovit WebView2 i při chybě**
                                        if (webView != null)
                                        {
                                            webView.Visibility = Visibility.Visible;
                                            Debug.WriteLine("WebView2 obnoven při chybě certificate dialogu");
                                        }
                                        
                                        e.Cancel = false; // Continue without cert
                                        deferral.Complete();
                                    });
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Dialog setup error: {ex.Message}");
                            // **VYLEPŠENO: Fallback používá dostupné certifikáty**
                            if (commercialCert != null)
                            {
                                e.SelectedCertificate = commercialCert;
                            }
                            else if (accessibleCerts.Count > 0)
                            {
                                e.SelectedCertificate = accessibleCerts[0];
                            }
                            e.Cancel = false;
                        }
                        
                        certificateWorkflowCompleted = true;
                        Debug.WriteLine("Certificate workflow started with async WPF dialog");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ClientCertificateRequested handler error: {ex.Message}");
                        e.Handled = true;
                        e.Cancel = false; // V případě chyby pokračovat bez certifikátu
                    }
                };

                // Logging pro debug
                webView.CoreWebView2.NavigationStarting += (s, e) =>
                {
                    Debug.WriteLine($"Navigation starting: {e.Uri}");
                };
                
                webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    Debug.WriteLine($"Navigation completed: IsSuccess={e.IsSuccess}, WebErrorStatus={e.WebErrorStatus}");
                    WebView_NavigationCompleted(s, e);
                };

                webView.CoreWebView2.ProcessFailed += (s, e) =>
                {
                    Debug.WriteLine($"WebView2 process failed: {e.ProcessFailedKind}");
                    ShowRetryOverlay($"Vnitřní chyba prohlížeče ({e.ProcessFailedKind}). Zkouším obnovit...", 0);
                    ScheduleAutoRetry(immediate: true);
                };
                
                webView.CoreWebView2.DOMContentLoaded += (s, e) =>
                {
                    Debug.WriteLine("DOM content loaded");
                };
                
                // Sledování změn title - může indikovat autentizaci nebo load stavu
                webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    try
                    {
                        var title = webView.CoreWebView2.DocumentTitle ?? "";
                        Debug.WriteLine($"Document title changed: {title}");
                        
                        // ÚPLNĚ ZAKÁZAT automatické skrývání na základě title changes!
                        // Welcome screen zůstane viditelný až do explicitního skrytí v ShowCertificateDialog
                        // Celý tento blok byl zcela odstraněn pro eliminaci automatického skrývání
                        
                        Debug.WriteLine($"Title changed but welcome screen will remain visible: {title}");
                        
                        // ÚPLNĚ ZAKÁZAT fallback timeout! Welcome screen zůstane až do manuálního skrytí
                        // Spustit fallback timeout jen jednou - ÚPLNĚ ZAKÁZÁNO
                        if (false) // ÚPLNĚ zakázáno - bez fallback timeoutu
                        {
                            // fallbackTimeoutStarted = true;
                            // Task.Run(async () =>
                            // {
                            //     await Task.Delay(15000); // Zvětšit timeout na 15 sekund
                            //     if (!certificateWorkflowCompleted)
                            //     {
                            //         Dispatcher.Invoke(() =>
                            //         {
                            //             Debug.WriteLine("Fallback timeout triggered - hiding welcome screen");
                            //             UpdateWelcomeMessage("Dokončuji načítání...");
                            //             StartWelcomeHideTimer();
                            //             certificateWorkflowCompleted = true;
                            //         });
                            //     }
                            // });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DocumentTitleChanged error: {ex.Message}");
                    }
                };
                
                // Console message logging
                webView.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    Debug.WriteLine($"Web message: {e.WebMessageAsJson}");
                };
                
                // Explicitně navigovat na launcher URL (XAML Source může selhat)
                Debug.WriteLine("Navigating to launcher.cmi.cz/app");
                webView.CoreWebView2.Navigate(LauncherUrl);
                
                Title = "ČMI Launcher";
            }
            catch (Exception ex)
            {
                var message = "Chyba při inicializaci WebView2.\n\n";
                
                // Podrobné logování pro diagnostiku
                var logPath = Path.Combine(Path.GetTempPath(), "CMILauncher_Error.log");
                try
                {
                    File.WriteAllText(logPath, $@"
=== CMI Launcher Error Log ===
Time: {DateTime.Now}
Exception: {ex.GetType().Name}
Message: {ex.Message}
HResult: 0x{ex.HResult:X8}
Stack: {ex.StackTrace}

WebView2 Runtime Check:
{FindWebView2RuntimeLog()}
");
                }
                catch { }
                
                if (ex.HResult == unchecked((int)0x80070005)) // E_ACCESSDENIED
                {
                    message += "WebView2 Runtime byl nedávno nainstalován.\n" +
                              "Prosím RESTARTUJTE počítač a zkuste aplikaci spustit znovu.\n\n";
                }
                else if (ex.HResult == unchecked((int)0x80070002)) // File not found
                {
                    message += "WebView2 Runtime nebyl nalezen.\n" +
                              "Systém hlásí, že soubory chybí.\n\n";
                }
                else
                {
                    message += $"Detaily: {ex.Message}\n";
                    message += $"Kód chyby: 0x{ex.HResult:X8}\n\n";
                }
                
                message += "Ujistěte se, že máte nainstalovaný Microsoft Edge WebView2 Runtime.\n" +
                          "Stáhněte z: https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                          $"Log uložen do: {logPath}";
                
                MessageBox.Show(
                    message,
                    "Chyba inicializace WebView2",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Application.Current.Shutdown();
            }
        }

        private string FindWebView2RuntimeLog()
        {
            var log = "";
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"))
                {
                    if (key != null)
                    {
                        var version = key.GetValue("pv") as string;
                        log += $"WOW6432 Registry: {version}\n";
                        if (!string.IsNullOrEmpty(version))
                        {
                            var path = $@"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\{version}";
                            log += $"Path exists: {Directory.Exists(path)}\n";
                        }
                    }
                    else
                    {
                        log += "WOW6432 Registry: Not found\n";
                    }
                }
            }
            catch (Exception ex)
            {
                log += $"Registry error: {ex.Message}\n";
            }
            return log;
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Debug.WriteLine($"Navigation failed: {e.WebErrorStatus}");
                
                // Detekce všech connection errorů - tiché obnovení
                // Všechny tyto chyby mohou být přechodné (změna sítě, VPN, atd.)
                if (e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted ||
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.Disconnected ||
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.Timeout ||
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.ServerUnreachable ||
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.CannotConnect ||
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.HostNameNotResolved)
                {
                    var connErrorMessage = e.WebErrorStatus switch
                    {
                        CoreWebView2WebErrorStatus.ConnectionAborted => "Spojení přerušeno",
                        CoreWebView2WebErrorStatus.Disconnected => "Odpojeno od sítě",
                        CoreWebView2WebErrorStatus.Timeout => "Vypršel časový limit připojení",
                        CoreWebView2WebErrorStatus.ServerUnreachable => "Server není dostupný",
                        CoreWebView2WebErrorStatus.CannotConnect => "Nelze se připojit k serveru",
                        CoreWebView2WebErrorStatus.HostNameNotResolved => "Nelze najít server launcher.cmi.cz",
                        _ => "Chyba připojení"
                    };
                    
                    Debug.WriteLine($"Connection error detected: {connErrorMessage}, attempting silent reload...");
                    HandleNetworkChangeError(connErrorMessage);
                    return;
                }
                
                // Ostatní chyby (neočekávané) - zobrazit rovnou chybu
                var errorMessage = $"Chyba při načítání stránky: {e.WebErrorStatus}";
                
                navigationRetries++;
                if (navigationRetries <= MaxRetries)
                {
                    ShowRetryOverlay($"{errorMessage}.", 0);
                    ScheduleAutoRetry();
                }
                else
                {
                    ShowErrorInWelcomeScreen($"{errorMessage}. Zkontrolujte připojení k internetu.");
                }
            }
            else
            {
                // Reset retry counter při úspěšné navigaci
                navigationRetries = 0;
                backoffIndex = 0;
                networkChangeRetries = 0;
                retryCts?.Cancel();
                retryCts = null;
                HideRetryOverlay();
                
                // Aktualizovat welcome screen - stránka se načetla
                UpdateWelcomeMessage("Připojeno! Načítám aplikaci...");
                
                // NEAUTOMATICKY skrývat welcome screen - počkat na dokončení certificate workflow
                // StartWelcomeHideTimer(); // Zakomentováno - bude se spouštět po certificate volbě
                
                Debug.WriteLine("Navigation succeeded");
            }
        }

        private void ScheduleAutoRetry(bool immediate = false)
        {
            try
            {
                retryCts?.Cancel();
                retryCts = new CancellationTokenSource();
                var token = retryCts.Token;

                int delaySec = immediate ? 0 : backoffSeconds[Math.Min(backoffIndex, backoffSeconds.Length - 1)];
                backoffIndex = Math.Min(backoffIndex + 1, backoffSeconds.Length - 1);
                navigationRetries = Math.Min(navigationRetries + 1, MaxRetries);

                UpdateRetryOverlayCountdown(delaySec);

                Task.Run(async () =>
                {
                    try
                    {
                        if (delaySec > 0)
                            await Task.Delay(TimeSpan.FromSeconds(delaySec), token);

                        if (!token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (webView?.CoreWebView2 != null)
                                {
                                    Debug.WriteLine("Auto-retry navigating to launcher...");
                                    webView.CoreWebView2.Navigate(LauncherUrl);
                                }
                            });
                        }
                    }
                    catch (TaskCanceledException) { }
                }, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScheduleAutoRetry error: {ex.Message}");
            }
        }

        private void ShowRetryOverlay(string message, int inSeconds)
        {
            Dispatcher.Invoke(() =>
            {
                if (RetryOverlay != null)
                {
                    RetryOverlay.Visibility = Visibility.Visible;
                    if (RetryMessage != null)
                    {
                        RetryMessage.Text = inSeconds > 0 ? $"{message} Zkusím to znovu za {inSeconds}s." : message;
                    }
                }
            });
        }

        private void UpdateRetryOverlayCountdown(int inSeconds)
        {
            if (inSeconds <= 0) return;
            Task.Run(async () =>
            {
                for (int i = inSeconds; i > 0; i--)
                {
                    if (retryCts?.IsCancellationRequested == true) break;
                    Dispatcher.Invoke(() =>
                    {
                        if (RetryMessage != null)
                            RetryMessage.Text = $"Nelze se připojit. Zkusím to znovu za {i}s...";
                    });
                    try { await Task.Delay(1000, retryCts?.Token ?? CancellationToken.None); } catch { break; }
                }
            });
        }

        private void HideRetryOverlay()
        {
            Dispatcher.Invoke(() =>
            {
                if (RetryOverlay != null)
                    RetryOverlay.Visibility = Visibility.Collapsed;
            });
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                retryCts?.Cancel();
                backoffIndex = 0;
                navigationRetries = 0;
                if (webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Reload();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Manual retry error: {ex.Message}");
            }
        }

        public void LaunchApp(string installDir, string command, string[] parameters)
        {
            try
            {
                // Rozdělit command pokud obsahuje |  (fallback commands)
                var commands = command.Split('|');
                
                foreach (var cmd in commands)
                {
                    string fullPath;
                    
                    if (Path.IsPathRooted(cmd))
                    {
                        fullPath = cmd;
                    }
                    else if (cmd.StartsWith("$HOME"))
                    {
                        fullPath = cmd.Replace("$HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    }
                    else
                    {
                        fullPath = Path.Combine(BaseDir, installDir, cmd);
                    }

                    if (File.Exists(fullPath))
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = fullPath,
                            Arguments = string.Join(" ", parameters),
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(fullPath)
                        };
                        
                        Process.Start(startInfo);
                        Debug.WriteLine($"Launched: {fullPath}");
                        return;
                    }
                }
                
                MessageBox.Show($"Aplikace nebyla nalezena: {command}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při spouštění aplikace: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenExternal(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při otevírání URL: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Globální zachycení kláves: Ctrl+D povolí DevTools, F12 blokováno
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                // Blokovat F12 (standardní DevTools)
                if (e.Key == System.Windows.Input.Key.F12)
                {
                    e.Handled = true;
                    return;
                }

                // Povolit otevření DevTools přes Ctrl+D
                if (e.Key == System.Windows.Input.Key.D && (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl)))
                {
                    if (webView?.CoreWebView2 != null)
                    {
                        try
                        {
                            webView.CoreWebView2.OpenDevToolsWindow();
                        }
                        catch (Exception devEx)
                        {
                            Debug.WriteLine($"DevTools otevření selhalo: {devEx.Message}");
                        }
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chyba v PreviewKeyDown: {ex.Message}");
            }
        }

        // Welcome screen animace a řízení
        private void StartWelcomeAnimation()
        {
            try
            {
                Debug.WriteLine("=== STARTING WELCOME ANIMATION ===");
                
                // ZAJISTIT, že welcome screen je viditelný a na vrcholu
                if (WelcomeScreen != null)
                {
                    Debug.WriteLine("Setting WelcomeScreen as visible and topmost");
                    WelcomeScreen.Visibility = Visibility.Visible;
                    System.Windows.Controls.Panel.SetZIndex(WelcomeScreen, 9999); // Nejvyšší z-index
                    
                    // Ujistit se, že WebView2 je pod welcome screen
                    if (webView != null)
                    {
                        System.Windows.Controls.Panel.SetZIndex(webView, 1);
                    }
                }
                else
                {
                    Debug.WriteLine("ERROR: WelcomeScreen is null!");
                }
                
                UpdateWelcomeMessage("Inicializuji WebView2 runtime...");
                AnimateProgressBar();
                Debug.WriteLine("=== WELCOME ANIMATION STARTED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Welcome animation error: {ex.Message}");
            }
        }

        private void UpdateWelcomeMessage(string message)
        {
            if (WelcomeMessage != null)
            {
                WelcomeMessage.Text = message;
            }
        }

        private void AnimateProgressBar()
        {
            try
            {
                if (ProgressBar != null)
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 300, // Šířka progress baru
                        Duration = TimeSpan.FromSeconds(8),
                        RepeatBehavior = RepeatBehavior.Forever,
                        AutoReverse = false
                    };
                    
                    progressAnimation = new Storyboard();
                    Storyboard.SetTarget(animation, ProgressBar);
                    Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));
                    progressAnimation.Children.Add(animation);
                    progressAnimation.Begin();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Progress animation error: {ex.Message}");
            }
        }

        private async void HideWelcomeScreenAsync(int delaySeconds = 2)
        {
            try
            {
                UpdateWelcomeMessage("Úspěšně připojeno!");
                
                // Počkej pár sekund, než welcome screen zmizí
                await Task.Delay(delaySeconds * 1000);
                
                Debug.WriteLine("Hiding welcome screen...");
                if (WelcomeScreen != null)
                {
                    WelcomeScreen.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("Welcome screen hidden.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding welcome screen: {ex.Message}");
            }
        }

        private void StartWelcomeHideTimer()
        {
            try
            {
                // ÚPLNĚ ZAKÁZÁNO - žádné automatické skrývání welcome screen
                // Welcome screen bude skryt pouze explicitně po dokončení certificate workflow
                Debug.WriteLine("StartWelcomeHideTimer called but DISABLED - welcome screen will remain visible");
                
                // Zrušit existující timer pokud existuje
                welcomeTimer?.Dispose();
                
                // NEKLIKAT žádný timer - welcome screen zůstává viditelný
                // welcomeTimer = new System.Threading.Timer(...)  <- ODSTRANĚNO
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartWelcomeHideTimer error: {ex.Message}");
                // NEAUTOMATICKY skrývat - pouze logovat chybu
                // HideWelcomeScreenAsync(0); <- ODSTRANĚNO
            }
        }

        private Task<CertificateDialogResult> ShowCertificateDialog(CoreWebView2ClientCertificate? commercialCert, int totalCertCount)
        {
            return Dispatcher.Invoke(() =>
            {
                try
                {
                    // Prepare dialog content
                    string certInfo = "";
                    string subtitle = "";
                    
                    if (commercialCert != null)
                    {
                        certInfo = $"Nalezený certifikát: {commercialCert.DisplayName ?? commercialCert.Subject}";
                        subtitle = "Nalezen komerční certifikát pro přihlášení";
                    }
                    else
                    {
                        certInfo = $"Nalezeno {totalCertCount} certifikátů, ale žádný komerční.";
                        subtitle = "Certifikáty k dispozici, ale žádný komerční";
                    }
                    
                    // Update dialog content
                    CertDialogSubtitle.Text = subtitle;
                    CertDialogMessage.Text = certInfo;
                    
                    // Create TaskCompletionSource for async result
                    certificateDialogTcs = new TaskCompletionSource<CertificateDialogResult>();
                    
                    // Show custom dialog
                    CertificateDialog.Visibility = Visibility.Visible;
                    
                    Debug.WriteLine("Custom certificate dialog shown");
                    
                    return certificateDialogTcs.Task;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ShowCertificateDialog error: {ex.Message}");
                    // V případě chyby pokračovat bez certifikátu
                    return Task.FromResult(CertificateDialogResult.ContinueWithoutCertificate);
                }
            });
        }
        
        // Synchronní verze certificate dialogu pro použití v WebView2 certificate request handleru
        private CertificateDialogResult ShowCertificateDialogSync(Microsoft.Web.WebView2.Core.CoreWebView2ClientCertificate? commercialCert, int totalCerts)
        {
            try
            {
                Debug.WriteLine($"ShowCertificateDialogSync called with cert: {commercialCert?.DisplayName ?? "null"}, total: {totalCerts}");
                
                // KRITICKÉ: Nastavit informace PŘED zobrazením
                if (commercialCert != null)
                {
                    CertDialogMessage.Text = $"Nalezený certifikát: {commercialCert.DisplayName}";
                    CertDialogSubtitle.Text = $"Komerční certifikát ({commercialCert.Subject})";
                }
                else
                {
                    CertDialogMessage.Text = $"Nalezeno {totalCerts} certifikátů v úložišti";
                    CertDialogSubtitle.Text = "Žádný komerční certifikát nenalezen";
                }
                
                // Reset TaskCompletionSource
                certificateDialogTcs = new TaskCompletionSource<CertificateDialogResult>();
                
                // KRITICKÉ: Zajistit že dialog je nad welcome screen
                Debug.WriteLine($"Before: CertificateDialog.Visibility = {CertificateDialog.Visibility}");
                Debug.WriteLine($"Before: WelcomeScreen.Visibility = {WelcomeScreen.Visibility}");
                
                // Zobrazit dialog s highest priority
                CertificateDialog.Visibility = Visibility.Visible;
                
                // Force refresh UI
                this.UpdateLayout();
                
                Debug.WriteLine($"After: CertificateDialog.Visibility = {CertificateDialog.Visibility}");
                Debug.WriteLine("Custom certificate dialog shown with Z-Index 1000");
                
                // Počkáme na výsledek synchronně
                var result = certificateDialogTcs.Task.GetAwaiter().GetResult();
                Debug.WriteLine($"Certificate dialog result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowCertificateDialogSync error: {ex.Message}");
                // V případě chyby pokračovat bez certifikátu
                return CertificateDialogResult.ContinueWithoutCertificate;
            }
        }
        
        // Certificate Dialog Event Handlers
        private void UseCertificateButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("UseCertificateButton_Click triggered");
            Debug.WriteLine("UseCertificateButton_Click triggered");
            ProcessCertificateChoice(CertificateDialogResult.UseCertificate);
        }
        
        private void UseCertificateButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("UseCertificateButton_MouseDown triggered");
            ProcessCertificateChoice(CertificateDialogResult.UseCertificate);
            e.Handled = true;
        }
        
        private void ContinueWithoutCertButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("ContinueWithoutCertButton_Click triggered");
            Debug.WriteLine("ContinueWithoutCertButton_Click triggered");
            ProcessCertificateChoice(CertificateDialogResult.ContinueWithoutCertificate);
        }
        
        private void ContinueWithoutCertButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("ContinueWithoutCertButton_MouseDown triggered");
            ProcessCertificateChoice(CertificateDialogResult.ContinueWithoutCertificate);
            e.Handled = true;
        }
        
        private void CancelCertButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("CancelCertButton_Click triggered");
            Debug.WriteLine("CancelCertButton_Click triggered");
            ProcessCertificateChoice(CertificateDialogResult.Cancel);
        }
        
        private void CancelCertButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("CancelCertButton_MouseDown triggered");
            ProcessCertificateChoice(CertificateDialogResult.Cancel);
            e.Handled = true;
        }
        
        private void ProcessCertificateChoice(CertificateDialogResult result)
        {
            Debug.WriteLine($"ProcessCertificateChoice: {result}");
            CertificateDialog.Visibility = Visibility.Collapsed;
            
            switch (result)
            {
                case CertificateDialogResult.UseCertificate:
                    // Zobrazit welcome screen s informací o přihlašování
                    WelcomeScreen.Visibility = Visibility.Visible;
                    UpdateWelcomeMessage("Přihlašuji s certifikátem...");
                    // Welcome screen se skryje automaticky po načtení webu v WebView_NavigationCompleted
                    break;
                case CertificateDialogResult.ContinueWithoutCertificate:
                    // Zobrazit welcome screen s informací o přihlašování
                    WelcomeScreen.Visibility = Visibility.Visible;
                    UpdateWelcomeMessage("Přihlašuji bez certifikátu...");
                    // Welcome screen se skryje automaticky po načtení webu v WebView_NavigationCompleted
                    break;
                case CertificateDialogResult.Cancel:
                    UpdateWelcomeMessage("Ruším připojení...");
                    // Ukončit aplikaci po kliknutí na Zrušit
                    Application.Current.Shutdown();
                    break;
            }
            
            certificateDialogTcs?.SetResult(result);
        }
        
        private void HandleNetworkChangeError(string errorMessage)
        {
            if (networkChangeRetries >= MaxNetworkChangeRetries)
            {
                Debug.WriteLine($"Max network change retries ({MaxNetworkChangeRetries}) reached");
                ShowErrorInWelcomeScreen(
                    $"{errorMessage}. Zkontrolujte prosím síťové připojení a klikněte na tlačítko níže."
                );
                networkChangeRetries = 0;
                return;
            }
            
            networkChangeRetries++;
            Debug.WriteLine($"Network change retry attempt {networkChangeRetries}/{MaxNetworkChangeRetries} - {errorMessage}");
            
            // Krátká pauza před reload (500ms, 1s, 2s)
            int[] delays = new[] { 500, 1000, 2000 };
            int delay = delays[Math.Min(networkChangeRetries - 1, delays.Length - 1)];
            
            // Naplánovat reload
            networkRetryTimer?.Dispose();
            networkRetryTimer = new System.Threading.Timer(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Debug.WriteLine($"Executing silent reload after network change (attempt {networkChangeRetries})");
                        webView?.CoreWebView2?.Reload();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during silent reload: {ex.Message}");
                        ShowErrorInWelcomeScreen($"{errorMessage}. Chyba při obnovování připojení.");
                    }
                });
            }, null, delay, Timeout.Infinite);
        }
        
        private void ShowErrorInWelcomeScreen(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Zobrazit welcome screen
                    if (WelcomeScreen != null)
                    {
                        WelcomeScreen.Visibility = Visibility.Visible;
                    }
                    
                    // Změnit text na chybovou zprávu
                    if (WelcomeMessage != null)
                    {
                        WelcomeMessage.Text = errorMessage;
                        WelcomeMessage.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Červená barva
                    }
                    
                    // Skrýt progress bar
                    if (ProgressBar != null)
                    {
                        ProgressBar.Visibility = Visibility.Collapsed;
                    }
                    if (ProgressBackground != null)
                    {
                        ProgressBackground.Visibility = Visibility.Collapsed;
                    }
                    
                    // Zastavit animaci
                    progressAnimation?.Stop();
                    
                    // Zobrazit nebo vytvořit Retry tlačítko
                    ShowRetryButton();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error showing error in welcome screen: {ex.Message}");
                }
            });
        }
        
        private void ShowRetryButton()
        {
            try
            {
                // Najít StackPanel v welcome screen
                if (WelcomeScreen == null) return;
                
                var border = WelcomeScreen.FindName("WelcomeBorder") as Border;
                var welcomePanel = border?.Child as StackPanel;
                
                if (welcomePanel == null)
                {
                    // Fallback - hledat přímo v children
                    foreach (var child in LogicalTreeHelper.GetChildren(WelcomeScreen))
                    {
                        if (child is Grid grid)
                        {
                            foreach (var gridChild in LogicalTreeHelper.GetChildren(grid))
                            {
                                if (gridChild is Border b && b.Child is StackPanel sp)
                                {
                                    welcomePanel = sp;
                                    break;
                                }
                            }
                        }
                    }
                }
                
                if (welcomePanel != null)
                {
                    // Najít existující button nebo vytvořit nový
                    var existingButton = welcomePanel.Children
                        .OfType<Button>()
                        .FirstOrDefault(b => b.Name == "WelcomeRetryButton");
                    
                    if (existingButton == null)
                    {
                        var retryButton = new Button
                        {
                            Name = "WelcomeRetryButton",
                            Content = "Zkusit znovu",
                            Width = 160,
                            Height = 40,
                            Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Material Blue
                            Foreground = Brushes.White,
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            Margin = new Thickness(0, 20, 0, 0),
                            Cursor = Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        
                        retryButton.Click += (s, e) =>
                        {
                            Debug.WriteLine("Retry button clicked from welcome screen");
                            RetryConnection();
                        };
                        
                        welcomePanel.Children.Add(retryButton);
                    }
                    else
                    {
                        existingButton.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing retry button: {ex.Message}");
            }
        }
        
        private void RetryConnection()
        {
            // Reset retry counters
            navigationRetries = 0;
            backoffIndex = 0;
            networkChangeRetries = 0;
            
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Obnovit původní stav welcome screen
                    if (WelcomeMessage != null)
                    {
                        WelcomeMessage.Text = "Připojuji se k aplikačnímu portálu...";
                        WelcomeMessage.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Původní šedá
                    }
                    
                    // Zobrazit progress bar
                    if (ProgressBar != null)
                    {
                        ProgressBar.Visibility = Visibility.Visible;
                    }
                    if (ProgressBackground != null)
                    {
                        ProgressBackground.Visibility = Visibility.Visible;
                    }
                    
                    // Skrýt retry button
                    var border = WelcomeScreen?.FindName("WelcomeBorder") as Border;
                    var welcomePanel = border?.Child as StackPanel;
                    
                    if (welcomePanel == null)
                    {
                        // Fallback - hledat přímo v children
                        foreach (var child in LogicalTreeHelper.GetChildren(WelcomeScreen))
                        {
                            if (child is Grid grid)
                            {
                                foreach (var gridChild in LogicalTreeHelper.GetChildren(grid))
                                {
                                    if (gridChild is Border b && b.Child is StackPanel sp)
                                    {
                                        welcomePanel = sp;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    var retryButton = welcomePanel?.Children
                        .OfType<Button>()
                        .FirstOrDefault(b => b.Name == "WelcomeRetryButton");
                    
                    if (retryButton != null)
                    {
                        retryButton.Visibility = Visibility.Collapsed;
                    }
                    
                    // Spustit animaci
                    StartWelcomeAnimation();
                    
                    // Reload stránky
                    if (webView?.CoreWebView2 != null)
                    {
                        Debug.WriteLine("Reloading page after retry button click");
                        webView.CoreWebView2.Reload();
                    }
                    else
                    {
                        Debug.WriteLine("Navigating to LauncherUrl after retry button click");
                        webView?.CoreWebView2?.Navigate(LauncherUrl);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during retry: {ex.Message}");
                    ShowErrorInWelcomeScreen("Chyba při pokusu o připojení.");
                }
            });
        }
    }
}
