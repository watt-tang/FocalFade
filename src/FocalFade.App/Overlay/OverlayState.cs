using FocalFade.Models;
using System.Windows;

namespace FocalFade.Overlay;

public sealed class OverlayState
{
    public List<Rect> FocusRects { get; set; } = [];
    public OverlayAppearance Appearance { get; set; } = new();
    public bool IsVisible { get; set; }
    public bool IsPaused { get; set; }
    public DateTime LastUpdate { get; set; }
}
