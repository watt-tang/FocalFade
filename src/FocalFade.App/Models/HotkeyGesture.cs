using System.Text.Json.Serialization;

namespace FocalFade.Models;

public sealed record HotkeyGesture
{
    [JsonPropertyName("modifiers")]
    public HotkeyModifiers Modifiers { get; init; }

    [JsonPropertyName("key")]
    public int Key { get; init; }

    public static HotkeyGesture FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new HotkeyGesture();

        var parts = value.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = HotkeyModifiers.None;
        int key = 0;

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= HotkeyModifiers.Control;
                    break;
                case "ALT":
                    modifiers |= HotkeyModifiers.Alt;
                    break;
                case "SHIFT":
                    modifiers |= HotkeyModifiers.Shift;
                    break;
                case "WIN":
                case "META":
                    modifiers |= HotkeyModifiers.Win;
                    break;
                default:
                    if (part.Length == 1)
                        key = char.ToUpperInvariant(part[0]);
                    else if (Enum.TryParse<System.Windows.Forms.Keys>(part, true, out var k))
                        key = (int)k;
                    break;
            }
        }

        return new HotkeyGesture { Modifiers = modifiers, Key = key };
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");

        if (Key >= 0x41 && Key <= 0x5A)
            parts.Add(((char)Key).ToString());
        else if (Key >= 0x30 && Key <= 0x39)
            parts.Add(((char)Key).ToString());
        else if (Key >= 0x70 && Key <= 0x87)
            parts.Add($"F{Key - 0x6F}");
        else if (Enum.IsDefined(typeof(System.Windows.Forms.Keys), Key))
            parts.Add(((System.Windows.Forms.Keys)Key).ToString());
        else if (Key > 0)
            parts.Add($"0x{Key:X2}");

        return string.Join("+", parts);
    }
}

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}
