# Manual QA Checklist

## Pre-Release Acceptance Testing

Test each item and check it off when passing.

### 1. Single Monitor - Core Behavior
- [ ] Launch FocalFade.exe
- [ ] Tray icon appears in system tray
- [ ] Enable from tray menu
- [ ] Open Notepad, VS Code, browser
- [ ] Switch between them rapidly - overlay follows real active window
- [ ] No laggy half-second delay - overlay updates quickly
- [ ] Background windows are dimmed correctly

### 2. Single Monitor - Drag Detection
- [ ] Drag a window - overlay hides immediately
- [ ] Release window - overlay reappears after short delay
- [ ] Resize a window - overlay hides during resize
- [ ] Release - overlay reappears
- [ ] Verify no jitter during drag

### 3. Single Monitor - Shell/USB False Target Prevention
- [ ] Insert a USB drive or trigger AutoPlay
- [ ] Verify FocalFade does NOT focus on a tiny desktop icon or USB shell surface
- [ ] Previous working window remains the target
- [ ] If no valid window, overlay hides safely (no tiny hole)

### 4. Multi-Monitor
- [ ] Primary monitor on left, secondary on right
- [ ] Active window on monitor 1 - hole appears on monitor 1, monitor 2 fully dimmed
- [ ] Active window on monitor 2 - hole appears on monitor 2, monitor 1 fully dimmed
- [ ] Move window between monitors - hole follows correctly
- [ ] Window spanning two monitors - holes on both monitors
- [ ] If available: different scaling (e.g., 100% + 125%)
- [ ] If available: secondary monitor at negative X coordinates

### 5. Fullscreen
- [ ] Play fullscreen browser video - overlay pauses by default
- [ ] PowerPoint slideshow - overlay pauses
- [ ] Toggle "Pause on fullscreen apps" off - overlay dims even in fullscreen

### 6. Settings
- [ ] Double-click tray icon opens Settings
- [ ] Adjust opacity slider - overlay updates live
- [ ] Adjust focus margin and corner radius
- [ ] Change dim color using hex input
- [ ] Adjust R/G/B sliders - hex and preview update
- [ ] Click a color preset - color and sliders update
- [ ] Verify live preview swatch shows the selected color
- [ ] Toggle animations on/off
- [ ] Settings saved after restart

### 7. App Rules & Per-App Opacity
- [ ] Add current app to exclusions - overlay hides for that app
- [ ] Set per-app opacity override on a rule
- [ ] Switch to that app - overlay uses per-app opacity
- [ ] Switch away - overlay uses global opacity

### 8. Hotkeys
- [ ] Ctrl+Alt+F - Toggle enabled
- [ ] Ctrl+Alt+Up - Increase opacity
- [ ] Ctrl+Alt+Down - Decrease opacity
- [ ] Ctrl+Alt+P - Presentation mode
- [ ] Ctrl+Alt+Space - Peek (10 seconds)
- [ ] Ctrl+Alt+S - Open settings

### 8b. Hotkey Customization
- [ ] Open Settings > Hotkeys
- [ ] Click on a hotkey field (e.g., "Toggle enabled")
- [ ] Press a new key combination (e.g., Ctrl+Shift+T)
- [ ] Verify the new hotkey is displayed
- [ ] Verify it works when pressed outside settings
- [ ] Try assigning a duplicate - verify conflict warning
- [ ] Try a bare key with no modifier - verify rejection
- [ ] Click "Reset All" - verify all hotkeys return to defaults
- [ ] Restart app - verify custom hotkeys persist

### 9. Tray Icon Theme
- [ ] Set tray icon theme to Auto
- [ ] Toggle Windows light/dark app theme in Settings > Personalization
- [ ] Verify tray icon updates to match theme
- [ ] Set theme to Light - icon stays light regardless
- [ ] Set theme to Dark - icon stays dark regardless

### 10. Blur (Experimental)
- [ ] Enable blur in Settings > Blur Effect
- [ ] Adjust blur intensity slider
- [ ] Verify focused window remains clear (not blurred)
- [ ] Verify dimmed areas show blur effect behind them
- [ ] Move focus between windows - blur panels update
- [ ] Disable blur - returns to normal dimming
- [ ] Verify blur works on multiple monitors (if available)
- [ ] Verify app does not crash if blur is unsupported on your system

### 11. Presentation Mode
- [ ] Ctrl+Alt+P toggles Presentation Mode
- [ ] Presentation mode shows optional border/halo
- [ ] Stronger default dim

### 12. Start with Windows
- [ ] Enable - registry value created in HKCU\...\Run
- [ ] Disable - registry value removed

### 13. Exit
- [ ] Exit from tray menu
- [ ] No overlay remains on screen
- [ ] Tray icon disappears
- [ ] No zombie processes

### Quality Checks
- [ ] No focus stealing
- [ ] Overlay doesn't block mouse clicks
- [ ] Overlay doesn't appear in Alt+Tab
- [ ] CPU usage near idle when nothing changes
- [ ] Memory usage reasonable (~30-80MB)
- [ ] No unhandled exceptions in logs
