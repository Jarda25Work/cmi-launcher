# Convert PNG to ICO using System.Drawing
Add-Type -AssemblyName System.Drawing

$pngPath = "c:\_Qsync\PrimaKurzy\aplikacni-portal\migration_test\CMILauncher\Resources\icon.png"
$icoPath = "c:\_Qsync\PrimaKurzy\aplikacni-portal\migration_test\CMILauncher\Resources\icon.ico"

try {
    # Load PNG
    $png = [System.Drawing.Image]::FromFile($pngPath)
    
    # Create bitmap with desired size
    $sizes = @(16, 32, 48, 256)
    $icon = New-Object System.Drawing.Icon($pngPath, 256, 256)
    
    # Save as ICO
    $fs = [System.IO.File]::OpenWrite($icoPath)
    $icon.Save($fs)
    $fs.Close()
    
    Write-Host "ICO file created: $icoPath" -ForegroundColor Green
    
    $png.Dispose()
    $icon.Dispose()
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
    
    # Alternative: just copy the PNG and let .NET handle it
    $png = [System.Drawing.Bitmap]::FromFile($pngPath)
    $icon = [System.Drawing.Icon]::FromHandle($png.GetHicon())
    
    $fs = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
    $icon.Save($fs)
    $fs.Close()
    
    $png.Dispose()
    $icon.Dispose()
    
    Write-Host "ICO file created (alternative method): $icoPath" -ForegroundColor Green
}
