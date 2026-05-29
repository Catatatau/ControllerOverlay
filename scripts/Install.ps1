[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repo = 'Catatatau/ControllerOverlay'
$apiUrl = "https://api.github.com/repos/$repo/releases/latest"
$tempInstaller = Join-Path $env:TEMP 'ControllerOverlay-Setup-latest.exe'

Write-Host 'ControllerOverlay automatic installer'
Write-Host 'Looking for the latest GitHub release...'

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$headers = @{
    'User-Agent' = 'ControllerOverlayInstaller'
    'Accept' = 'application/vnd.github+json'
}

try {
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -UseBasicParsing
}
catch {
    Write-Host "Could not read latest release from https://github.com/$repo/releases/latest" -ForegroundColor Red
    throw
}

$asset = $release.assets |
    Where-Object { $_.name -like 'ControllerOverlay-Setup-*.exe' } |
    Select-Object -First 1

if (-not $asset) {
    throw 'No ControllerOverlay-Setup-*.exe asset was found in the latest release.'
}

Write-Host "Downloading $($asset.name)..."
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $tempInstaller -Headers $headers -UseBasicParsing
Unblock-File -LiteralPath $tempInstaller -ErrorAction SilentlyContinue

Write-Host 'Starting installer...'
$process = Start-Process -FilePath $tempInstaller -Wait -PassThru

if ($process.ExitCode -ne 0) {
    throw "Installer exited with code $($process.ExitCode)."
}

Write-Host 'ControllerOverlay installed successfully.' -ForegroundColor Green
