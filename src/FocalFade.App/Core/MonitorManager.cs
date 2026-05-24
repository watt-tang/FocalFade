using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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
        get
        {
            lock (_lock) return _monitors.AsReadOnly();
        }
    }

    public void Refresh()
    {
        try
        {
            var monitors = new List<MonitorInfo>();

            User32.EnumWindows((hWnd, _) =>
            {
                var className = User32.GetClassName(hWnd);
                if (className == "Shell_TrayWnd" || className == "Shell_SecondaryTrayWnd")
                    return true;

                IntPtr hMonitor = User32.MonitorFromWindow(hWnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
                if (hMonitor == IntPtr.Zero) return true;

                var mi = new MONITORINFOEX { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFOEX>() };
                if (!User32.GetMonitorInfoW(hMonitor, ref mi)) return true;

                if (monitors.Any(m => m.HMonitor == hMonitor))
                    return true;

                double dpiScale = DpiCoordinator.GetDpiScaleForMonitor(hMonitor);

                var physicalBounds = new Rect(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Width, mi.rcMonitor.Height);
                var workAreaPhysical = new Rect(mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Width, mi.rcWork.Height);

                monitors.Add(new MonitorInfo
                {
                    HMonitor = hMonitor,
                    PhysicalBounds = physicalBounds,
                    DipBounds = DpiCoordinator.PhysicalToDip(physicalBounds, dpiScale, dpiScale),
                    WorkAreaPhysical = workAreaPhysical,
                    WorkAreaDip = DpiCoordinator.PhysicalToDip(workAreaPhysical, dpiScale, dpiScale),
                    IsPrimary = (mi.dwFlags & 1) != 0,
                    DpiScaleX = dpiScale,
                    DpiScaleY = dpiScale,
                    DeviceName = mi.szDevice
                });

                return true;
            }, IntPtr.Zero);

            lock (_lock) _monitors = monitors;

            _logger.LogInformation("Refreshed monitors: {Count} found", monitors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh monitors");
        }
    }

    public MonitorInfo? GetMonitorContaining(Rect bounds)
    {
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

    public MonitorInfo? GetMonitorFromPoint(Point point)
    {
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
