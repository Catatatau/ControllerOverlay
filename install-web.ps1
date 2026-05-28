$ErrorActionPreference = 'Stop'
Write-Host "Downloading ControllerOverlay Setup..."
$url = "https://raw.githubusercontent.com/Catatatau/ControllerOverlay/main/dist/ControllerOverlay-Setup-1.0.0.exe"
$tempExe = Join-Path $env:TEMP "ControllerOverlay-Setup.exe"
Invoke-WebRequest -Uri $url -OutFile $tempExe
Write-Host "Starting setup..."
Start-Process -FilePath $tempExe -Wait
Write-Host "Done!"
