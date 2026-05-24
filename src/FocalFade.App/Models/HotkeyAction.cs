namespace FocalFade.Models;

public sealed record HotkeyAction(string Key, string DisplayName, string DefaultGesture)
{
    public static readonly HotkeyAction[] AllActions =
    [
        new("ToggleEnabled", "Toggle enabled/disabled", "Ctrl+Alt+F"),
        new("IncreaseOpacity", "Increase opacity", "Ctrl+Alt+Up"),
        new("DecreaseOpacity", "Decrease opacity", "Ctrl+Alt+Down"),
        new("PresentationMode", "Toggle Presentation Mode", "Ctrl+Alt+P"),
        new("TemporaryPeek", "Peek (10 sec)", "Ctrl+Alt+Space"),
        new("OpenSettings", "Open settings", "Ctrl+Alt+S"),
        new("QuickPanel", "Quick panel", "Ctrl+Alt+Q"),
    ];
}
