Place prerequisite installers in the Prereqs folder before building or shipping the bootstrapper:

Required files (x64):
- dotnet-runtime-8.0-x64.exe   # .NET 8 Desktop Runtime installer (offline)
- MicrosoftEdgeWebView2RuntimeInstallerX64.exe  # WebView2 Evergreen runtime (standalone)

Download links:
- .NET: https://dotnet.microsoft.com/download/dotnet/8.0 (Windows Desktop Runtime x64 offline)
- WebView2: https://developer.microsoft.com/microsoft-edge/webview2/ (Evergreen Standalone x64)

If the files are missing, the build will fail with WIX0103. At runtime, the bootstrapper requires these files present next to the EXE to install prerequisites offline.
