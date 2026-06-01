@echo off
setlocal
set "SOURCE_DIR=%~dp0"
if "%SOURCE_DIR:~-1%"=="\" set "SOURCE_DIR=%SOURCE_DIR:~0,-1%"

echo Installing ControllerOverlay...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SOURCE_DIR%\install.ps1" -SourceDir "%SOURCE_DIR%" -Launch
if errorlevel 1 (
    echo.
    echo ControllerOverlay installation failed.
    echo Send the error above to the developer.
    exit /b 1
)
exit /b 0
