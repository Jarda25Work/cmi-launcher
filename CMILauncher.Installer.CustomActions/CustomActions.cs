using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using WixToolset.Dtf.WindowsInstaller;

namespace CMILauncher.Installer.CustomActions
{
    public class CustomActions
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [CustomAction]
        public static ActionResult CheckAndInstallDotNet8(Session session)
        {
            session.Log("Checking .NET 8 Desktop Runtime...");

            try
            {
                // Check if .NET 8 is installed
                if (IsDotNet8Installed(session))
                {
                    session.Log(".NET 8 Desktop Runtime is already installed");
                    return ActionResult.Success;
                }

                session.Log(".NET 8 Desktop Runtime is NOT installed. Downloading...");

                // Download .NET 8 Desktop Runtime
                string tempPath = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime-8.0.11-win-x64.exe");
                string downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/9d6b6b34-44b5-4cf4-b924-79a00deb9795/2f17f5643d45b7a9b1e5b8edd1e7a6d1/windowsdesktop-runtime-8.0.11-win-x64.exe";

                using (var response = httpClient.GetAsync(downloadUrl).Result)
                {
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }

                session.Log("Downloaded .NET 8 installer to: " + tempPath);

                // Install .NET 8
                session.Log("Installing .NET 8 Desktop Runtime...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0 || process.ExitCode == 3010)
                    {
                        session.Log(".NET 8 Desktop Runtime installed successfully");
                    }
                    else
                    {
                        session.Log("WARNING: .NET 8 installation returned exit code: " + process.ExitCode);
                    }
                }

                // Cleanup
                try { File.Delete(tempPath); } catch { }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("ERROR installing .NET 8: " + ex.Message);
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult CheckAndInstallWebView2(Session session)
        {
            session.Log("Checking WebView2 Runtime...");

            try
            {
                // Check if WebView2 is installed
                if (IsWebView2Installed(session))
                {
                    session.Log("WebView2 Runtime is already installed");
                    return ActionResult.Success;
                }

                session.Log("WebView2 Runtime is NOT installed. Downloading...");

                // Download WebView2 Runtime
                string tempPath = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebview2Setup.exe");
                string downloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

                using (var response = httpClient.GetAsync(downloadUrl).Result)
                {
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }

                session.Log("Downloaded WebView2 installer to: " + tempPath);

                // Install WebView2
                session.Log("Installing WebView2 Runtime...");
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/silent /install",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0 || process.ExitCode == 3010)
                    {
                        session.Log("WebView2 Runtime installed successfully");
                    }
                    else
                    {
                        session.Log("WARNING: WebView2 installation returned exit code: " + process.ExitCode);
                    }
                }

                // Cleanup
                try { File.Delete(tempPath); } catch { }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("ERROR installing WebView2: " + ex.Message);
                return ActionResult.Failure;
            }
        }

        private static bool IsDotNet8Installed(Session session)
        {
            try
            {
                // Check registry
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost"))
                {
                    if (key != null)
                    {
                        var version = key.GetValue("Version")?.ToString();
                        if (!string.IsNullOrEmpty(version) && version.StartsWith("8."))
                        {
                            session.Log("Found .NET version: " + version);
                            return true;
                        }
                    }
                }

                // Try running dotnet command
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (output.Contains("Microsoft.WindowsDesktop.App 8."))
                    {
                        session.Log("Found .NET 8 via dotnet command");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log("Error checking .NET installation: " + ex.Message);
            }

            return false;
        }

        private static bool IsWebView2Installed(Session session)
        {
            try
            {
                string[] regPaths = {
                    @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
                    @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
                };

                foreach (var path in regPaths)
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            var pv = key.GetValue("pv")?.ToString();
                            if (!string.IsNullOrEmpty(pv))
                            {
                                session.Log("Found WebView2 version: " + pv);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log("Error checking WebView2 installation: " + ex.Message);
            }

            return false;
        }
    }
}
