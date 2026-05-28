[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'ControllerOverlay.csproj'
$distDir = Join-Path $repoRoot 'dist'
$payloadDir = Join-Path $distDir 'installer-payload'
$installerPath = Join-Path $distDir "ControllerOverlay-Setup-$Version.exe"
$sedPath = Join-Path $distDir 'ControllerOverlay-Setup.sed'

$repoFull = [System.IO.Path]::GetFullPath($repoRoot)
$distFull = [System.IO.Path]::GetFullPath($distDir)
if (-not $distFull.StartsWith($repoFull, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write outside repository root: $distFull"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null
if (Test-Path -LiteralPath $payloadDir) {
    Remove-Item -LiteralPath $payloadDir -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:Version=$Version `
    -o $payloadDir

$exePath = Join-Path $payloadDir 'ControllerOverlay.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Publish did not produce ControllerOverlay.exe"
}

Get-ChildItem -LiteralPath $payloadDir -File |
    Where-Object { $_.Name -notin @('ControllerOverlay.exe') } |
    Remove-Item -Force

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1') -Destination (Join-Path $payloadDir 'install.ps1') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.cmd') -Destination (Join-Path $payloadDir 'install.cmd') -Force

$iexpress = Join-Path $env:WINDIR 'System32\iexpress.exe'
if (-not (Test-Path -LiteralPath $iexpress)) {
    throw "IExpress was not found at $iexpress"
}

$payloadWithSlash = $payloadDir.TrimEnd('\') + '\'
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=ControllerOverlay instalado com sucesso.
TargetName=$installerPath
FriendlyName=ControllerOverlay Setup
AppLaunched=cmd /c install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles

[Strings]
FILE0="install.cmd"
FILE1="install.ps1"
FILE2="ControllerOverlay.exe"

[SourceFiles]
SourceFiles0=$payloadWithSlash

[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
"@

Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

if (Test-Path -LiteralPath $installerPath) {
    Remove-Item -LiteralPath $installerPath -Force
}

& $iexpress /N /Q $sedPath

$deadline = [DateTime]::UtcNow.AddSeconds(90)
while (-not (Test-Path -LiteralPath $installerPath) -and [DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Milliseconds 500
}

if (-not (Test-Path -LiteralPath $installerPath)) {
    throw "IExpress did not produce $installerPath"
}

$lastLength = -1
$stableChecks = 0
while ($stableChecks -lt 3 -and [DateTime]::UtcNow -lt $deadline) {
    $currentLength = (Get-Item -LiteralPath $installerPath).Length
    if ($currentLength -eq $lastLength -and $currentLength -gt 0) {
        $stableChecks++
    }
    else {
        $stableChecks = 0
        $lastLength = $currentLength
    }

    Start-Sleep -Milliseconds 500
}

Get-Item -LiteralPath $installerPath
