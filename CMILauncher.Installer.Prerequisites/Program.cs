using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;

namespace CMILauncher.Installer.Prerequisites
{
    class Program
    {
        static int Main(string[] args)
        {
            // Kontrola admin prav
            if (!IsAdministrator())
            {
                Console.WriteLine("CHYBA: Jsou potreba administrativa prava");
                return 1;
            }

            bool installDotNet = args.Length > 0 && args[0] == "dotnet";
            bool installWebView2 = args.Length > 0 && args[0] == "webview2";

            try
            {
                if (installDotNet)
                {
                    Console.WriteLine("Instalace .NET 8 Desktop Runtime...");
                    
                    // Kontrola zda uz je nainstalovan
                    if (IsDotNet8Installed())
                    {
                        Console.WriteLine(".NET 8 Desktop Runtime je jiz nainstalovan");
                        return 0;
                    }

                    // Stazeni a instalace
                    string url = "https://download.visualstudio.microsoft.com/download/pr/b280d97f-25a9-4ab7-8a12-b2eb207dd7a2/bf6e0c9087ace030e0c5f1129a92d0b0/windowsdesktop-runtime-8.0.11-win-x64.exe";
                    string tempPath = Path.Combine(Path.GetTempPath(), "dotnet8-runtime.exe");
                    
                    Console.WriteLine("Stahuji z: " + url);
                    using (WebClient client = new WebClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DownloadFile(url, tempPath);
                    }

                    Console.WriteLine("Spoustim instalaci...");
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/install /quiet /norestart",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    Process process = Process.Start(psi);
                    process.WaitForExit();
                    
                    File.Delete(tempPath);
                    
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine(".NET 8 Desktop Runtime uspesne nainstalovan");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Instalace selhala s kodem: " + process.ExitCode);
                        return process.ExitCode;
                    }
                }
                else if (installWebView2)
                {
                    Console.WriteLine("Instalace WebView2 Runtime...");
                    
                    // Kontrola zda uz je nainstalovan
                    if (IsWebView2Installed())
                    {
                        Console.WriteLine("WebView2 Runtime je jiz nainstalovan");
                        return 0;
                    }

                    // Stazeni a instalace
                    string url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
                    string tempPath = Path.Combine(Path.GetTempPath(), "webview2-runtime.exe");
                    
                    Console.WriteLine("Stahuji z: " + url);
                    using (WebClient client = new WebClient())
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        client.DownloadFile(url, tempPath);
                    }

                    Console.WriteLine("Spoustim instalaci...");
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/silent /install",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    Process process = Process.Start(psi);
                    process.WaitForExit();
                    
                    File.Delete(tempPath);
                    
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("WebView2 Runtime uspesne nainstalovan");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("Instalace selhala s kodem: " + process.ExitCode);
                        return process.ExitCode;
                    }
                }
                else
                {
                    Console.WriteLine("Pouziti: InstallPrerequisites.exe [dotnet|webview2]");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CHYBA: " + ex.Message);
                return 1;
            }
        }

        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static bool IsDotNet8Installed()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Contains("Microsoft.WindowsDesktop.App 8.");
            }
            catch
            {
                return false;
            }
        }

        static bool IsWebView2Installed()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"))
                {
                    return key != null && key.GetValue("pv") != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
