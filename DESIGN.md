# OpenDimmer — UI/UX Design Specification (`DESIGN.md`)

## 1. Product Overview
**OpenDimmer** is a lightweight, portable desktop utility for Windows 10/11 that controls screen brightness (dimming) and blue-light warmth (tint) across multiple monitors independently or via a unified Master channel.

The interface should feel like a **native, modern Windows 11 dark-mode system flyout / hardware utility** (inspired by *Twinkle Tray*, *EarTrumpet*, and a modernized version of classic *Dimmer*):
- **Simple, compact, and functional** — zero visual clutter, no scattered buttons, and no clipped labels or ugly legacy white scrollbars.
- **Responsive & Resizable** — works cleanly at a compact size (`320×360 px`) and scales gracefully when resized larger.

---

## 2. Visual Direction & Color Palette (Dark Mode)
- **Window Background (Surface 0):** `#181B20` (Deep matte charcoal)
- **Card / Panel Surface (Surface 1):** `#22272E` with `1px` border `#2D333B` and `8px` border radius
- **Master Card Surface (Accented Surface):** `#1F2937` with subtle accent border `#3B82F6` (at 35% opacity) to visually distinguish the Master channel (`M`) from individual monitors (`1`, `2`, ...)
- **Primary Accent:** `#3B82F6` (Electric Blue — active toggles, slider fill, active segmented tab)
- **Warm Tint Accent:** `#F59E0B` (Amber — used when a warm Night Light filter is active on a screen)
- **Typography:** `Segoe UI Variable` / `Segoe UI` / `Inter`
  - Primary Text: `#F3F4F6`
  - Secondary / Muted Text: `#9CA3AF`
  - Numeric Value Readout (`Consolas` or tabular numerals): `#E5E7EB`

---

## 3. Navigation Architecture (Eliminating Tab Clutter)
Instead of 5 cramped horizontal text tabs (`Screens`, `Color Filter`, `Options`, `About`, `Debug`) that overflow and get cut off on narrow windows:

### Top Header Bar (`Height: 40px`)
- **Left:** App Icon (subtle glowing sun/dimmer icon) + Title **`OpenDimmer`** (`13px SemiBold`).
- **Center / Right Segmented Pill (2 Views Only):**
  1. **`Screens`** (Sliders & per-screen warmth)
  2. **`Settings`** (Consolidated Options, Diagnostics & About in a clean scrollable list)
- **Far Right Window Controls:** Minimal icon buttons (`—` Minimize, `▢` Maximize/Restore, `✕` Close) with subtle hover states (`#2D333B`, red `#E11D48` on Close hover).

---

## 4. View 1: `Screens` (Main Mixer View)
A side-by-side vertical mixer layout (`UniformGrid` or flex row) where each column is a self-contained **Channel Strip Card**:

### Channel Strip Card (`M` Master + `1`, `2`... Monitors)
1. **Top Toggle Header:**
   - Modern custom checkbox or pill toggle with the channel badge (`M`, `1`, `2`).
   - Clicking `M` enables all monitors and syncs them to `M`'s brightness level.
   - Toggling an individual monitor (`1` or `2`) controls only that monitor and automatically disengages the `M` lock.
2. **Center Vertical Slider (Flexible Height `1fr`):**
   - Custom modern track (`6px` wide rounded bar, `#111418` background, `#3B82F6` active fill from bottom to thumb).
   - Clean circular or rounded-pill thumb (`16px`) with subtle elevation shadow.
   - Subtle tick marks on the sides (`0%` to `90%`).
   - Responds to mouse drag and mouse wheel scroll anywhere over the card.
3. **Bottom Numeric Pill & Tint Dot:**
   - Compact dark input pill (`#14171C`, border `#2D333B`, `44×24px`, centered text `20`–`90`) that accepts direct keyboard typing.
   - On monitor strips (`1`, `2`), a small circular **Tint Indicator Dot (`10px`)** next to or below the number that opens a clean popover/cycle for that screen's color filter (*Neutral Black*, *Warm Amber*, *Night Light*, *Soft Sepia*) with a clear label on hover — avoiding a separate cluttered tab.

---

## 5. View 2: `Settings` (Consolidated Options, Color Filter & Info)
A single, clean vertical list with a **thin, dark custom scrollbar (`4px` thumb `#374151`)** divided into 3 compact grouped cards:

### Group A: Color Filter (Night Light / Tint)
- Short 1-line subtitle: *"Reduce eye strain at night by adding a warm tint to the dimming filter."*
- One compact row per monitor (`Screen 1`, `Screen 2`) with a sleek dark dropdown (`Neutral Black`, `Warm Amber`, `Deep Ember`, `Soft Sepia`).

### Group B: Behavior & Display
Clean rows with label on the left and a modern **Switch / Checkbox** on the right (never truncating text):
- **Show screen numbers (`1`, `2`)** — Displays large monitor ID overlay
- **Show screen borders** — Highlights monitor edges
- **Proportional Master mode** — Preserves brightness offset between screens when moving `M`
- **Hardware Gamma mode** — Also dims Start Menu, Taskbar, and mouse cursor
- **Auto-Hide Taskbar fix** — Leaves 1px trigger edge for auto-hidden taskbars
- **Keep panel undimmed** — Keeps OpenDimmer above the dark overlay
- **Close (`✕`) hides to system tray**
- **Show in taskbar**
- **Start minimized in tray**
- **Launch at Windows startup**

### Group C: Actions & Diagnostics (Footer)
- Two compact side-by-side secondary buttons (`Height: 28px`):
  - **`Reset Window`** (Restores default size & bottom-right corner position)
  - **`Copy Diagnostics`** (Copies OS, GPU/Monitor bounds, DPI, and `OpenDimmer.json` to clipboard with a `"Copied!"` confirmation state — replacing the separate Debug tab)
- Subtle footer line: `OpenDimmer v1.0.0 • Open Source (MIT) • GitHub`
