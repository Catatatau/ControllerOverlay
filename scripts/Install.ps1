[CmdletBinding()]
param(
    [string]$Version = 'latest',
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerOverlay'),
    [switch]$NoLaunch
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

$appName = 'ControllerOverlay'
$exeName = 'ControllerOverlay.exe'
$targetAppDir = $null
$targetExe = $null
$tempRoot = Join-Path $env:TEMP 'ControllerOverlay'
$tempDir = Join-Path $tempRoot ([Guid]::NewGuid().ToString('N'))

function Get-ReleaseAsset {
    param(
        [Parameter(Mandatory = $true)]$Release,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $Release.assets |
        Where-Object { $_.name -eq $Name } |
        Select-Object -First 1
}

function Save-ReleaseAsset {
    param(
        [Parameter(Mandatory = $true)]$Asset,
        [Parameter(Mandatory = $true)][string]$OutFile
    )

    $downloadUrl = $Asset.browser_download_url
    Write-Host "Downloading $($Asset.name)..."

    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $OutFile -Headers $script:headers -UseBasicParsing
    }
    catch {
        Write-Host 'GitHub API download failed; trying the public latest/download URL...' -ForegroundColor Yellow
        $downloadUrl = "$script:repoUrl/releases/latest/download/$($Asset.name)"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $OutFile -UseBasicParsing
    }

    if (-not (Test-Path -LiteralPath $OutFile)) {
        throw "Download failed. File was not created: $OutFile"
    }

    $downloadedFile = Get-Item -LiteralPath $OutFile
    if ($downloadedFile.Length -lt 2) {
        throw "Download failed. File is empty: $OutFile"
    }

    return $downloadUrl
}

function Test-WindowsExecutable {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    $signature = [System.IO.File]::ReadAllBytes($Path)[0..1]
    return $signature[0] -eq 0x4D -and $signature[1] -eq 0x5A
}

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

function Write-Uninstaller {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    @'
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
'@ | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Register-Uninstaller {
    param(
        [Parameter(Mandatory = $true)][string]$VersionText,
        [Parameter(Mandatory = $true)][string]$UninstallPath
    )

    $uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ControllerOverlay'
    New-Item -Path $uninstallKey -Force | Out-Null
    $estimatedSizeKb = [int][Math]::Ceiling((Get-Item -LiteralPath $targetExe).Length / 1KB)

    New-ItemProperty -Path $uninstallKey -Name 'DisplayName' -Value 'ControllerOverlay' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'DisplayVersion' -Value $VersionText -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'Publisher' -Value 'CATATAU' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'URLInfoAbout' -Value $repoUrl -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'DisplayIcon' -Value $targetExe -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$UninstallPath`"" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'EstimatedSize' -Value $estimatedSizeKb -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallKey -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null
}

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

$exeAsset = Get-ReleaseAsset -Release $release -Name $exeName
if (-not $exeAsset) {
    throw "No $exeName asset was found in the release. The automatic installer needs the raw app executable, not ControllerOverlay-Setup."
}

$keyboardsAsset = Get-ReleaseAsset -Release $release -Name 'keyboards.zip'

New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$tempExe = Join-Path $tempDir $exeName
$tempKeyboardsZip = Join-Path $tempDir 'keyboards.zip'

try {
    $exeDownloadUrl = Save-ReleaseAsset -Asset $exeAsset -OutFile $tempExe
    if (-not (Test-WindowsExecutable -Path $tempExe)) {
        throw "Download did not produce a Windows executable. Refusing to install: $exeDownloadUrl"
    }

    if ($keyboardsAsset) {
        Save-ReleaseAsset -Asset $keyboardsAsset -OutFile $tempKeyboardsZip | Out-Null
    }
    else {
        Write-Host 'keyboards.zip was not found in the release; keyboard presets will not be refreshed.' -ForegroundColor Yellow
    }

    Unblock-File -LiteralPath $tempExe -ErrorAction SilentlyContinue
    Stop-RunningApp

    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $downloadedVersion = (Get-Item -LiteralPath $tempExe).VersionInfo.ProductVersion
    if ($downloadedVersion -match '^(\d+\.\d+\.\d+)') {
        $installVersion = $Matches[1]
    }
    else {
        $installVersion = ($release.tag_name -replace '^v', '')
    }

    $targetAppDir = New-VersionedAppDirectory -Root $InstallDir -VersionText $installVersion
    $targetExe = Join-Path $targetAppDir $exeName
    Copy-FileWithRetry -Source $tempExe -Destination $targetExe

    $legacyRootExe = Join-Path $InstallDir $exeName
    if (Test-Path -LiteralPath $legacyRootExe) {
        Remove-Item -LiteralPath $legacyRootExe -Force -ErrorAction SilentlyContinue
    }

    $targetKeyboardsDir = Join-Path $targetAppDir 'keyboards'
    if (Test-Path -LiteralPath $tempKeyboardsZip) {
        $targetNohBoardDir = Join-Path $targetKeyboardsDir 'NohBoard'
        if (Test-Path -LiteralPath $targetNohBoardDir) {
            Remove-Item -LiteralPath $targetNohBoardDir -Recurse -Force
        }

        New-Item -ItemType Directory -Path $targetKeyboardsDir -Force | Out-Null
        Expand-Archive -LiteralPath $tempKeyboardsZip -DestinationPath $targetKeyboardsDir -Force
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
    Write-Uninstaller -Path $uninstallPath

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

    $installedVersion = (Get-Item -LiteralPath $targetExe).VersionInfo.ProductVersion
    if ([string]::IsNullOrWhiteSpace($installedVersion)) {
        $installedVersion = $release.tag_name
    }

    Register-Uninstaller -VersionText $installedVersion -UninstallPath $uninstallPath

    if (-not (Test-Path -LiteralPath $targetExe)) {
        throw "Install failed. $exeName was not created at $targetExe"
    }

    Get-ChildItem -LiteralPath $InstallDir -Directory -Filter 'app-*' -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -ne $targetAppDir } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    if (-not $NoLaunch) {
        Start-Process -FilePath $targetExe
    }

    Write-Host "ControllerOverlay installed in $InstallDir" -ForegroundColor Green
}
finally {
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
