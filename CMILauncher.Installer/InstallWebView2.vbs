' VBScript pro instalaci WebView2 Runtime
' Tento skript se spusti jako Custom Action v MSI instalatoru

Option Explicit

Dim objShell, objFSO, objHTTP, objStream
Dim strURL, strTempPath, strInstallerPath
Dim intResult

' Vytvorit objekty
Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")

' URL pro WebView2 Runtime
strURL = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"

' Cesta pro docasny soubor
strTempPath = objShell.ExpandEnvironmentStrings("%TEMP%")
strInstallerPath = strTempPath & "\webview2-runtime-installer.exe"

' Stahnout instalator
On Error Resume Next
Set objHTTP = CreateObject("MSXML2.ServerXMLHTTP.6.0")
If Err.Number <> 0 Then
    Set objHTTP = CreateObject("MSXML2.ServerXMLHTTP")
End If
On Error Goto 0

objHTTP.Open "GET", strURL, False
objHTTP.setRequestHeader "User-Agent", "Mozilla/5.0"
objHTTP.Send

If objHTTP.Status = 200 Then
    ' Ulozit soubor
    Set objStream = CreateObject("ADODB.Stream")
    objStream.Type = 1 ' Binary
    objStream.Open
    objStream.Write objHTTP.ResponseBody
    objStream.SaveToFile strInstallerPath, 2 ' Overwrite
    objStream.Close
    Set objStream = Nothing
    
    ' Spustit instalaci
    intResult = objShell.Run("""" & strInstallerPath & """ /silent /install", 1, True)
    
    ' Smazat docasny soubor
    If objFSO.FileExists(strInstallerPath) Then
        objFSO.DeleteFile strInstallerPath
    End If
    
    ' Vratit vysledek
    If intResult = 0 Then
        WScript.Quit 0 ' Uspech
    Else
        WScript.Quit 1603 ' Chyba instalace
    End If
Else
    WScript.Quit 1603 ' Chyba stahovani
End If

Set objHTTP = Nothing
Set objFSO = Nothing
Set objShell = Nothing
