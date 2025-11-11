using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        
        public MainWindow()
        {
            Debug.WriteLine("=== STARTING CMI LAUNCHER ===");
            InitializeComponent();
            Debug.WriteLine("InitializeComponent completed");
            StartWelcomeAnimation();
            Debug.WriteLine("StartWelcomeAnimation completed");
            InitializeWebView();
            Debug.WriteLine("InitializeWebView completed");
            // Povolit vlastní klávesové zkratky
            this.Focusable = true;
            this.Focus();
            Debug.WriteLine("=== CMI LAUNCHER INITIALIZATION COMPLETED ===");
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
                
                await webView.EnsureCoreWebView2Async(environment);
                
                // Zakázat context menu (pravé tlačítko myši)
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                
                // Povolit DevTools pouze pro programové otevření (blokneme F12)
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // F12 zablokujeme ručně, okno otevře Ctrl+D
                
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
                        var certs = e.MutuallyTrustedCertificates;
                        Debug.WriteLine($"Client certificate requested. Found {certs.Count} certificates.");

                        // Pokud nejsou žádné certifikáty, pokračovat bez nich
                        if (certs.Count == 0)
                        {
                            e.Handled = true;
                            e.Cancel = false; // Pokračovat bez certifikátu
                            Debug.WriteLine("No client certificates available, continuing without certificate.");
                            
                            certificateWorkflowCompleted = true;
                            Dispatcher.Invoke(() => UpdateWelcomeMessage("Pokračuji bez certifikátu..."));
                            return;
                        }

                        // Najít commercial certifikát
                        Microsoft.Web.WebView2.Core.CoreWebView2ClientCertificate? commercialCert = null;
                        foreach (var c in certs)
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

                        // Zobrazit JEDNODUCHÝ MessageBox test
                        var result = Dispatcher.Invoke(() => {
                            var msgResult = MessageBox.Show(
                                "Byl nalezen klientský certifikát. Chcete ho použít pro přihlášení?",
                                "Výběr certifikátu",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question
                            );
                            return msgResult == MessageBoxResult.Yes ? CertificateDialogResult.UseCertificate : CertificateDialogResult.ContinueWithoutCertificate;
                        });
                        
                        e.Handled = true;
                        
                        switch (result)
                        {
                            case CertificateDialogResult.UseCertificate:
                                if (commercialCert != null)
                                {
                                    e.SelectedCertificate = commercialCert;
                                    e.Cancel = false;
                                    Debug.WriteLine($"User selected certificate: {commercialCert.DisplayName}");
                                }
                                else
                                {
                                    // Fallback na první dostupný
                                    e.SelectedCertificate = certs[0];
                                    e.Cancel = false;
                                    Debug.WriteLine($"Using fallback certificate: {certs[0].DisplayName}");
                                }
                                break;
                                
                            case CertificateDialogResult.ContinueWithoutCertificate:
                                e.Cancel = false; // Pokračovat bez certifikátu
                                Debug.WriteLine("User chose to continue without certificate.");
                                break;
                                
                            case CertificateDialogResult.Cancel:
                                e.Cancel = true; // Zrušit načítání
                                Debug.WriteLine("User cancelled certificate selection.");
                                break;
                        }
                        
                        certificateWorkflowCompleted = true;
                        Debug.WriteLine($"Certificate workflow completed with result: {result}");
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
                
                var errorMessage = e.WebErrorStatus switch
                {
                    CoreWebView2WebErrorStatus.ConnectionAborted => "Připojení bylo přerušeno",
                    CoreWebView2WebErrorStatus.Disconnected => "Žádné připojení k internetu",
                    CoreWebView2WebErrorStatus.HostNameNotResolved => "Nelze najít server launcher.cmi.cz",
                    CoreWebView2WebErrorStatus.Timeout => "Vypršel časový limit připojení",
                    CoreWebView2WebErrorStatus.ServerUnreachable => "Server není dostupný",
                    CoreWebView2WebErrorStatus.CannotConnect => "Nelze se připojit k serveru",
                    _ => $"Chyba při načítání stránky: {e.WebErrorStatus}"
                };
                
                ShowRetryOverlay($"{errorMessage}.", 0);
                ScheduleAutoRetry();
            }
            else
            {
                // Reset retry counter při úspěšné navigaci
                navigationRetries = 0;
                backoffIndex = 0;
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
                // Reset certificate dialog TaskCompletionSource
                certificateDialogTcs = new TaskCompletionSource<CertificateDialogResult>();
                
                // Zobrazit custom dialog
                CertificateDialog.Visibility = Visibility.Visible;
                Debug.WriteLine("Custom certificate dialog shown synchronously");
                
                // Počkáme na výsledek synchronně (blokující volání)
                var result = certificateDialogTcs.Task.GetAwaiter().GetResult();
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
            Debug.WriteLine("User chose to use certificate");
            CertificateDialog.Visibility = Visibility.Collapsed;
            UpdateWelcomeMessage("Přihlašuji s certifikátem...");
            HideWelcomeScreenAsync(1); // Skrýt welcome screen po 1 sekundě
            certificateDialogTcs?.SetResult(CertificateDialogResult.UseCertificate);
        }
        
        private void ContinueWithoutCertButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("User chose to continue without certificate");
            CertificateDialog.Visibility = Visibility.Collapsed;
            UpdateWelcomeMessage("Přihlašuji bez certifikátu...");
            HideWelcomeScreenAsync(1); // Skrýt welcome screen po 1 sekundě
            certificateDialogTcs?.SetResult(CertificateDialogResult.ContinueWithoutCertificate);
        }
        
        private void CancelCertButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("User chose to cancel");
            CertificateDialog.Visibility = Visibility.Collapsed;
            UpdateWelcomeMessage("Ruším připojení...");
            // Neposkytujeme žádný delay - okamžité zrušení
            certificateDialogTcs?.SetResult(CertificateDialogResult.Cancel);
        }
    }
}
