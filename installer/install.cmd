@echo off
setlocal
echo Installing ControllerOverlay...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -SourceDir "%~dp0" -Launch
if errorlevel 1 (
    echo.
    echo ControllerOverlay installation failed.
    echo Send the error above to the developer.
    pause
    exit /b 1
)
exit /b 0
