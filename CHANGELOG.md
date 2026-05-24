# Changelog

All notable changes to FocalFade will be documented in this file.

## [1.0.0-beta.1] - 2026-05-24

### Added
- Initial release
- Active window focus dimming with EvenOdd geometry
- Active app mode - dims all windows except the foreground app
- Multi-monitor support with per-monitor overlays
- Mixed DPI support (PerMonitorV2)
- Customizable opacity (10%-90%), dim color, corner radius, focus margin
- Presentation mode with optional border/halo
- System tray integration with context menu
- Global hotkeys (Ctrl+Alt+F, Ctrl+Alt+Up/Down, Ctrl+Alt+P, Ctrl+Alt+Space)
- Settings window with live preview
- App exclusion rules with default presets
- Start with Windows (HKCU registry)
- Pause on fullscreen apps
- Single instance enforcement
- JSON settings with atomic writes and backup
- Unit tests for geometry, settings, rules, fullscreen detection
- GitHub Actions CI/CD pipeline
- Portable self-contained publish script
