<div align="center">
  
# 🎮 Controller Overlay for Rocket League

*A standalone Windows executable controller overlay that displays Xbox, PlayStation, and generic controller inputs in real-time. Designed to be lightweight, modern, and transparent, sitting perfectly above Rocket League or any other game.*

[![GitHub Release](https://img.shields.io/github/v/release/Catatatau/ControllerOverlay?style=for-the-badge&color=2ea44f)](https://github.com/Catatatau/ControllerOverlay/releases)
[![Downloads](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2FCatatatau%2FControllerOverlay%2Fmain%2Fbadges%2Fdownloads.json)](https://github.com/Catatatau/ControllerOverlay/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

</div>

---

## ✨ Features

- 🚀 **Standalone `.exe`**: Portable and lightweight, requiring no installation dependencies.
- 📦 **One-Click Installer**: Automatically sets up the app, adds shortcuts to your Desktop/Start Menu, and registers an uninstaller.
- 📌 **Always on Top**: Works perfectly over games running in Borderless Windowed or Windowed mode.
- 👻 **Click-Through Mode**: Play your game seamlessly without accidentally interacting with the overlay.
- 🎮 **Multi-Controller Support**: Native support for XInput (Xbox) and DirectInput (PlayStation DualShock/DualSense and Generic).
- 🎨 **Customizable UI**: Choose between Xbox and PlayStation layouts, adjust themes, scale, opacity, deadzone, and accent colors.
- ⌨️ **Global Hotkeys**: Easily hide or tweak the overlay mid-game without alt-tabbing.
- 📊 **HUD Metrics**: Optional FPS and ball speed readout.
- 🔄 **Smart Game FPS Source**: Reads game FPS from RTSS/RivaTuner shared memory when available, with ETW and local Stats API fallback.
- ⚽ **Safe Ball Telemetry**: Ball speed can be received from a local UDP, TCP, or WebSocket Stats API source without reading game memory.
- 🖐️ **Movable HUD**: Drag the FPS/ball speed panel freely when click-through is disabled.

---

## ⚡ Installation

To install **ControllerOverlay** on any computer, simply run the following command in PowerShell:

```powershell
irm https://raw.githubusercontent.com/Catatatau/ControllerOverlay/main/scripts/Install.ps1 | iex
```

Or from CMD:

```cmd
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "irm https://raw.githubusercontent.com/Catatatau/ControllerOverlay/main/scripts/Install.ps1 | iex"
```

> **Note:** This command downloads the latest `ControllerOverlay.exe` from GitHub Releases, installs the app to `%LOCALAPPDATA%\Programs\ControllerOverlay`, creates your shortcuts, and makes it easy to uninstall later.

---

Direct executable download:

```text
https://github.com/Catatatau/ControllerOverlay/releases/latest/download/ControllerOverlay.exe
```

---

## 🕹️ How to Use with Rocket League

1. Launch **Rocket League**.
2. Go to **Settings -> Video** and set your Display Mode to **Borderless** or **Windowed**. *(Exclusive Fullscreen prevents external overlays from rendering on top).*
3. Run `ControllerOverlay.exe` from your Desktop.
4. Connect your controller (hot-plugging is fully supported).
5. Position the overlay where you like it by clicking and dragging (make sure "Lock Position" is off).
6. Press `F2` to open the settings panel and customize your experience.
7. Enjoy!

---

## ⌨️ Default Hotkeys

| Shortcut | Action |
| --- | --- |
| `Ctrl + Shift + O` | 👁️ Show/Hide Overlay |
| `F2` | ⚙️ Open Settings Window |

---

## ⚙️ Settings & Configuration

Settings are automatically saved to your `%APPDATA%\ControllerOverlay\settings.json` file.

- **Theme / Layout**: Swap between Xbox (A, B, X, Y) and PlayStation (Cross, Circle, Square, Triangle).
- **Stick Deadzone**: Adjust the analog stick sensitivity. Default is `0.08`.
- **Lock Position**: Prevents accidentally dragging the overlay.

---

## 📡 Advanced: Telemetry & FPS

### Ball Speed Telemetry
The overlay does not read or hook game memory. To show ball speed, provide a local UDP packet source or TCP/WebSocket Stats API server on the configured port (default `49123`).

Supported payload format:
```json
{"Event":"UpdateState","Data":{"Game":{"BallSpeed":123.4}}}
```

### Game FPS
For accurate game FPS tracking, run **RTSS/RivaTuner Statistics Server**. The overlay reads RTSS shared memory natively. As a fallback, it will use ETW (requires running as administrator) or the local Stats API.

Supported Stats API payload format for FPS:
```json
{"Event":"UpdateState","Data":{"Game":{"FPS":65}}}
```

---

## ❓ Troubleshooting & Anti-Cheat

- **Is this a cheat?**  
  **No.** This app only reads physical controller inputs from Windows natively using `XInput` and `DirectInput`. It does **not** hook into game memory, read game files, or interact with Rocket League in any way. It is completely safe to use.
- **My controller is not detected:**  
  If you are using a PlayStation controller, ensure it's recognized by Windows. If it acts erratically, using [DS4Windows](https://ds4-windows.com/) will provide perfect XInput translation.
- **I can't click the settings gear:**  
  If "Click-Through" is enabled, you cannot click the overlay. Use `F2` to open settings and disable click-through.

---

## 🛠️ Build Instructions (Developers)

Requires **.NET 8.0 SDK**. Run the following command to generate a portable standalone executable:

```cmd
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

> *The project is configured to automatically build and create a new Release via GitHub Actions on every push to the `main` branch.*

<div align="center">
  <br/>
  Made with ❤️ by Catatau
</div>
