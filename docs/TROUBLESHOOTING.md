# Troubleshooting

## Overlay Not Visible

1. Check that FocalFade is enabled (right-click tray icon → Enabled)
2. Verify opacity isn't too low (try 50% or higher)
3. Check if the active app is in the exclusion list
4. Some apps (games, video players) may force themselves above overlays
5. Try switching between "Active Window" and "All Monitors" modes

## Overlay Covers Active Window

1. This can happen with certain window styles (e.g., borderless maximized windows)
2. Try increasing the Focus Margin in Settings → Appearance
3. Report the specific app and window style for investigation

## Multi-Monitor Offset

1. Ensure Windows display scaling is set correctly for each monitor
2. Check that FocalFade's app manifest enables PerMonitorV2 DPI awareness
3. If using mixed DPI, try setting all monitors to the same scale temporarily to diagnose

## Hotkey Conflict

1. If a hotkey fails to register, FocalFade logs a warning
2. Another application may already use that hotkey combination
3. Check Settings → Diagnostics for hotkey registration status
4. Common conflicts: other tray utilities, screen capture tools, gaming overlays

## Tray Icon Missing

1. Windows may hide the tray icon - click the up-arrow in the system tray to expand
2. Drag the FocalFade icon to the visible tray area
3. Restart FocalFade if the icon doesn't appear

## Fullscreen Apps Not Pausing

1. Ensure "Pause on fullscreen apps" is checked in Settings → General
2. Some borderless-windowed games may not be detected as fullscreen
3. FocalFade uses monitor-bounds matching with 8px tolerance for fullscreen detection

## Games and DirectX/Vulkan Apps

1. DirectX/Vulkan fullscreen exclusive mode may not allow overlays
2. Use borderless windowed mode in game settings for best compatibility
3. Add the game to the exclusion list if overlay causes issues

## Admin/Elevated Windows

1. FocalFade runs as a normal user (asInvoker)
2. Windows running as administrator may appear above the overlay
3. Running FocalFade as administrator is NOT recommended for security
4. Instead, add problematic elevated apps to the exclusion list

## Reset All Settings

1. Open Settings → Diagnostics → Reset to Defaults
2. Or manually delete `%APPDATA%\FocalFade\settings.json`
3. Restart FocalFade

## Logs

1. Open Settings → Diagnostics → Open Log Folder
2. Logs are in `%LOCALAPPDATA%\FocalFade\Logs\`
3. Enable verbose logging for more detail (Settings → Diagnostics)
4. Logs do NOT include window titles by default
