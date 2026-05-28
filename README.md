# Controller Overlay for Rocket League

A standalone Windows executable controller overlay that displays Xbox, PlayStation, and generic controller inputs in real-time. Designed to be lightweight, modern, and transparent, sitting perfectly above Rocket League or any other game.

## Features
- **Standalone `.exe`**: Portable and lightweight, requiring no installation.
- **Installer**: Build a Windows setup executable that installs shortcuts and an uninstaller.
- **Always on top**: Works perfectly over games running in Borderless Windowed or Windowed mode.
- **Click-Through Mode**: Play your game seamlessly without accidentally interacting with the overlay.
- **Multi-Controller Support**: Native support for XInput (Xbox) and DirectInput (PlayStation DualShock/DualSense and Generic) controllers.
- **Customizable UI**: Choose between Xbox and PlayStation layouts, adjust themes, scale, opacity, deadzone, and accent colors.
- **Global Hotkeys**: Easily hide or tweak the overlay mid-game.
- **HUD Metrics**: Optional FPS and ball speed readout.
- **Game FPS Source**: Reads game FPS from RTSS/RivaTuner shared memory when available, with ETW and local Stats API fallback.
- **Safe Ball Telemetry**: Ball speed can be received from a local TCP/WebSocket Stats API source without reading game memory.
- **Movable HUD**: Drag the FPS/ball speed panel when click-through is disabled.

## How to Use with Rocket League
1. Launch **Rocket League**.
2. Go to Settings -> Video and set your Display Mode to **Borderless** or **Windowed**. (Exclusive Fullscreen prevents external overlays from rendering on top).
3. Run `ControllerOverlay.exe`.
4. Connect your controller (it supports hot-plugging).
5. Ensure the overlay is positioned where you like it. (Click and drag anywhere on the overlay if "Lock Position" is off).
6. Press `Ctrl + Shift + C` to open the settings panel and adjust click-through, scale, background, and metrics.
7. Enjoy!

## Default Hotkeys
- `Ctrl + Shift + O`: Show/Hide Overlay
- `Ctrl + Shift + C`: Open Settings Window

## Installer
The latest local installer is generated at:

```txt
dist\ControllerOverlay-Setup-1.0.0.exe
```

It installs the app to `%LOCALAPPDATA%\Programs\ControllerOverlay`, creates Desktop and Start Menu shortcuts, and registers an uninstaller in Windows.

To rebuild the installer:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\build-installer.ps1
```

## Ball Speed Telemetry
The overlay does not read or hook game memory. To show ball speed, provide a local TCP or WebSocket Stats API server on the configured port, default `49123`.

Supported payloads:
```json
{"Event":"UpdateState","Data":{"Game":{"BallSpeed":123.4}}}
```

You can change the Stats API port in the settings window.

## Game FPS
For FPS matching the game, run RTSS/RivaTuner Statistics Server. The overlay reads RTSS shared memory first and can fall back to ETW when running as administrator.

As a fallback, provide FPS over the local Stats API on the configured port, default `49123`.

Supported payloads:
```json
{"Event":"UpdateState","Data":{"Game":{"FPS":65}}}
```

You can drag the HUD box while click-through is off. Its position is saved automatically.

## Settings & Configuration
You can open the Settings menu by pressing `Ctrl + Shift + C`.
Settings are automatically saved to your `%APPDATA%\ControllerOverlay\settings.json` file.

- **Theme / Layout**: Swap between Xbox (A, B, X, Y) and PlayStation (Cross, Circle, Square, Triangle).
- **Stick Deadzone**: Adjust the analog stick sensitivity. Default is 0.08.
- **Lock Position**: Prevents accidentally dragging the overlay.

## Troubleshooting & Anti-Cheat
- **Is this a cheat?**: No. This app only reads physical controller inputs from Windows natively using `XInput` and `DirectInput`. It does **not** hook into game memory, read game files, or interact with Rocket League in any way. It is completely safe.
- **My controller is not detected**: If you are using a PlayStation controller, ensure it's recognized by Windows. If it acts erratically, DS4Windows is compatible and will provide an XInput translation.
- **I can't click the settings gear**: If click-through is enabled, you cannot click the overlay. Use `Ctrl + Shift + C` to open settings and disable click-through.

## Build Instructions (Developers)
Requires .NET 8.0 SDK.
Run the following command to generate a portable executable:
```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
The executable will be located in `bin\Release\net8.0-windows\win-x64\publish\`.

Run the following command to generate the installer:
```cmd
powershell.exe -NoProfile -ExecutionPolicy Bypass -File installer\build-installer.ps1
```
