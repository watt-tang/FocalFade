using FocalFade.Models;
using FocalFade.Native;
using System.Windows;

namespace FocalFade.Core;

public static class FullscreenDetector
{
    private const int FullscreenTolerancePx = 8;

    public static bool IsFullscreen(WindowInfo window, MonitorInfo monitor)
    {
        if (!window.IsVisible || window.IsMinimized)
            return false;

        var monitorBounds = monitor.PhysicalBounds;
        var windowBounds = window.PhysicalBounds;

        // Check if window approximately covers monitor
        return Math.Abs(windowBounds.Left - monitorBounds.Left) <= FullscreenTolerancePx
            && Math.Abs(windowBounds.Top - monitorBounds.Top) <= FullscreenTolerancePx
            && Math.Abs(windowBounds.Right - monitorBounds.Right) <= FullscreenTolerancePx
            && Math.Abs(windowBounds.Bottom - monitorBounds.Bottom) <= FullscreenTolerancePx;
    }

    public static bool IsFullscreenOnAnyMonitor(WindowInfo window, IReadOnlyList<MonitorInfo> monitors)
    {
        return monitors.Any(m => IsFullscreen(window, m));
    }
}
