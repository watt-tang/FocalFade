using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocalFade.Models;

namespace FocalFade.Settings;

public partial class HotkeyBindingViewModel : ObservableObject
{
    [ObservableProperty] private string _actionKey;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _gestureString;
    [ObservableProperty] private string _defaultGesture;
    [ObservableProperty] private bool _isCapturing;
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private bool _hasConflict;
    [ObservableProperty] private bool _isInvalid;

    public HotkeyBindingViewModel(string actionKey, string displayName, string gestureString, string defaultGesture)
    {
        _actionKey = actionKey;
        _displayName = displayName;
        _gestureString = gestureString;
        _defaultGesture = defaultGesture;
    }

    public HotkeyGesture ParsedGesture => HotkeyGesture.FromString(GestureString);

    public bool IsModified => GestureString != DefaultGesture;

    public void StartCapture()
    {
        IsCapturing = true;
        ValidationMessage = "Press a hotkey combination...";
        HasConflict = false;
        IsInvalid = false;
    }

    public bool TryApplyCapture(System.Windows.Input.KeyEventArgs e, IEnumerable<HotkeyBindingViewModel> allBindings)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

        // Ignore modifier-only keys
        if (key is System.Windows.Input.Key.LeftCtrl or System.Windows.Input.Key.RightCtrl
            or System.Windows.Input.Key.LeftAlt or System.Windows.Input.Key.RightAlt
            or System.Windows.Input.Key.LeftShift or System.Windows.Input.Key.RightShift
            or System.Windows.Input.Key.LWin or System.Windows.Input.Key.RWin
            or System.Windows.Input.Key.System)
        {
            ValidationMessage = "Press a key along with a modifier...";
            return false;
        }

        // Build modifiers
        var modifiers = HotkeyModifiers.None;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
            modifiers |= HotkeyModifiers.Control;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) ||
            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt))
            modifiers |= HotkeyModifiers.Alt;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ||
            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
            modifiers |= HotkeyModifiers.Shift;
        if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LWin) ||
            System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RWin))
            modifiers |= HotkeyModifiers.Win;

        // Require at least one modifier
        if (modifiers == HotkeyModifiers.None)
        {
            ValidationMessage = "A modifier key (Ctrl, Alt, Shift, Win) is required.";
            IsInvalid = true;
            return false;
        }

        // Convert WPF Key to virtual key code
        int vk = KeyToVirtualKey(key);
        if (vk == 0)
        {
            ValidationMessage = "This key cannot be used as a hotkey.";
            IsInvalid = true;
            return false;
        }

        var newGesture = new HotkeyGesture { Modifiers = modifiers, Key = vk };
        var newGestureStr = newGesture.ToString();

        // Check for duplicates
        foreach (var other in allBindings)
        {
            if (other == this) continue;
            if (string.Equals(other.GestureString, newGestureStr, StringComparison.OrdinalIgnoreCase))
            {
                ValidationMessage = $"Conflicts with: {other.DisplayName}";
                HasConflict = true;
                IsInvalid = true;
                return false;
            }
        }

        // Accept
        GestureString = newGestureStr;
        IsCapturing = false;
        ValidationMessage = "";
        HasConflict = false;
        IsInvalid = false;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ParsedGesture));
        return true;
    }

    public void CancelCapture()
    {
        IsCapturing = false;
        ValidationMessage = "";
        HasConflict = false;
        IsInvalid = false;
    }

    public void ResetToDefault()
    {
        GestureString = DefaultGesture;
        IsCapturing = false;
        ValidationMessage = "";
        HasConflict = false;
        IsInvalid = false;
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ParsedGesture));
    }

    public static (bool IsValid, string Error) Validate(string gestureStr)
    {
        if (string.IsNullOrWhiteSpace(gestureStr))
            return (false, "Empty hotkey");

        var gesture = HotkeyGesture.FromString(gestureStr);
        if (gesture.Key == 0)
            return (false, "No key specified");
        if (gesture.Modifiers == HotkeyModifiers.None)
            return (false, "No modifier key (Ctrl/Alt/Shift/Win)");

        return (true, "");
    }

    private static int KeyToVirtualKey(System.Windows.Input.Key wpfKey)
    {
        // Map WPF Key enum to Win32 virtual key codes
        return wpfKey switch
        {
            System.Windows.Input.Key.A => 0x41,
            System.Windows.Input.Key.B => 0x42,
            System.Windows.Input.Key.C => 0x43,
            System.Windows.Input.Key.D => 0x44,
            System.Windows.Input.Key.E => 0x45,
            System.Windows.Input.Key.F => 0x46,
            System.Windows.Input.Key.G => 0x47,
            System.Windows.Input.Key.H => 0x48,
            System.Windows.Input.Key.I => 0x49,
            System.Windows.Input.Key.J => 0x4A,
            System.Windows.Input.Key.K => 0x4B,
            System.Windows.Input.Key.L => 0x4C,
            System.Windows.Input.Key.M => 0x4D,
            System.Windows.Input.Key.N => 0x4E,
            System.Windows.Input.Key.O => 0x4F,
            System.Windows.Input.Key.P => 0x50,
            System.Windows.Input.Key.Q => 0x51,
            System.Windows.Input.Key.R => 0x52,
            System.Windows.Input.Key.S => 0x53,
            System.Windows.Input.Key.T => 0x54,
            System.Windows.Input.Key.U => 0x55,
            System.Windows.Input.Key.V => 0x56,
            System.Windows.Input.Key.W => 0x57,
            System.Windows.Input.Key.X => 0x58,
            System.Windows.Input.Key.Y => 0x59,
            System.Windows.Input.Key.Z => 0x5A,
            System.Windows.Input.Key.D0 => 0x30,
            System.Windows.Input.Key.D1 => 0x31,
            System.Windows.Input.Key.D2 => 0x32,
            System.Windows.Input.Key.D3 => 0x33,
            System.Windows.Input.Key.D4 => 0x34,
            System.Windows.Input.Key.D5 => 0x35,
            System.Windows.Input.Key.D6 => 0x36,
            System.Windows.Input.Key.D7 => 0x37,
            System.Windows.Input.Key.D8 => 0x38,
            System.Windows.Input.Key.D9 => 0x39,
            System.Windows.Input.Key.F1 => 0x70,
            System.Windows.Input.Key.F2 => 0x71,
            System.Windows.Input.Key.F3 => 0x72,
            System.Windows.Input.Key.F4 => 0x73,
            System.Windows.Input.Key.F5 => 0x74,
            System.Windows.Input.Key.F6 => 0x75,
            System.Windows.Input.Key.F7 => 0x76,
            System.Windows.Input.Key.F8 => 0x77,
            System.Windows.Input.Key.F9 => 0x78,
            System.Windows.Input.Key.F10 => 0x79,
            System.Windows.Input.Key.F11 => 0x7A,
            System.Windows.Input.Key.F12 => 0x7B,
            System.Windows.Input.Key.Space => 0x20,
            System.Windows.Input.Key.Up => 0x26,
            System.Windows.Input.Key.Down => 0x28,
            System.Windows.Input.Key.Left => 0x25,
            System.Windows.Input.Key.Right => 0x27,
            System.Windows.Input.Key.Home => 0x24,
            System.Windows.Input.Key.End => 0x23,
            System.Windows.Input.Key.Insert => 0x2D,
            System.Windows.Input.Key.Delete => 0x2E,
            System.Windows.Input.Key.PageUp => 0x21,
            System.Windows.Input.Key.PageDown => 0x22,
            System.Windows.Input.Key.Escape => 0x1B,
            System.Windows.Input.Key.Return => 0x0D,
            System.Windows.Input.Key.Back => 0x08,
            System.Windows.Input.Key.Tab => 0x09,
            _ => 0
        };
    }
}
