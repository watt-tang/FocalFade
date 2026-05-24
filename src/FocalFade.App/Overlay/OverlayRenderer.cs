using FocalFade.Models;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Overlay;

public sealed class OverlayRenderer : IDisposable
{
    private readonly ILogger<OverlayRenderer> _logger;
    private readonly IMonitorManager _monitorManager;
    private readonly Dictionary<IntPtr, FocusOverlayWindow> _overlayWindows = new();
    private OverlayAppearance _appearance = new();
    private bool _disposed;

    public OverlayRenderer(ILogger<OverlayRenderer> logger, IMonitorManager monitorManager)
    {
        _logger = logger;
        _monitorManager = monitorManager;
    }

    public void Initialize()
    {
        RecreateOverlays();
    }

    public void RecreateOverlays()
    {
        DestroyOverlays();

        foreach (var monitor in _monitorManager.Monitors)
        {
            try
            {
                var overlay = new FocusOverlayWindow();
                overlay.PositionOnMonitor(monitor);
                overlay.Hide();
                _overlayWindows[monitor.HMonitor] = overlay;
                _logger.LogDebug("Created overlay for monitor {Device}", monitor.DeviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create overlay for monitor {Device}", monitor.DeviceName);
            }
        }
    }

    public void Show()
    {
        int fadeMs = _appearance.AnimationsEnabled ? _appearance.FadeDurationMs : 0;
        foreach (var overlay in _overlayWindows.Values)
        {
            overlay.ShowWithAnimation(fadeMs);
        }
    }

    public void Hide()
    {
        int fadeMs = _appearance.AnimationsEnabled ? _appearance.FadeDurationMs : 0;
        foreach (var overlay in _overlayWindows.Values)
        {
            overlay.HideWithAnimation(fadeMs);
        }
    }

    public void UpdateFocusRects(List<Rect> focusRects, OverlayMode mode)
    {
        foreach (var (hMonitor, overlay) in _overlayWindows)
        {
            var monitor = _monitorManager.Monitors.FirstOrDefault(m => m.HMonitor == hMonitor);
            if (monitor == null) continue;

            List<Rect> rectsForMonitor;

            switch (mode)
            {
                case OverlayMode.CurrentMonitor:
                    // Only show on the monitor containing the focus rect
                    if (focusRects.Count > 0 && monitor.DipBounds.IntersectsWith(GetBoundsRect(focusRects)))
                    {
                        rectsForMonitor = OverlayGeometryService.GetIntersectionsWithMonitor(monitor.DipBounds, focusRects);
                    }
                    else
                    {
                        rectsForMonitor = [];
                    }
                    break;

                case OverlayMode.AllMonitors:
                default:
                    rectsForMonitor = OverlayGeometryService.GetIntersectionsWithMonitor(monitor.DipBounds, focusRects);
                    break;
            }

            overlay.UpdateOverlay(rectsForMonitor, _appearance, monitor);
        }
    }

    public void UpdateAppearance(OverlayAppearance appearance)
    {
        _appearance = appearance;
    }

    private static Rect GetBoundsRect(List<Rect> rects)
    {
        if (rects.Count == 0) return Rect.Empty;
        var result = rects[0];
        foreach (var r in rects.Skip(1))
            result.Union(r);
        return result;
    }

    private void DestroyOverlays()
    {
        foreach (var overlay in _overlayWindows.Values)
        {
            try
            {
                overlay.Close();
            }
            catch { }
        }
        _overlayWindows.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyOverlays();
    }
}
