[CmdletBinding()]
param(
    [string]$Version = 'latest'
)

$ErrorActionPreference = 'Stop'

$repo = 'Catatatau/ControllerOverlay'
$repoUrl = "https://github.com/$repo"
$apiUrl = if ($Version -eq 'latest') {
    "https://api.github.com/repos/$repo/releases/latest"
}
else {
    "https://api.github.com/repos/$repo/releases/tags/$Version"
}
$tempDir = Join-Path $env:TEMP 'ControllerOverlay'

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
    Write-Host "Could not read release metadata from $repoUrl/releases/latest" -ForegroundColor Red
    throw
}

$asset = $release.assets |
    Where-Object { $_.name -like 'ControllerOverlay-Setup-*.exe' } |
    Sort-Object name -Descending |
    Select-Object -First 1

if (-not $asset) {
    throw "No ControllerOverlay-Setup-*.exe asset was found in the release. Open $repoUrl/releases/latest and check the release assets."
}

New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$tempInstaller = Join-Path $tempDir $asset.name

if (Test-Path -LiteralPath $tempInstaller) {
    Remove-Item -LiteralPath $tempInstaller -Force
}

$downloadUrl = $asset.browser_download_url
Write-Host "Downloading $($asset.name) from $downloadUrl..."
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempInstaller -Headers $headers -UseBasicParsing
}
catch {
    Write-Host 'GitHub API download failed; trying the public latest/download URL...' -ForegroundColor Yellow
    $downloadUrl = "$repoUrl/releases/latest/download/$($asset.name)"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $tempInstaller -UseBasicParsing
}

if (-not (Test-Path -LiteralPath $tempInstaller)) {
    throw "Download failed. The installer was not created at $tempInstaller"
}

$downloadedFile = Get-Item -LiteralPath $tempInstaller
if ($downloadedFile.Length -lt 2) {
    throw "Download failed. The installer file is empty: $tempInstaller"
}

$signature = [System.IO.File]::ReadAllBytes($tempInstaller)[0..1]
if ($signature[0] -ne 0x4D -or $signature[1] -ne 0x5A) {
    Remove-Item -LiteralPath $tempInstaller -Force -ErrorAction SilentlyContinue
    throw "Download did not produce a Windows executable. Refusing to run: $downloadUrl"
}

Unblock-File -LiteralPath $tempInstaller -ErrorAction SilentlyContinue

Write-Host 'Starting installer...'
$process = Start-Process -FilePath $tempInstaller -Wait -PassThru

if ($process.ExitCode -ne 0) {
    throw "Installer exited with code $($process.ExitCode)."
}

$installedExe = Join-Path $env:LOCALAPPDATA 'Programs\ControllerOverlay\ControllerOverlay.exe'
if (-not (Test-Path -LiteralPath $installedExe)) {
    throw "Installer finished, but ControllerOverlay.exe was not found at $installedExe"
}

Write-Host 'ControllerOverlay installed successfully.' -ForegroundColor Green
