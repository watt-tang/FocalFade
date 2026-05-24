using System.Windows;

namespace FocalFade.Models;

public sealed record MonitorInfo
{
    public IntPtr HMonitor { get; init; }
    public Rect PhysicalBounds { get; init; }
    public Rect DipBounds { get; init; }
    public Rect WorkAreaPhysical { get; init; }
    public Rect WorkAreaDip { get; init; }
    public bool IsPrimary { get; init; }
    public double DpiScaleX { get; init; } = 1.0;
    public double DpiScaleY { get; init; } = 1.0;
    public string DeviceName { get; init; } = string.Empty;
}
