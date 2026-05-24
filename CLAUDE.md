# FocalFade - Claude Code Reference

## Build Commands
```bash
dotnet build                    # Build solution
dotnet test                     # Run all tests
dotnet build -c Release         # Release build
dotnet publish src/FocalFade.App/FocalFade.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Architecture
- **FocalFade.App**: WPF tray utility targeting .NET 8 LTS (TODO: migrate to .NET 10 LTS when SDK available)
- **Native/**: P/Invoke wrappers for Win32 APIs (User32, DwmApi, ShCore, Shell32)
- **Core/**: Application services - window tracking, monitor management, settings, hotkeys
- **Overlay/**: WPF overlay windows with EvenOdd geometry for focus dimming
- **Settings/**: MVVM settings window using CommunityToolkit.Mvvm
- **Tray/**: System tray icon and menu via Hardcodet.NotifyIcon.Wpf
- **Models/**: Records for AppSettings, AppRule, WindowInfo, MonitorInfo, etc.
- **Services/**: Interfaces for DI (IActiveWindowTracker, IOverlayManager, ISettingsStore, etc.)

## Key Design Decisions
- One overlay window per monitor (not one giant virtual-desktop overlay)
- WinEventHook for foreground tracking with 500ms DispatcherTimer fallback
- Settings stored in %APPDATA%\FocalFade\settings.json with atomic writes
- Named Mutex for single-instance enforcement
- No runtime network calls, no telemetry

## Coding Standards
- Nullable reference types enabled
- MVVM with CommunityToolkit.Mvvm for settings UI
- Records for simple models
- Interfaces for testable services
- Native interop isolated in Native/ namespace
