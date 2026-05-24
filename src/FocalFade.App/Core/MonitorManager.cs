using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;

namespace FocalFade.Core;

public sealed class MonitorManager : IMonitorManager
{
    private readonly ILogger<MonitorManager> _logger;
    private List<MonitorInfo> _monitors = [];
    private readonly object _lock = new();

    public MonitorManager(ILogger<MonitorManager> logger)
    {
        _logger = logger;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Refresh();
    }

    public IReadOnlyList<MonitorInfo> Monitors
    {
        get { lock (_lock) return _monitors.AsReadOnly(); }
    }

    public void Refresh()
    {
        try
        {
            var monitors = new List<MonitorInfo>();

            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (!User32.GetMonitorInfoW(hMonitor, ref mi))
                    return true;

                double dpiScale = DpiCoordinator.GetDpiScaleForMonitor(hMonitor);

                var physicalBounds = new Rect(
                    mi.rcMonitor.Left, mi.rcMonitor.Top,
                    mi.rcMonitor.Width, mi.rcMonitor.Height);

                var workAreaPhysical = new Rect(
                    mi.rcWork.Left, mi.rcWork.Top,
                    mi.rcWork.Width, mi.rcWork.Height);

                monitors.Add(new MonitorInfo
                {
                    HMonitor = hMonitor,
                    PhysicalBounds = physicalBounds,
                    DipBounds = DpiCoordinator.PhysicalToDip(physicalBounds, dpiScale, dpiScale),
                    WorkAreaPhysical = workAreaPhysical,
                    WorkAreaDip = DpiCoordinator.PhysicalToDip(workAreaPhysical, dpiScale, dpiScale),
                    IsPrimary = (mi.dwFlags & NativeConstants.MONITORINFOF_PRIMARY) != 0,
                    DpiScaleX = dpiScale,
                    DpiScaleY = dpiScale,
                    DeviceName = mi.szDevice
                });

                return true;
            }, IntPtr.Zero);

            lock (_lock) _monitors = monitors;

            _logger.LogInformation("Refreshed monitors: {Count} found", monitors.Count);
            foreach (var m in monitors)
            {
                _logger.LogDebug("Monitor {Device}: Physical=({X},{Y},{W},{H}) DIP=({DX},{DY},{DW},{DH}) DPI={Dpi} Primary={Primary}",
                    m.DeviceName,
                    m.PhysicalBounds.X, m.PhysicalBounds.Y, m.PhysicalBounds.Width, m.PhysicalBounds.Height,
                    m.DipBounds.X, m.DipBounds.Y, m.DipBounds.Width, m.DipBounds.Height,
                    m.DpiScaleX, m.IsPrimary);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh monitors");
        }
    }

    public MonitorInfo? GetMonitorContainingPhysical(Rect physicalBounds)
    {
        lock (_lock)
        {
            MonitorInfo? best = null;
            double bestArea = 0;

            foreach (var m in _monitors)
            {
                var intersection = physicalBounds;
                intersection.Intersect(m.PhysicalBounds);
                double area = intersection.Width * intersection.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = m;
                }
            }

            return best;
        }
    }

    public MonitorInfo? GetMonitorContaining(Rect bounds)
    {
        // For backward compat, assume DIP bounds
        lock (_lock)
        {
            MonitorInfo? best = null;
            double bestArea = 0;

            foreach (var m in _monitors)
            {
                var intersection = bounds;
                intersection.Intersect(m.DipBounds);
                double area = intersection.Width * intersection.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = m;
                }
            }

            return best;
        }
    }

    public MonitorInfo? GetMonitorFromPhysicalPoint(int x, int y)
    {
        var pt = new POINT { X = x, Y = y };
        IntPtr hMonitor = User32.MonitorFromPoint(pt, NativeConstants.MONITOR_DEFAULTTONEAREST);
        lock (_lock)
        {
            return _monitors.FirstOrDefault(m => m.HMonitor == hMonitor);
        }
    }

    public MonitorInfo? GetMonitorFromPoint(Point point)
    {
        // Convert DIP point to physical for lookup
        lock (_lock)
        {
            foreach (var m in _monitors)
            {
                if (m.DipBounds.Contains(point))
                    return m;
            }
            return _monitors.FirstOrDefault(m => m.IsPrimary);
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("Display settings changed, refreshing monitors");
        Refresh();
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
