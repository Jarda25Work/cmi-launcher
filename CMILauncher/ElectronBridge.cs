using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CMILauncher
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class ElectronBridge
    {
        private readonly MainWindow _mainWindow;
        private const string BaseDir = "c:/iscmi";

        public ElectronBridge(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Send(string channel, string argsJson)
        {
            try
            {
                Debug.WriteLine($"IPC Received: {channel} - {argsJson}");
                
                var args = JsonSerializer.Deserialize<JsonElement[]>(argsJson);
                
                switch (channel)
                {
                    case "launch":
                        HandleLaunch(args);
                        break;
                        
                    case "openExternal":
                        HandleOpenExternal(args);
                        break;
                        
                    case "canLaunch":
                        HandleCanLaunch(args);
                        break;
                        
                    case "install":
                        HandleInstall(args);
                        break;
                        
                    case "hello":
                        HandleHello();
                        break;
                        
                    default:
                        Debug.WriteLine($"Unknown IPC channel: {channel}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling IPC: {ex.Message}");
            }
        }

        public string SendSync(string channel, string argsJson)
        {
            // Pro synchronní volání (pokud je potřeba)
            Debug.WriteLine($"IPC Sync Received: {channel} - {argsJson}");
            return "{}";
        }

        private void HandleLaunch(JsonElement[] args)
        {
            if (args.Length < 2) return;
            
            var installDir = args[0].GetString();
            var command = args[1].GetString();
            var parameters = args.Length > 2 && args[2].ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<string[]>(args[2].GetRawText())
                : Array.Empty<string>();

            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.LaunchApp(installDir, command, parameters);
            });
        }

        private void HandleOpenExternal(JsonElement[] args)
        {
            if (args.Length < 1) return;
            
            var url = args[0].GetString();
            
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.OpenExternal(url);
            });
        }

        private void HandleCanLaunch(JsonElement[] args)
        {
            if (args.Length < 5) return;
            
            var appId = args[0].GetString();
            var env = args[1].GetString();
            var command = args[2].GetString();
            var installDir = args[3].GetString();
            
            _mainWindow.Dispatcher.Invoke(() =>
            {
                var status = CheckCanLaunch(command, installDir);
                SendCanLaunchResult(appId, env, status);
            });
        }

        private int CheckCanLaunch(string command, string installDir)
        {
            try
            {
                // Rozdělit command pokud obsahuje | (fallback commands)
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

                    Debug.WriteLine($"Checking if exists: {fullPath}");
                    
                    if (File.Exists(fullPath))
                    {
                        Debug.WriteLine($"✓ Found: {fullPath}");
                        return 0; // Aplikace je nainstalovaná
                    }
                }
                
                // Žádný command nenalezen, zkontrolovat jestli existuje instalační složka
                var installPath = Path.Combine(BaseDir, installDir);
                Debug.WriteLine($"Checking install dir: {installPath}");
                
                if (Directory.Exists(installPath))
                {
                    Debug.WriteLine($"⚠ Install dir exists but command not found: {installPath}");
                    return -2; // Složka existuje, ale exe ne = probíhá instalace?
                }
                
                Debug.WriteLine($"✗ Not installed: {installDir}");
                return -1; // Aplikace není nainstalovaná
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking canLaunch: {ex.Message}");
                return -1;
            }
        }

        private async void SendCanLaunchResult(string appId, string env, int status)
        {
            try
            {
                // Volat JavaScript callback zpět do webové aplikace
                var script = $@"
                    (function() {{
                        if (window.canLaunchCallbacks && window.canLaunchCallbacks['{appId}_{env}']) {{
                            window.canLaunchCallbacks['{appId}_{env}']({status});
                            delete window.canLaunchCallbacks['{appId}_{env}'];
                        }}
                        console.log('canLaunchResult: {appId} {env} = {status}');
                    }})();
                ";
                
                await _mainWindow.webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending canLaunchResult: {ex.Message}");
            }
        }

        private void HandleInstall(JsonElement[] args)
        {
            if (args.Length < 4) return;
            
            var appId = args[0].GetString();
            var env = args[1].GetString();
            var url = args[2].GetString();
            var dest = args[3].GetString();
            
            _mainWindow.Dispatcher.Invoke(async () =>
            {
                await InstallAppAsync(appId, env, url, dest);
            });
        }

        private async System.Threading.Tasks.Task InstallAppAsync(string appId, string env, string url, string dest)
        {
            try
            {
                // DŮLEŽITÉ: dest může být např. "zakazky2" 
                // BaseDir je "c:/iscmi" takže výsledek: "c:\iscmi\zakazky2"
                var destination = Path.GetFullPath(Path.Combine(BaseDir, dest));
                Debug.WriteLine($"=== INSTALL START: {appId} ===");
                Debug.WriteLine($"Destination: {destination}");
                Debug.WriteLine($"URL: {url}");
                
                // Vytvořit cílovou složku
                Directory.CreateDirectory(destination);
                Debug.WriteLine($"Created directory: {destination}");
                
                // 1. Nejdřív stáhnout manifest.json a zjistit správný soubor k download
                string manifestUrl = $"{url}/manifest.json";
                string archiveFileName = "current.tar.gz"; // Fallback
                bool containsBaseFolder = false;
                
                Debug.WriteLine($"Downloading manifest from {manifestUrl}...");
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    try
                    {
                        var manifestJson = await client.GetStringAsync(manifestUrl);
                        Debug.WriteLine($"Manifest downloaded, parsing...");
                        
                        var manifest = JsonDocument.Parse(manifestJson);
                        
                        // Parsování: manifest[appId][env]
                        if (manifest.RootElement.TryGetProperty(appId, out var appElement))
                        {
                            if (appElement.TryGetProperty(env, out var envElement))
                            {
                                Debug.WriteLine($"Found manifest entry for {appId}.{env}");
                                
                                // Získat verzi
                                if (envElement.TryGetProperty("version", out var versionElement))
                                {
                                    var version = versionElement.GetString();
                                    Debug.WriteLine($"Version: {version}");
                                    
                                    // Získat archive template (např. "zakazky2_%s.zip")
                                    if (envElement.TryGetProperty("archive", out var archiveElement))
                                    {
                                        var archiveTemplate = archiveElement.GetString();
                                        // Format archive name s verzí - nahradit %s verzí (např. "zakazky2_7.0.7.5.zip")
                                        // Podporuje %s (printf style) i {0} (.NET style)
                                        archiveFileName = archiveTemplate.Replace("%s", version ?? "").Replace("{0}", version ?? "");
                                        Debug.WriteLine($"Archive template: {archiveTemplate} -> {archiveFileName}");
                                    }
                                }
                                
                                // containsBaseFolder určuje jestli archiv obsahuje složku nebo jen soubory
                                if (envElement.TryGetProperty("containsBaseFolder", out var baseFolderElement))
                                {
                                    containsBaseFolder = baseFolderElement.GetBoolean();
                                    Debug.WriteLine($"ContainsBaseFolder: {containsBaseFolder}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Manifest doesn't contain env '{env}' for app '{appId}', using fallback");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Manifest doesn't contain app '{appId}', using fallback");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to download/parse manifest: {ex.Message}, using fallback");
                    }
                }
                
                // 2. Stáhnout a rozbalit přímo ze streamu (JAKO ELECTRON - bez ukládání na disk)
                // Electron používá res.pipe(unzipper) - my použijeme ZipArchive/tar přímo s HTTP streamem
                // Tím se vyhneme antivirovým kontrolám dočasných souborů
                var archiveUrl = $"{url}/{archiveFileName}";
                Debug.WriteLine($"Downloading and extracting from {archiveUrl}...");
                
                // Pokud containsBaseFolder je true, extrahovat do BaseDir (archiv obsahuje složku dest/)
                // Pokud false, extrahovat přímo do destination (soubory jsou na root levelu archivu)
                var extractPath = containsBaseFolder ? BaseDir : destination;
                Debug.WriteLine($"Extract path: {extractPath} (containsBaseFolder={containsBaseFolder})");
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    var response = await client.GetAsync(archiveUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = $"Failed to download archive.\nURL: {archiveUrl}\nStatus: {response.StatusCode}";
                        Debug.WriteLine($"ERROR: {error}");
                        await SendInstallResult(appId, env, false, error);
                        return;
                    }
                    
                    Debug.WriteLine($"HTTP status: {response.StatusCode}, starting stream extraction...");
                    
                    await SendDownloadedResult(appId, env);
                    Debug.WriteLine($"Sent 'downloaded' callback");
                    
                    // Extrahovat přímo ze streamu bez uložení na disk
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (archiveFileName.EndsWith(".zip"))
                        {
                            // ZIP - použít ZipArchive přímo se streamem
                            Debug.WriteLine("Extracting ZIP from stream...");
                            using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read))
                            {
                                foreach (var entry in archive.Entries)
                                {
                                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                                    
                                    // Bezpečnostní kontrola - zabránit path traversal
                                    if (!destinationPath.StartsWith(Path.GetFullPath(extractPath)))
                                    {
                                        Debug.WriteLine($"Skipping unsafe path: {entry.FullName}");
                                        continue;
                                    }
                                    
                                    if (entry.FullName.EndsWith("/"))
                                    {
                                        // Složka
                                        Directory.CreateDirectory(destinationPath);
                                    }
                                    else
                                    {
                                        // Soubor
                                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                                        using (var entryStream = entry.Open())
                                        using (var fileStream = File.Create(destinationPath))
                                        {
                                            await entryStream.CopyToAsync(fileStream);
                                        }
                                    }
                                }
                            }
                            Debug.WriteLine("ZIP extracted successfully from stream");
                        }
                        else if (archiveFileName.EndsWith(".tar.gz") || archiveFileName.EndsWith(".tgz"))
                        {
                            // TAR.GZ - musíme použít tar command, ale můžeme streamu dát stdin
                            Debug.WriteLine("Extracting TAR.GZ from stream...");
                            
                            // Pro tar ze streamu musíme bohužel použít temp soubor (tar.exe neumí stdin stream v Powershell)
                            // ALE: dáme mu bezpečné jméno a hned smažeme
                            var tempTar = Path.Combine(destination, $"_install_temp_{Guid.NewGuid()}.tar.gz");
                            try
                            {
                                using (var fileStream = File.Create(tempTar))
                                {
                                    await stream.CopyToAsync(fileStream);
                                }
                                Debug.WriteLine($"Temp tar saved to {tempTar}");
                                
                                ExtractTarGz(tempTar, extractPath);
                                Debug.WriteLine("TAR.GZ extracted successfully");
                            }
                            finally
                            {
                                // Smazat temp soubor hned
                                try { File.Delete(tempTar); Debug.WriteLine("Temp tar deleted"); }
                                catch { }
                            }
                        }
                    }
                }
                
                // Zobrazit co se rozbalilo
                var files = Directory.GetFiles(destination, "*.*", SearchOption.AllDirectories);
                Debug.WriteLine($"Extracted {files.Length} files:");
                foreach (var file in files.Take(10))
                {
                    Debug.WriteLine($"  - {file.Substring(destination.Length + 1)}");
                }
                if (files.Length > 10) Debug.WriteLine($"  ... and {files.Length - 10} more files");
                
                // Zkontrolovat a spustit install.bat pokud existuje
                var installerPath = Path.Combine(destination, "install.bat");
                if (File.Exists(installerPath))
                {
                    Debug.WriteLine($"Found installer: {installerPath}");
                    Debug.WriteLine($"Running installer...");
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        WorkingDirectory = destination,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                }
                else
                {
                    Debug.WriteLine($"No install.bat found - installation complete");
                }
                
                Debug.WriteLine($"=== INSTALL SUCCESS: {appId} ===");
                await SendInstallResult(appId, env, true, "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== INSTALL ERROR: {appId} ===");
                Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendInstallResult(appId, env, false, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private void ExtractTarGz(string archivePath, string destination)
        {
            Debug.WriteLine($"ExtractTarGz: {archivePath} -> {destination}");
            
            // Použít tar command (dostupný ve Windows 10+)
            var startInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{destination}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            
            Debug.WriteLine($"Running: tar {startInfo.Arguments}");
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                
                Debug.WriteLine($"tar exit code: {process.ExitCode}");
                if (!string.IsNullOrEmpty(stdout)) Debug.WriteLine($"tar stdout: {stdout}");
                if (!string.IsNullOrEmpty(stderr)) Debug.WriteLine($"tar stderr: {stderr}");
                
                if (process.ExitCode != 0)
                {
                    throw new Exception($"tar extraction failed with exit code {process.ExitCode}: {stderr}");
                }
            }
            else
            {
                throw new Exception("Failed to start tar process");
            }
        }

        private async System.Threading.Tasks.Task SendDownloadedResult(string appId, string env)
        {
            try
            {
                var script = $@"
                    (function() {{
                        const callbacks = window.ipcCallbacks['downloaded'] || [];
                        callbacks.forEach(cb => {{
                            try {{
                                cb(null, '{appId}', '{env}');
                            }} catch (e) {{
                                console.error('Error in downloaded callback:', e);
                            }}
                        }});
                        console.log('downloaded: {appId} {env}');
                    }})();
                ";
                
                await _mainWindow.webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending downloaded result: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SendInstallResult(string appId, string env, bool success, string error)
        {
            try
            {
                // Scala očekává: (event, appId, env, error: js.UndefOr[String])
                // Pokud success → error musí být undefined (ne null!)
                // Pokud error → error je string
                var errorParam = success ? "undefined" : $"'{error?.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ")}'";
                
                // Pokud je chyba, zobrazit MessageBox
                if (!success && !string.IsNullOrEmpty(error))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"Instalace aplikace {appId} ({env}) selhala:\n\n{error}",
                            "Chyba instalace",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error
                        );
                    });
                }
                
                var script = $@"
                    (function() {{
                        const callbacks = window.ipcCallbacks['installed'] || [];
                        callbacks.forEach(cb => {{
                            try {{
                                cb(null, '{appId}', '{env}', {errorParam});
                            }} catch (e) {{
                                console.error('Error in installed callback:', e);
                            }}
                        }});
                        console.log('✓ installed callback sent: {appId} {env} success={success.ToString().ToLower()}', {errorParam});
                    }})();
                ";
                
                await _mainWindow.webView.CoreWebView2.ExecuteScriptAsync(script);
                Debug.WriteLine($"Sent 'installed' callback: {appId} {env} success={success}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending install result: {ex.Message}");
            }
        }

        private void HandleHello()
        {
            Debug.WriteLine("Hello from web app!");
        }
    }
}
