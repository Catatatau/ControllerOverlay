[CmdletBinding()]
param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerOverlay'),
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

$appName = 'ControllerOverlay'
$exeName = 'ControllerOverlay.exe'
$sourceExe = Join-Path $SourceDir $exeName
$targetExe = Join-Path $InstallDir $exeName

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Installer payload is missing $exeName in $SourceDir"
}

Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $targetExe -Force

$uninstallPath = Join-Path $InstallDir 'Uninstall-ControllerOverlay.ps1'
$uninstallScript = @'
$ErrorActionPreference = 'SilentlyContinue'

$appName = 'ControllerOverlay'
$installDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'ControllerOverlay.lnk'
$startMenuFolder = Join-Path ([Environment]::GetFolderPath('Programs')) 'ControllerOverlay'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ControllerOverlay'

Get-Process -Name $appName | Stop-Process -Force
Remove-Item -LiteralPath $desktopShortcut -Force
Remove-Item -LiteralPath $startMenuFolder -Recurse -Force
Remove-Item -LiteralPath $uninstallKey -Recurse -Force

$deleteCommand = "Start-Sleep -Milliseconds 500; Remove-Item -LiteralPath '$installDir' -Recurse -Force"
Start-Process -FilePath powershell.exe -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-WindowStyle', 'Hidden', '-Command', $deleteCommand)
'@
Set-Content -LiteralPath $uninstallPath -Value $uninstallScript -Encoding UTF8

function New-AppShortcut {
    param(
        [Parameter(Mandatory = $true)][string]$ShortcutPath,
        [Parameter(Mandatory = $true)][string]$TargetPath,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [string]$Description = 'Controller overlay for Rocket League'
    )

    $folder = Split-Path -Parent $ShortcutPath
    New-Item -ItemType Directory -Path $folder -Force | Out-Null

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = "$TargetPath,0"
    $shortcut.Description = $Description
    $shortcut.Save()
}

$desktopShortcut = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'ControllerOverlay.lnk'
$startMenuFolder = Join-Path ([Environment]::GetFolderPath('Programs')) 'ControllerOverlay'
$startMenuShortcut = Join-Path $startMenuFolder 'ControllerOverlay.lnk'
$uninstallShortcut = Join-Path $startMenuFolder 'Uninstall ControllerOverlay.lnk'

New-AppShortcut -ShortcutPath $desktopShortcut -TargetPath $targetExe -WorkingDirectory $InstallDir
New-AppShortcut -ShortcutPath $startMenuShortcut -TargetPath $targetExe -WorkingDirectory $InstallDir
New-AppShortcut -ShortcutPath $uninstallShortcut -TargetPath 'powershell.exe' -WorkingDirectory $InstallDir -Description 'Uninstall ControllerOverlay'

$shell = New-Object -ComObject WScript.Shell
$uninstallLink = $shell.CreateShortcut($uninstallShortcut)
$uninstallLink.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$uninstallPath`""
$uninstallLink.IconLocation = "$targetExe,0"
$uninstallLink.Save()

$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ControllerOverlay'
New-Item -Path $uninstallKey -Force | Out-Null
$estimatedSizeKb = [int][Math]::Ceiling((Get-Item -LiteralPath $targetExe).Length / 1KB)

New-ItemProperty -Path $uninstallKey -Name 'DisplayName' -Value 'ControllerOverlay' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayVersion' -Value '1.1.0' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'Publisher' -Value 'CATATAU' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'URLInfoAbout' -Value 'https://github.com/Catatatau/ControllerOverlay' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayIcon' -Value $targetExe -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallPath`"" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'EstimatedSize' -Value $estimatedSizeKb -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null

Write-Host "ControllerOverlay installed in $InstallDir"

if ($Launch) {
    Start-Process -FilePath $targetExe
}
