using System.Windows.Media;

namespace FocalFade.Core;

public static class ColorService
{
    public static bool TryParseHex(string hex, out Color color)
    {
        color = Colors.Black;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        hex = hex.TrimStart('#');

        // Support #RGB, #RRGGBB, #AARRGGBB
        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }
        else if (hex.Length == 4)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}";
        }

        if (hex.Length == 6)
            hex = "FF" + hex;

        if (hex.Length != 8) return false;

        if (!byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out byte a) ||
            !byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out byte r) ||
            !byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out byte g) ||
            !byte.TryParse(hex[6..8], System.Globalization.NumberStyles.HexNumber, null, out byte b))
            return false;

        color = Color.FromArgb(a, r, g, b);
        return true;
    }

    public static string ToHex(Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static string ToHexRgb(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public static readonly (string Name, string Hex)[] Presets =
    [
        ("Black", "#000000"),
        ("Warm Gray", "#3D3635"),
        ("Cool Gray", "#2D3436"),
        ("Navy", "#1A2744"),
        ("Sepia", "#3E2C1C"),
        ("Dark Blue", "#0D1B2A"),
        ("Charcoal", "#2C2C2C"),
        ("Deep Purple", "#1B0A2E"),
    ];
}
