# Manual QA Checklist

## Pre-Release Acceptance Testing

Test each item and check it off when passing.

### Basic Functionality
- [ ] Launch FocalFade.exe
- [ ] Tray icon appears in system tray
- [ ] Double-click tray icon opens Settings window
- [ ] Right-click tray icon shows context menu

### Enable/Disable
- [ ] Toggle Enabled from tray menu - overlay appears/disappears
- [ ] Ctrl+Alt+F toggles enabled state
- [ ] Tray tooltip shows "FocalFade: On" / "FocalFade: Off"

### Focus Tracking
- [ ] Open Notepad, VS Code, browser
- [ ] Switch between windows - clear region follows active window
- [ ] Background windows are dimmed
- [ ] Switching windows updates the clear region smoothly

### Multi-Monitor
- [ ] Move active window across monitors - overlay follows
- [ ] Each monitor has its own overlay
- [ ] Test with negative monitor coordinates if available

### Opacity
- [ ] Change opacity from tray menu (20%, 35%, 45%, 55%, 70%)
- [ ] Opacity changes apply immediately
- [ ] Ctrl+Alt+Up/Down adjusts opacity

### Settings Window
- [ ] Opens from tray and doesn't break overlay behavior
- [ ] Adjust opacity slider - overlay updates live
- [ ] Adjust focus margin slider
- [ ] Adjust corner radius slider
- [ ] Toggle animations on/off
- [ ] Settings are saved and restored after restart

### Focus Modes
- [ ] Active Window mode - only foreground window clear
- [ ] Active App mode - all windows of foreground app clear
- [ ] Current Monitor Only - dims only monitor with active window
- [ ] All Monitors - dims all monitors

### Fullscreen
- [ ] Play fullscreen video - overlay pauses by default
- [ ] Open PowerPoint fullscreen - overlay pauses
- [ ] Toggle "Pause on fullscreen apps" behavior

### App Rules
- [ ] Add current app to exclusions
- [ ] Excluded app's window is not dimmed
- [ ] Remove app from exclusions
- [ ] Default exclusions work (PowerPoint, OBS, VLC, etc.)

### Presentation Mode
- [ ] Ctrl+Alt+P toggles Presentation Mode
- [ ] Presentation mode shows optional border/halo
- [ ] Tray menu reflects Presentation Mode state

### Startup
- [ ] Enable "Start with Windows" - registry value created
- [ ] Verify in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- [ ] Disable "Start with Windows" - registry value removed

### Hotkeys
- [ ] Ctrl+Alt+F - Toggle enabled
- [ ] Ctrl+Alt+Up - Increase opacity
- [ ] Ctrl+Alt+Down - Decrease opacity
- [ ] Ctrl+Alt+P - Presentation mode
- [ ] Ctrl+Alt+Space - Peek (10 seconds)

### Exit
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
