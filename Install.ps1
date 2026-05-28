[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$appName = 'ControllerOverlay'
$exeName = 'ControllerOverlay.exe'
$InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\ControllerOverlay')
$targetExe = Join-Path $InstallDir $exeName

Write-Host "Baixando a versao mais recente do ControllerOverlay..."
$url = "https://github.com/Catatatau/ControllerOverlay/releases/download/latest/ControllerOverlay.zip"
$tempZip = Join-Path $env:TEMP "ControllerOverlay.zip"
$tempExtract = Join-Path $env:TEMP "ControllerOverlay_Extract"

try {
    # Habilita TLS 1.2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $tempZip -UseBasicParsing
} catch {
    Write-Host "Erro: Não foi possivel baixar o arquivo. Certifique-se de que o GitHub Action gerou a release 'latest'." -ForegroundColor Red
    exit 1
}

if (Test-Path $tempExtract) { Remove-Item $tempExtract -Recurse -Force }
New-Item -ItemType Directory -Path $tempExtract -Force | Out-Null

Write-Host "Extraindo arquivos..."
Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force

$sourceExe = Join-Path $tempExtract $exeName
if (-not (Test-Path -LiteralPath $sourceExe)) {
    Write-Host "Erro: O arquivo ControllerOverlay.exe não foi encontrado no ZIP." -ForegroundColor Red
    exit 1
}

Write-Host "Parando processos existentes..."
Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Copiando para a pasta de instalacao..."
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

Write-Host "Criando atalhos..."
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

Write-Host "Registrando no Painel de Controle..."
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\ControllerOverlay'
New-Item -Path $uninstallKey -Force | Out-Null
$estimatedSizeKb = [int][Math]::Ceiling((Get-Item -LiteralPath $targetExe).Length / 1KB)

New-ItemProperty -Path $uninstallKey -Name 'DisplayName' -Value 'ControllerOverlay' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayVersion' -Value '1.0.0' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'Publisher' -Value 'CATATAU' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'URLInfoAbout' -Value 'https://github.com/Catatatau' -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'InstallLocation' -Value $InstallDir -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'DisplayIcon' -Value $targetExe -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$uninstallPath`"" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'EstimatedSize' -Value $estimatedSizeKb -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoModify' -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name 'NoRepair' -Value 1 -PropertyType DWord -Force | Out-Null

Write-Host "Limpando arquivos temporarios..."
Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Instalacao concluida com sucesso!" -ForegroundColor Green
Start-Process -FilePath $targetExe
