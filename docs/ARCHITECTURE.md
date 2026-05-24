# Architecture

## Overview

FocalFade is a WPF tray utility that dims desktop areas around the active window. It uses a per-monitor overlay approach with Win32 P/Invoke for window tracking and DPI awareness.

## Core Components

### Native Layer (`Native/`)
Win32 P/Invoke wrappers for:
- **User32**: Window management, hotkeys, WinEvent hooks, monitor enumeration
- **DwmApi**: DWM window attributes (extended frame bounds, cloaked state)
- **ShCore**: DPI awareness and per-monitor DPI queries
- **Shell32**: Tray icon (via Hardcodet.NotifyIcon.Wpf library)

Key design: All P/Invoke delegates (WinEventDelegate, EnumWindowsProc) are kept alive as instance fields to prevent GC collection during callbacks.

### Active Window Tracking (`Core/ActiveWindowTracker`)
- Primary: `SetWinEventHook` for `EVENT_SYSTEM_FOREGROUND`
- Fallback: 500ms `DispatcherTimer` to recover from missed events
- Debounce: 40ms debounce to avoid rapid-fire updates
- Filtering: Ignores shell windows (Progman, WorkerW, Shell_TrayWnd), cloaked windows, minimized windows, own-process windows

### Monitor & DPI System (`Core/MonitorManager`, `Core/DpiCoordinator`)
- Enumerates monitors via `MonitorFromWindow` + `GetMonitorInfoW`
- Stores bounds in both physical pixels and WPF DIPs
- Supports negative coordinates (monitors to the left of primary)
- Listens to `SystemEvents.DisplaySettingsChanged` for topology changes
- DPI conversion: physical / dpiScale = DIP

### Overlay Rendering (`Overlay/`)
- One `FocusOverlayWindow` per monitor
- Each window is borderless, topmost, transparent, click-through, not in Alt+Tab
- Uses WPF `GeometryGroup` with `FillRule.EvenOdd` for hole-punching
- The dimming area is the first child (full monitor rect)
- Focus holes are subsequent children (expanded by margin, rounded corners)
- Animations via WPF `DoubleAnimation` with `QuadraticEase`

### Settings & Persistence (`Core/SettingsStore`)
- JSON file at `%APPDATA%\FocalFade\settings.json`
- Atomic writes (write temp → replace original)
- Corrupt settings backed up as `settings.corrupt.TIMESTAMP.json`
- Schema version field for future migrations
- Debounced auto-save (500ms)

### App Lifecycle (`Core/AppBootstrapper`)
1. Single-instance check via named Mutex
2. Load settings
3. Initialize tray icon
4. Create overlay windows
5. Start active window tracker
6. Register global hotkeys
7. Apply initial enabled state

## Why Not Electron?
FocalFade is a desktop utility that needs to:
- Overlay transparent windows at exact pixel positions
- Respond to Win32 events in real-time
- Use minimal CPU and memory
- Run without installation

Electron would add ~100MB of runtime overhead and make precise overlay positioning much harder.

## Limitations
- **UAC Secure Desktop**: Windows prevents any app from drawing over the secure desktop. FocalFade naturally pauses.
- **Fullscreen Apps**: Some apps force their own topmost state, which can conflict with overlays.
- **Admin Windows**: Non-elevated FocalFade cannot overlay elevated windows by default.
