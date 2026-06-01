[CmdletBinding()]
param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerOverlay'),
    [string]$Version = '1.2.3',
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

$appName = 'ControllerOverlay'
$exeName = 'ControllerOverlay.exe'
$sourceExe = Join-Path $SourceDir $exeName
$targetAppDir = $null
$targetExe = $null

function Stop-RunningApp {
    $processes = @(Get-Process -Name $appName -ErrorAction SilentlyContinue)
    if ($processes.Count -eq 0) {
        return
    }

    $ids = @($processes | Select-Object -ExpandProperty Id)
    $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    foreach ($id in $ids) {
        try {
            Wait-Process -Id $id -Timeout 10 -ErrorAction SilentlyContinue
        }
        catch {
        }
    }
}

function Copy-FileWithRetry {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    for ($attempt = 1; $attempt -le 12; $attempt++) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force
            return
        }
        catch {
            if ($attempt -eq 12) {
                throw
            }

            Start-Sleep -Milliseconds (250 * $attempt)
        }
    }
}

function New-VersionedAppDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$VersionText
    )

    $safeVersion = $VersionText -replace '[^0-9A-Za-z_.-]', '_'
    if ([string]::IsNullOrWhiteSpace($safeVersion)) {
        $safeVersion = 'current'
    }

    $appDir = Join-Path $Root "app-$safeVersion"
    if (Test-Path -LiteralPath $appDir) {
        try {
            Remove-Item -LiteralPath $appDir -Recurse -Force
        }
        catch {
            $appDir = Join-Path $Root ("app-$safeVersion-" + (Get-Date -Format 'yyyyMMddHHmmss'))
        }
    }

    New-Item -ItemType Directory -Path $appDir -Force | Out-Null
    return $appDir
}

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Installer payload is missing $exeName in $SourceDir"
}

Stop-RunningApp

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
$targetAppDir = New-VersionedAppDirectory -Root $InstallDir -VersionText $Version
$targetExe = Join-Path $targetAppDir $exeName
Copy-FileWithRetry -Source $sourceExe -Destination $targetExe

$legacyRootExe = Join-Path $InstallDir $exeName
if (Test-Path -LiteralPath $legacyRootExe) {
    Remove-Item -LiteralPath $legacyRootExe -Force -ErrorAction SilentlyContinue
}

$sourceKeyboardsZip = Join-Path $SourceDir 'keyboards.zip'
$targetKeyboardsDir = Join-Path $targetAppDir 'keyboards'
if (Test-Path -LiteralPath $sourceKeyboardsZip) {
    $targetNohBoardDir = Join-Path $targetKeyboardsDir 'NohBoard'
    if (Test-Path -LiteralPath $targetNohBoardDir) {
        Remove-Item -LiteralPath $targetNohBoardDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $targetKeyboardsDir -Force | Out-Null
    Expand-Archive -LiteralPath $sourceKeyboardsZip -DestinationPath $targetKeyboardsDir -Force
}

$userKeyboardsDir = Join-Path $env:APPDATA 'ControllerOverlay\keyboards'
New-Item -ItemType Directory -Path $userKeyboardsDir -Force | Out-Null
$userKeyboardsReadme = Join-Path $userKeyboardsDir 'README.txt'
if (-not (Test-Path -LiteralPath $userKeyboardsReadme)) {
    @'
Coloque aqui pastas de teclado no formato NohBoard.
Cada modelo precisa ter um arquivo keyboard.json.

Exemplo:
%APPDATA%\ControllerOverlay\keyboards\MeuModelo\keyboard.json
'@ | Set-Content -LiteralPath $userKeyboardsReadme -Encoding UTF8
}

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

New-AppShortcut -ShortcutPath $desktopShortcut -TargetPath $targetExe -WorkingDirectory $targetAppDir
New-AppShortcut -ShortcutPath $startMenuShortcut -TargetPath $targetExe -WorkingDirectory $targetAppDir
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
New-ItemProperty -Path $uninstallKey -Name 'DisplayVersion' -Value $Version -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'Publisher' -Value 'CATATAU' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'URLInfoAbout' -Value 'https://github.com/Catatatau/ControllerOverlay' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayIcon' -Value $targetExe -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallPath`"" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'EstimatedSize' -Value $estimatedSizeKb -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null

Get-ChildItem -LiteralPath $InstallDir -Directory -Filter 'app-*' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -ne $targetAppDir } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "ControllerOverlay installed in $InstallDir"

if ($Launch) {
    Start-Process -FilePath $targetExe
}
