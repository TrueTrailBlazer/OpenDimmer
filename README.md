<p align="center">
  <img src="assets/icon.png" alt="OpenDimmer Icon" width="112" />
</p>

<h1 align="center">OpenDimmer</h1>

<p align="center">
  <strong>Lightweight, portable, open-source multi-monitor brightness &amp; Night Light (warm tint) controller for Windows 10/11 and Linux (X11/XRandR).</strong>
</p>

<p align="center">
  <a href="https://github.com/TrueTrailBlazer/OpenDimmer/releases/latest"><strong>Latest Release</strong></a> •
  <a href="https://github.com/TrueTrailBlazer/OpenDimmer/releases/latest/download/OpenDimmer-win-portable.exe"><strong>Download for Windows (.exe)</strong></a> •
  <a href="https://github.com/TrueTrailBlazer/OpenDimmer/releases/latest/download/OpenDimmer-linux-x64.tar.gz"><strong>Download for Linux (x64)</strong></a>
</p>

---

## Download & Quick Start

| Platform | Package | Size & Runtime | How to Run |
| :--- | :--- | :--- | :--- |
| **Windows 10 / 11** | [`OpenDimmer-win-portable.exe`](https://github.com/TrueTrailBlazer/OpenDimmer/releases/latest/download/OpenDimmer-win-portable.exe) | `~200 KB` portable (uses built-in `.NET Framework 4.7.2`, zero install) | Download and run `OpenDimmer-win-portable.exe`. |
| **Linux (X11 / XRandR)** | [`OpenDimmer-linux-x64.tar.gz`](https://github.com/TrueTrailBlazer/OpenDimmer/releases/latest/download/OpenDimmer-linux-x64.tar.gz) | Self-contained single-file binary (`.NET 8 + Avalonia UI`) | Extract (`tar -xzf OpenDimmer-linux-x64.tar.gz`) and run `./OpenDimmer`. |

### Is OpenDimmer Safe? (About the Windows SmartScreen Prompt)

When opening `OpenDimmer-win-portable.exe` for the first time on Windows, you may see a blue **"Windows protected your PC"** (*Microsoft Defender SmartScreen*) window. **This does not mean a virus or threat was detected:**

- **Why the prompt appears:** Windows automatically displays this message for any new independent application whose developer has not purchased a paid commercial Code Signing certificate (which costs hundreds of dollars per year).
- **100% Open Source & Auditable:** Every single line of code is public in this repository, with zero obfuscation, zero installers, and zero hidden background services.
- **100% Offline & Zero Telemetry:** OpenDimmer never connects to the internet, collects no data, and only saves your display settings in a readable `OpenDimmer.json` file.
- **Built Transparently by GitHub:** Every official executable in [Releases](https://github.com/TrueTrailBlazer/OpenDimmer/releases) is compiled automatically on GitHub's own servers via [GitHub Actions](https://github.com/TrueTrailBlazer/OpenDimmer/actions) directly from the public source code (with verifiable SHA-256 checksums in `SHA256SUMS.txt`)—not uploaded from a private computer.
- **How to open on first launch:** Click **More info** → **Run anyway** (Windows will remember your choice and won't ask again). Or, if you prefer, you can inspect the `.cs` files and compile your own executable locally in 1 second by running `.\build.ps1`.

---

## Why OpenDimmer Was Created (Origin & Motivation)

When working at night across a dual-monitor setup (such as a laptop screen paired with an external ultrawide monitor), hardware monitor controls are often slow, inconsistent, or unable to lower screen luminance enough to prevent eye strain.

For years, the classic freeware utility **Dimmer** (by Nelson Pires) was a popular solution for software dimming. However, because it is closed-source and unmaintained since 2022, several critical architectural bugs remained unfixed on modern Windows 10/11 systems:
1. **Loss of Dimming Filter on Popups & Photos:** Opening an image, a browser download popup, or interacting with split-screen windows caused the dimming overlay to drop behind the active window—or disappear on half of the screen while remaining checked in the UI.
2. **Show Desktop (`Win + D`) & Focus Drops:** Attaching overlays to `GetDesktopWindow()` caused Windows to demote the dimming layer whenever the user interacted with the desktop or switched monitors.
3. **Sleep / Wake & Hotplug Desynchronization:** Waking a laptop from sleep or reconnecting an HDMI/DisplayPort cable caused overlays to stack on a single monitor or reset the configuration file.
4. **Stacked Black Screens:** Launching the executable a second time while minimized in the tray spawned duplicate instances, stacking opacity and turning screens pitch black.
5. **Auto-Hide Taskbar Lock:** Fullscreen topmost overlays blocked the 1-pixel bottom edge trigger required for Windows auto-hidden taskbars to pop up.

**OpenDimmer** was born out of the need to solve all of these multi-monitor limitations definitively while providing a modern, polished dark-mode mixer interface across **Windows** and **Linux**.

---

## Clean-Room Implementation & Inspiration

- **100% Original Code (Zero Copied Code):** OpenDimmer is a **clean-room implementation written from scratch** in C#. No source code, XAML/BAML resources, or binaries from the original *Dimmer* were copied or reused.
- **Inspiration:** We credit and thank Nelson Pires' original *Dimmer* for inspiring the compact vertical multi-monitor slider concept, which we re-imagined with a modern hardware mixer UI, native Win32 & X11/XRandR backends, EDID monitor detection, and color temperature controls.

---

## Key Features

- **Dual-OS Native Architecture (`OpenDimmer.Core`):** Shared business logic with dedicated native frontends—a tiny `~200 KB` **WPF + Win32** executable on Windows and a native **Avalonia UI + X11/XRandR** application on Linux.
- **Persistent Z-Order Overlay Protection on Windows (`WinEventHook` + `SWP_NOACTIVATE`):** Actively monitors window foreground and popup creation events (`EVENT_SYSTEM_FOREGROUND` & `EVENT_OBJECT_SHOW`) to keep dimming overlays above opened photos, notifications, and popups without ever stealing keyboard/mouse focus.
- **Native Hardware CRTC Gamma on Linux (`libXrandr`):** Uses `XRRSetCrtcGamma` per monitor (supporting 8-bit 256-entry and 10-bit 1024-entry LUTs), preserving and restoring each display's original calibrated LUT on exit.
- **EDID Hardware Monitor Detection:** Automatically reads the real manufacturer/model name of each display (e.g., *LG Ultrawide*, *Notebook Display*) and connection port (*HDMI*, *DP*, *eDP*) directly from Windows Registry EDID descriptors or Linux `/sys/class/drm/card*-*/edid`.
- **Per-Monitor Color Filter & Night Light (`Tint`):** Choose between **Neutral Black (`6500K`)**, **Warm Amber (`4500K`)**, **Deep Ember (`2200K`)**, and **Soft Sepia (`3200K`)** with adjustable warmth intensity per monitor—or toggle all displays at once using **Night Sync**.
- **Hybrid Hardware Gamma Mode on Windows (`SetDeviceGammaRamp`):** Optional hardware LUT gamma dimming that also darkens the Windows Start Menu, Taskbar, system popups, and hardware mouse cursor.
- **Dynamic Sleep & Display Hotplug Recovery:** Realigns monitors non-destructively by hardware `DeviceName` without wiping disconnected monitor settings.
- **Auto-Hide Taskbar Compatibility (Windows):** Detects `ABS_AUTOHIDE` via `SHAppBarMessage` and preserves a 1px trigger edge so auto-hidden taskbars always slide up smoothly.
- **True Single-Instance Guard:** Uses a global `Mutex` + `EventWaitHandle` on Windows and a Unix Domain Socket on Linux so launching OpenDimmer again wakes the existing tray instance instead of stacking dark overlays.

---

## Project Structure

```text
OpenDimmer/
├── .github/workflows/release.yml     # Automated Windows + Linux build, test & release pipeline
├── assets/
│   ├── app.ico                       # Multi-resolution Windows executable & tray icon (16–256px)
│   └── icon.png                      # High-resolution OpenDimmer application icon (1024px)
├── build.ps1                         # Zero-dependency Windows build script (.NET 4.7.2 csc.exe)
├── run-linux.sh                      # Quick Linux launch script (.NET 8 + Avalonia + X11)
├── DESIGN.md                         # UI/UX design system specification
├── OpenDimmer.sln                    # Visual Studio / .NET Solution
├── src/
│   ├── OpenDimmer.Core/              # Shared cross-platform core (Models, DimmingMath, EdidParser, SettingsStore)
│   ├── OpenDimmer.Windows/           # Windows 10/11 target (WPF, Win32 overlay, GammaService, EDID registry)
│   └── OpenDimmer.Linux/             # Linux target (.NET 8, Avalonia UI, libX11/libXrandr, DRM EDID)
└── tests/
    └── OpenDimmer.Core.Tests/        # xUnit automated test suite for OpenDimmer.Core
```

---

## Building from Source

### Windows 10 / 11 (Zero External SDKs Required)

You can compile the standalone `~200 KB` portable executable on any Windows 10/11 machine using the built-in .NET Framework compiler:

```powershell
.\build.ps1
```

The executable will be generated in `dist\OpenDimmer.exe`.

### Linux (Ubuntu / Debian / KDE / XFCE / X11)

Requires `.NET 8 SDK` and standard X11 libraries (`libX11.so.6`, `libXrandr.so.2`):

```bash
# Run unit tests
dotnet test tests/OpenDimmer.Core.Tests/OpenDimmer.Core.Tests.csproj -c Release

# Launch OpenDimmer natively on X11
./run-linux.sh
```

---

## License

MIT License — see [LICENSE](LICENSE).
