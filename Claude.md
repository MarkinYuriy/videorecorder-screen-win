# ScreenRecorder — Project Context for Claude Code

## Tech Stack
- **Language:** C#
- **UI Framework:** WPF (XAML)
- **IDE:** Visual Studio Community 2022
- **Workload:** .NET Desktop Development
- **Target OS:** Windows
- **Installer:** Inno Setup (wizard + uninstaller)

## Target Platforms
- **x64 (AMD64)** — primary target
- **ARM64** — Surface, Snapdragon Copilot+ PCs (same codebase, two build targets)
- ~~x86 32-bit~~ — not supported

Build both targets via .NET publish profiles. Inno Setup bundles both into one installer with architecture detection.

---

## NuGet Dependencies
- `NAudio` — WASAPI loopback (system audio) + microphone capture
- `FFMpegCore` — MP4 encoding wrapper around ffmpeg.exe
- `ffmpeg.exe` — bundled inside installer (LGPL license, no modification)

## Architecture — App Type
**System Tray application** — no main window, lives in tray.

Tray menu:
- Новая запись (New Recording)
- Открыть папку записей (Open Recordings Folder)
- Настройки (Settings)
- Выход (Exit)

---

## Feature: New Recording (Wizard)

### Step 1 — Region Selection
- Fullscreen transparent WPF overlay window
- Shows previously selected region by default (saved in settings)
- User drags to reselect or confirms existing selection
- Confirm: mouse release or Enter
- Cancel: Escape

### Step 2 — Recording Settings
- FPS selector (default from settings)
- Microphone: On / Off
- System audio (speakers): On / Off

### Step 3 — Ready
- Summary of selected settings
- [Start Recording] button

---

## Feature: During Recording

### Visual indicators on screen
- Semi-transparent border around selected region
- 🔴 Blinking red dot in one corner of the region
- Timer (elapsed time) displayed above the region
- [Stop] button next to the blinking dot

### Tray icon
- Changes to "recording" state icon while recording is active

### Stop recording
- Click [Stop] button on screen overlay
- OR global hotkey (default: `Ctrl+Shift+R`, configurable in Settings)

---

## Feature: After Recording (Save Logic)

```
Auto-format enabled?
    YES → generate filename: "Recording_2026-04-12_14-30.mp4"
    NO  → user types filename manually

File with this name already exists?
    NO  → save
    YES → dialog:
            "Recording_2026-04-12_14-30.mp4 already exists"
            [Overwrite] [Save as (1)] [Cancel]

"Save as (1)" selected?
    → check if (1) exists → if yes → (2) → etc until free
```

### Notifications
- Auto-save mode → toast notification in tray: success + filename
- Manual mode → Save dialog (choose folder + filename)

---

## Feature: Settings

| Setting | Default | Notes |
|---|---|---|
| Auto-format filename | ON | Format: `Recording_YYYY-MM-DD_HH-mm.mp4` |
| Recordings folder | `Documents/Recordings/` | User can change |
| Global hotkey | `Ctrl+Shift+R` | Configurable, conflict detection |
| Default FPS | 10 | Used in wizard |
| Video quality | Medium (4000 kbps) | Preset or manual |
| Launch with Windows | OFF | Toggleable |
| Language | Russian | Localizable via .resx |

### Video Quality
- Presets: Low (1000 kbps) / Medium (4000 kbps) / High (8000 kbps)
- Selecting a preset fills the manual input field
- Manual input: numeric only, min 500 kbps, max 50000 kbps
- Validation: red highlight + tooltip if out of range

### Hotkey
- Default: `Ctrl+Shift+R`
- User can rebind in Settings
- Conflict detection: warn if combination already used by system/other apps

### Language / Localization
- Default: Russian
- Architecture: WPF `.resx` resource files per language
- Adding new language = new `.resx` file only, no code changes

### Launch with Windows
- Toggle in Settings
- Also offered as checkbox during Inno Setup installation

---

## Project Structure
```
ScreenRecorder/
├── CLAUDE.md
├── ScreenRecorder.sln
└── ScreenRecorder/
    ├── App.xaml
    ├── App.xaml.cs
    ├── Resources/
    │   ├── Strings.ru.resx          ← Russian (default)
    │   └── Strings.en.resx          ← English
    ├── Models/
    │   └── RecordingSettings.cs     ← FPS, bitrate, folder, hotkey, etc.
    ├── Services/
    │   ├── ScreenCaptureService.cs  ← grabs frames at N fps from selected region
    │   ├── AudioCaptureService.cs   ← NAudio WASAPI loopback + mic
    │   ├── EncoderService.cs        ← FFMpegCore → MP4
    │   ├── HotkeyService.cs         ← global hotkey registration
    │   └── TrayService.cs           ← tray icon, menu, toast notifications
    ├── Views/
    │   ├── OverlayWindow.xaml       ← fullscreen transparent region selector
    │   ├── RecordingOverlay.xaml    ← border + dot + timer + stop button during recording
    │   ├── WizardWindow.xaml        ← 3-step wizard
    │   └── SettingsWindow.xaml      ← settings panel
    └── Installer/
        └── setup.iss                ← Inno Setup script
```

---

## Critical Technical Requirement — UI Excluded from Capture

All overlay UI elements (region border, blinking dot, timer, stop button) must be
invisible to the screen capture. Two-layer approach:

1. **WinAPI flag** on all overlay windows:
   ```csharp
   SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
   ```
   Window is visible to user but excluded from any screen capture.

2. **Capture method** — use `BitBlt` / `Graphics.CopyFromScreen` by region coordinates
   directly from desktop DC, never capture the full screen with overlays on top.

---

## Implementation Order
1. Tray icon + menu skeleton
2. Settings model + persistence (JSON)
3. Region selection overlay (OverlayWindow)
4. Wizard (3 steps)
5. Screen capture loop (ScreenCaptureService)
6. Audio capture (AudioCaptureService)
7. Encoding to MP4 (EncoderService)
8. Recording overlay (border + dot + timer + stop)
9. Save logic (auto-format, conflict resolution, toast)
10. Hotkey service (global, configurable)
11. Settings window UI
12. Localization (.resx)
13. Inno Setup installer script
14. Polish + error handling