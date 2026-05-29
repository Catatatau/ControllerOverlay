# ControllerOverlay

ControllerOverlay is a lightweight Windows overlay for Rocket League and other games. It can show controller inputs, keyboard/mouse inputs, FPS, and ball speed in a movable transparent overlay.

## Automatic Install

Run this command in PowerShell:

```powershell
irm https://raw.githubusercontent.com/Catatatau/ControllerOverlay/main/scripts/Install.ps1 | iex
```

The script downloads the latest `ControllerOverlay-Setup-*.exe` from GitHub Releases, runs the installer, creates Desktop/Start Menu shortcuts, and registers an uninstaller in Windows.

Manual download:

```text
https://github.com/Catatatau/ControllerOverlay/releases/latest
```

## Features

- Xbox and PlayStation controller overlay.
- Keyboard/mouse overlay with multiple presets.
- FPS HUD from RTSS, ETW, or local Stats API fallback.
- Ball speed HUD from local TCP/WebSocket Stats API.
- Movable FPS/ball speed HUD with optional transparent background.
- Click-through mode for playing without interacting with the overlay.
- Settings panel with `Ctrl + Shift + C`.
- Show/hide overlay with `Ctrl + Shift + O`.

## Keyboard/Mouse Presets

The keyboard overlay is inspired by the selectable-layout idea from NohBoard, but the presets are implemented directly in this app.

Available presets:

- `FPS Compacto`: compact Q/W/E/R, A/S/D/F, Shift/Ctrl/Space, and mouse buttons.
- `WASD + Mouse`: minimal movement keys plus mouse buttons.
- `FPS Completo`: number row, FPS keys, modifiers, space, and mouse buttons.
- `Rocket League`: useful Rocket League keyboard controls in a compact shape.
- `Setas + Mouse`: arrow key layout with mouse buttons.
- `Numpad`: numeric keypad layout.

To use them:

1. Open settings with `Ctrl + Shift + C`.
2. Set `Modelo` to `Teclado/Mouse`.
3. Choose a preset in `Modelo teclado`.

## Rocket League Setup

1. Open Rocket League.
2. Use Borderless or Windowed mode.
3. Start ControllerOverlay.
4. Move the overlay where you want it.
5. Use `Ctrl + Shift + C` to open settings.

Exclusive fullscreen can prevent external overlays from appearing above the game.

## Stats API

The overlay does not hook or edit Rocket League memory. Ball speed can come from a local TCP/WebSocket Stats API on the configured port, default `49123`.

Example payload:

```json
{"Event":"UpdateState","Data":{"Game":{"BallSpeed":123.4,"FPS":65}}}
```

## Development

Build:

```powershell
dotnet build src/ControllerOverlay/ControllerOverlay.csproj -c Release
```

Build installer:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\installer\build-installer.ps1
```

The installer is generated in:

```text
dist\ControllerOverlay-Setup-1.1.0.exe
```
