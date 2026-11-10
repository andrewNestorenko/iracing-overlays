# Free Overlay — Telemetry HUD Overlays for iRacing

**Free Overlay** is a lightweight, zero-dependency, native 64-bit Windows telemetry overlay suite for **iRacing** (inspired by SimLab / RaceLab). It reads live 60Hz telemetry and session info directly from iRacing's shared memory (`Local\IRSDKMemMapFileName`) and renders crisp, Per-Monitor V2 High-DPI (4K / 5K) always-on-top, click-through overlays over borderless-windowed iRacing.

It also includes a built-in **20-car GT3 + 60Hz Delta Simulator** so you can position, resize, and customize all overlays even when iRacing is not running.

---

## Included Windows & Overlays

All overlays and the configuration hub are packaged inside a single standalone executable (`FreeOverlay.exe`):

### 1. Free Overlay Hub (Control Center & Settings)
- Opens on launch and can be toggled anytime via **`F5`** or the Windows system tray icon.
- **Toggle Overlays On / Off**: Enable or disable the **Standings Overlay**, **Delta Bar Overlay**, and **Garage61 Telemetry & Braking Coach Overlay** independently.
- **Live Per-Widget Customization**:
  - **Standings Settings**: Timing column (`GAP TO LEADER` vs `INTERVAL AHEAD`), iRating display (`FULL 4,125` vs `COMPACT 4.1k`), show/hide projected `±iR` delta, Font Scale (`65%`–`175%`), Window Opacity (`45%`–`100%`), and `Reset Position & Size`.
  - **Delta Bar Settings**: Reference Lap Mode (`Session Best`, `Optimal`, `Personal Best`, `Last Lap`), Bar Scale Range (`±1.0s`, `±2.0s`, `±5.0s`), show/hide Sector Splits (`S1..Sn`), Font Scale, Window Opacity, and `Reset Position & Size`.
  - **Garage61 Coach Settings**: Layout Style (`OPT A: TRACE`, `OPT B: BAR`, `OPT C: AUDIT`), Lookahead Window (`+80m`, `+120m`, `+180m`), Reference Lap `.csv` loader (`LOAD CSV...` or built-in `SPA` GT3 reference lap), Font Scale, Window Opacity, and `Reset Position & Size`.
- **Automatic Persistence**: All settings, window coordinates, dimensions, and font scales are saved automatically to `%LOCALAPPDATA%\FreeOverlay\settings.cfg`.

### 2. Standings Overlay
- **Position (`P`)** & **Car Number (`#`)** with class color bar and podium badges.
- **Country / Club Flag (`FLG`)**: Maps iRacing `ClubName` / `FlairName` to ISO country codes and draws crisp vector flags natively on Windows 10/11.
- **Driver Name (`DRIVER`)**: Highlights the player row in cyan with a `YOU` badge and displays `PIT` badges for cars on pit road.
- **Safety Rating (`SR`)**: Official iRacing License class colors (`R`, `D`, `C`, `B`, `A`, `P`) + Safety Rating (`A 4.18`).
- **iRating (`iRATING`)**: Current iRating + live projected iRating gain/loss (`±iR`) calculated against the session's Strength of Field (`SOF`).
- **Gap / Interval (`GAP` / `INT`)**: Toggleable between gap to leader and interval to the car ahead.
- **Last Lap Time (`LAST LAP`)**: Color-coded **Purple** for Session Best and **Green** for Personal Best.
- **Player-Centered Auto-Scroll**: When the field has more cars than fit inside your resized widget height, the table automatically centers around your car and the drivers immediately ahead and behind you.

### 3. Delta Bar Overlay (Pro Bar + Official Sector Splits)
- **Reference Mode & Reference Lap Time (Left)**: Switchable between `SESSION BEST`, `OPTIMAL LAP`, `PERSONAL BEST`, and `LAST LAP` (either in the Hub or by clicking the reference label directly when unlocked).
- **Big Center Delta Readout**: Displays live time delta (`-0.28s`) with border and text colored by instantaneous rate-of-change (`LapDeltaTo..._DD`) so you immediately see whether you are currently gaining (green) or losing (red) speed in the current corner.
- **Predicted Lap Time & Off-Track Indicator (Right)**: Real-time projected lap time (`Ref Lap + Live Delta`) plus an automatic `OFF-TRACK (1x)` warning when `PlayerTrackSurface == 0`.
- **Bi-Directional Center-Zero Progress Bar**: Anchored at `0.00s` in the center; fills left in green/purple when ahead of reference pace and right in red when behind.
- **Official iRacing Sector Splits (`S1 .. Sn`)**: Parses the track's official sector list (`SplitTimeInfo -> Sectors -> SectorStartPct`) from the iRacing SDK YAML and computes live & locked sector splits across any number of track sectors.

### 4. Garage61 Reference Telemetry & Braking Coach Overlay
- **Distance-Indexed (`LapDistPct`) Telemetry Synchronization**: Aligns your live car position, brake pressure, throttle, speed, and gear against a Garage61 `.csv` telemetry export (or the built-in Spa-Francorchamps GT3 reference lap) by exact track distance rather than elapsed time.
- **3 Switchable Layout Designs** (selectable in the Hub or by clicking the `STYLE A / B / C` pill on the overlay):
  - **Option A — Lookahead Trace + Pedal Bars**: Spatial lookahead graph (`-60m` behind to `+80m`/`+120m`/`+180m` ahead of your bumper) with the Garage61 ghost brake/throttle curves scrolling toward your `NOW` crosshair, `G61 BRK` marker flag, braking spot countdown/delta banner, and dual vertical live vs. ghost brake & throttle bars with peak target line.
  - **Option B — Compact Brake Coach Bar**: Ultra-minimalist horizontal sightline bar showing the countdown to the next braking zone (`BRK IN 34 m` → `BRAKE NOW!`), live vs. ghost brake pressure comparison bar with target peak marker, and exact braking spot delta in meters (`4.2m EARLY` / `1.5m LATE` / `ON TARGET`).
  - **Option C — Pro Box + Corner Audit**: Combines the spatial lookahead trace, live pedal matcher, speed & gear delta box, and a **3-Corner Braking Audit Strip** that automatically scores your last 3 corners on braking point accuracy (`±m`), peak brake pressure (`% vs %`), and minimum apex speed (`kph`).

---

## SimLab-Style Hover Resize & Font Scaling

When overlays are **unlocked**:
1. Hover your mouse over either overlay to reveal a **single top-right icon button** and **4 corner L-brackets**.
2. Click the top-right icon button to toggle between two corner-drag modes:
   - **`⤢` (Diagonal Arrows — Blue)**: Dragging any corner **resizes the widget window** (fitting more or fewer driver rows).
   - **`Aa` (Font Icon — Purple)**: Dragging any corner **scales the font size** (`65%` to `175%`) and proportionally scales the widget.
3. Press **`F6`** (or click `LOCK ALL` in the Hub) before driving to lock all overlays into **Click-Through Mode** (`WS_EX_TRANSPARENT`) so mouse clicks pass straight through to iRacing.

---

## Global Hotkeys

| Hotkey | Action |
| :--- | :--- |
| **`F5`** | Show / Hide **Free Overlay Hub** (Control Center & Settings) |
| **`F6`** | **Lock / Unlock** all overlays (enables/disables click-through mode) |
| **`F7`** | Cycle Telemetry Source (`AUTO` → `SIMULATOR` → `LIVE iRACING`) |
| **`F8`** | Save settings & Quit Free Overlay |

---

## Quick Start (Windows 10 / 11)

1. Download **`FreeOverlay.exe`** from the [**GitHub Releases**](../../releases) page (or from the latest GitHub Actions workflow artifact).
2. In iRacing **Graphics Settings**, run in **Borderless Windowed** mode (*Fullscreen* unchecked, *Border* unchecked) so transparent topmost Windows overlays can render over the simulator.
3. Double-click **`FreeOverlay.exe`**.

---

## Building from Source

### GitHub Actions Release Pipeline
The repository includes [`.github/workflows/release.yml`](.github/workflows/release.yml), which automatically compiles `win-native/FreeOverlayApp.cs` on `windows-latest` using the 64-bit .NET Framework compiler (`csc.exe`) with the `PerMonitorV2` High-DPI manifest (`win-native/app.manifest`):
- Every push to `main` builds `FreeOverlay.exe`, uploads it as a workflow artifact (`FreeOverlay-Windows-x64`), and updates the rolling `latest` GitHub Release.
- Pushing any version tag (`v1.0.0`, `v1.1.0`, etc.) or triggering the workflow manually via **Actions → Build & Release FreeOverlay.exe → Run workflow** publishes a tagged GitHub Release with `FreeOverlay.exe` attached.

### Local Build on Windows (PowerShell)
```powershell
New-Item -ItemType Directory -Force -Path release | Out-Null
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe /optimize+ /platform:x64 `
  /win32manifest:win-native\app.manifest `
  /out:release\FreeOverlay.exe `
  win-native\FreeOverlayApp.cs
```

### Cross-Build from macOS / Linux
```bash
python3 win-native/build_exe.py
```
Generates `release/FreeOverlay.exe` using `clang -target x86_64-pc-windows-msvc` and the embedded PE32+ linker.

---

## Project Structure

```text
├── .github/workflows/
│   └── release.yml             # CI/CD pipeline producing FreeOverlay.exe artifact & GitHub Release
├── win-native/
│   ├── FreeOverlayApp.cs       # Complete C# source (Hub, Standings, Delta Bar, Live iRacing SDK & Simulator)
│   ├── app.manifest            # Win32 Per-Monitor V2 High-DPI manifest for 4K/5K displays
│   └── build_exe.py            # Cross-platform PE32+ executable builder for macOS/Linux
├── release/
│   └── FreeOverlay.exe         # Standalone 64-bit Windows executable
└── preview.html                # Browser-based interactive preview prototype
```
