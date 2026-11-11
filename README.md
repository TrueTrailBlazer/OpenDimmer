# OpenDimmer

**OpenDimmer** is a lightweight, portable, open-source multi-monitor brightness and Night Light (warm tint) controller for Windows 10/11.

---

## Why OpenDimmer Was Created (Origin & Motivation)

When working at night across a dual-monitor setup (such as a laptop screen paired with an external ultrawide monitor), hardware monitor controls are often slow, inconsistent, or unable to lower screen luminance enough to prevent eye strain.

For years, the classic freeware utility **Dimmer** (by Nelson Pires) was a popular solution for software dimming. However, because it is closed-source and unmaintained since 2022, several critical architectural bugs remained unfixed on modern Windows 10/11 systems:
1. **Loss of Dimming Filter on Popups & Photos:** Opening an image, a browser download popup, or interacting with split-screen windows caused the dimming overlay to drop behind the active window—or disappear on half of the screen while remaining checked in the UI.
2. **Show Desktop (`Win + D`) & Focus Drops:** Attaching overlays to `GetDesktopWindow()` caused Windows to demote the dimming layer whenever the user interacted with the desktop or switched monitors.
3. **Sleep / Wake & Hotplug Desynchronization:** Waking a laptop from sleep or reconnecting an HDMI/DisplayPort cable caused overlays to stack on a single monitor or reset the configuration file.
4. **Stacked Black Screens:** Launching the executable a second time while minimized in the tray spawned duplicate instances, stacking opacity and turning screens pitch black.
5. **Auto-Hide Taskbar Lock:** Fullscreen topmost overlays blocked the 1-pixel bottom edge trigger required for Windows auto-hidden taskbars to pop up.

**OpenDimmer** was born out of the need to solve all of these multi-monitor limitations definitively while providing a modern, polished dark-mode mixer interface.

---

## Clean-Room Implementation & Inspiration

- **100% Original Code (Zero Copied Code):** OpenDimmer is a **clean-room implementation written from scratch** in C# / WPF (.NET Framework 4.7.2+). No source code, XAML/BAML resources, or binaries from the original *Dimmer* were copied or reused.
- **Inspiration:** We credit and thank Nelson Pires' original *Dimmer* for inspiring the compact vertical multi-monitor slider concept, which we re-imagined with a modern hardware mixer UI, native Win32 event hooks, EDID monitor detection, and color temperature controls.

---

## Key Features

- **Persistent Z-Order Overlay Protection (`WinEventHook` + `SWP_NOACTIVATE`):** Actively monitors window foreground and popup creation events (`EVENT_SYSTEM_FOREGROUND` & `EVENT_OBJECT_SHOW`) to keep dimming overlays above opened photos, notifications, and popups without ever stealing keyboard/mouse focus.
- **EDID Hardware Monitor Detection:** Automatically reads the real manufacturer/model name of each display (e.g., *LG Ultrawide*, *Notebook Display*) and connection type (*HDMI*, *DP*, *eDP*) directly from hardware EDID registry descriptors.
- **Per-Monitor Color Filter & Night Light (`Tint`):** Choose between **Neutral Black (`6500K`)**, **Warm Amber (`4500K`)**, **Deep Ember (`2200K`)**, and **Soft Sepia (`3200K`)** with adjustable warmth intensity per monitor—or toggle all displays at once using **Night Sync**.
- **Hybrid Hardware Gamma Mode (`SetDeviceGammaRamp`):** Optional hardware LUT gamma dimming that also darkens the Windows Start Menu, Taskbar, system popups, and hardware mouse cursor.
- **Dynamic Sleep & Display Hotplug Recovery:** Listens to `SystemEvents.DisplaySettingsChanged` and `SystemEvents.PowerModeChanged` to realign overlays automatically when docking/undocking monitors—saving settings non-destructively by hardware `DeviceName`.
- **Auto-Hide Taskbar Compatibility:** Detects `ABS_AUTOHIDE` via `SHAppBarMessage` and preserves a 1px trigger edge so auto-hidden taskbars always slide up smoothly.
- **True Single-Instance Guard:** Uses a global `Mutex` and `EventWaitHandle` IPC wakeup so launching `OpenDimmer.exe` again wakes the existing tray instance instead of stacking dark overlays.

---

## Project Structure

```text
OpenDimmer/
├── build.ps1                         # Zero-dependency build script (uses native Windows .NET 4 compiler)
├── DESIGN.md                         # UI/UX design system specification
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
        │   ├── DisplayInfoService.cs # EDID & Win32 hardware monitor name detection
        │   ├── GammaService.cs       # Per-monitor hardware LUT gamma & warm tint
        │   ├── SettingsService.cs    # Non-destructive pretty-printed JSON persistence
        │   └── OverlayManager.cs     # Multi-monitor overlay lifecycle & Z-Order protection
        └── UI/
            ├── OverlayWindow.cs      # Click-through per-monitor dimming & warmth surface
            └── MainWindow.cs         # Modern dark mixer UI, custom Tint popup & system tray
```

---

## Building from Source

You can compile **OpenDimmer** on any Windows 10/11 machine without installing external SDKs:

```powershell
.\build.ps1
```

The standalone portable executable (~40 KB) will be generated in `dist\OpenDimmer.exe`.

---

## License

MIT License — see [LICENSE](LICENSE).
