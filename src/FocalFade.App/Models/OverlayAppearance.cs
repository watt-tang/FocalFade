using System.Windows.Media;

namespace FocalFade.Models;

public sealed record OverlayAppearance
{
    public double Opacity { get; init; } = 0.45;
    public Color DimColor { get; init; } = Colors.Black;
    public double FocusMargin { get; init; } = 8.0;
    public double CornerRadius { get; init; } = 8.0;
    public bool AnimationsEnabled { get; init; } = true;
    public int FadeDurationMs { get; init; } = 120;
    public int MoveDurationMs { get; init; } = 80;
    public bool ShowBorder { get; init; }
    public Color BorderColor { get; init; } = Color.FromArgb(80, 255, 255, 255);
    public double BorderThickness { get; init; } = 1.0;
}
