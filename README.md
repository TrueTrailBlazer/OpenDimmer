# OpenDimmer

Lightweight, portable, open-source multi-monitor brightness & screen dimmer utility for Windows.

Built from scratch in C# / WPF (.NET Framework 4.7.2+) to solve the long-standing multi-monitor Z-Order, DPI, and sleep/wake bugs found in legacy screen dimmers.

## Key Improvements & Architecture

- **Persistent Z-Order Guardian (`WinEventHook` + `SWP_NOACTIVATE`):** Prevents opened photos, browser download popups, and snapped windows from jumping in front of the dimming overlay without stealing focus.
- **No `GetDesktopWindow()` Owner Bug:** Overlays use `WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` so `Win + D` (Show Desktop), `Alt + Tab`, and switching focus across monitors never drop the filter.
- **Dynamic Multi-Monitor & Sleep Recovery:** Listens to `SystemEvents.DisplaySettingsChanged` and `SystemEvents.PowerModeChanged` to automatically realign overlays when waking from sleep or docking/undocking external monitors—without deleting your saved configuration.
- **True Single-Instance Guard:** Launching `OpenDimmer.exe` while it is already running in the system tray wakes and focuses the existing instance instead of stacking multiple dark overlays.
- **Per-Monitor DPI Awareness (`PerMonitorV2`):** Accurate physical bounds across mixed-DPI setups (e.g., laptop screen + external LG monitor).

## Project Structure

```text
OpenDimmer/
├── build.ps1                         # Zero-dependency build script (uses native Windows .NET 4 compiler)
├── OpenDimmer.sln                    # Visual Studio Solution
└── src/
    └── OpenDimmer/
        ├── OpenDimmer.csproj
        ├── app.manifest              # PerMonitorV2 DPI manifest
        ├── Program.cs                # Single-instance entry point & IPC wakeup
        ├── Models/
        │   └── AppSettings.cs        # Portable JSON configuration models
        ├── Services/
        │   ├── NativeMethods.cs      # Win32 P/Invoke (Z-Order, WinEventHook, Gamma, AppBar)
        │   ├── AutoRunService.cs     # Windows startup registry integration
        │   ├── GammaService.cs       # Per-monitor hardware LUT gamma & warm tint
        │   ├── SettingsService.cs    # Non-destructive settings persistence
        │   └── OverlayManager.cs     # Multi-monitor overlay lifecycle & Z-Order enforcement
        └── UI/
            ├── OverlayWindow.cs      # Click-through per-monitor dimming surface
            └── MainWindow.cs         # Dark control panel UI & system tray integration
```

## Building

You can compile **OpenDimmer** on any Windows 10/11 machine without installing external SDKs:

```powershell
.\build.ps1
```

The portable executable will be generated in `dist\OpenDimmer.exe`.

## License

MIT License — see [LICENSE](LICENSE).
