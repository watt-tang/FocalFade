# Privacy Policy

FocalFade is a local-only desktop application with a strong commitment to user privacy.

## What FocalFade Does NOT Do

- **No telemetry** - FocalFade does not collect, transmit, or report any usage data
- **No network calls** - FocalFade makes zero outbound network connections at runtime
- **No analytics** - No tracking, no crash reporting, no phone-home
- **No user content collection** - FocalFade does not read, store, or transmit window titles, file contents, or any user data
- **No cloud storage** - Everything stays on your local machine

## What FocalFade Stores Locally

### Settings File
- **Location**: `%APPDATA%\FocalFade\settings.json`
- **Contents**: Your preferences (opacity, colors, hotkeys, app rules, window placement)
- **Format**: Human-readable JSON
- **Can be deleted**: Yes, delete the file to reset to defaults

### Log Files (optional)
- **Location**: `%LOCALAPPDATA%\FocalFade\Logs\`
- **Contents**: Application events (start, stop, errors, settings changes)
- **Default**: Window titles are NOT included in logs
- **Opt-in**: Verbose logging can be enabled in settings, which may include window class names for debugging

### Registry
- **Location**: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- **Purpose**: Optional "Start with Windows" feature
- **Value**: Path to the FocalFade executable
- **Can be removed**: Toggle off in settings, or delete the registry value manually

## Data Flow

```
User Action → FocalFade → Local Settings File
                ↓
        Win32 APIs (window info)
                ↓
        Overlay Rendering (visual only)
```

No data leaves your machine.

## Third-Party Dependencies

FocalFade uses these open-source libraries (all local, no network):
- **CommunityToolkit.Mvvm** - MVVM framework for settings UI
- **Hardcodet.NotifyIcon.Wpf** - System tray icon
- **Microsoft.Extensions.Hosting** - Dependency injection and logging framework
- **System.Text.Json** - Settings serialization

None of these make network calls in FocalFade's usage.

## Contact

If you have privacy concerns, please open an issue on GitHub.
