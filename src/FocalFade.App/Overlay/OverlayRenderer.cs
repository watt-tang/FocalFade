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
    private OverlayMode _currentMode = OverlayMode.AllMonitors;
    private bool _disposed;
    private bool _isHiddenForDrag;

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
                overlay.HideImmediate();
                _overlayWindows[monitor.HMonitor] = overlay;
                _logger.LogDebug("Created overlay for monitor {Device}: Physical=({X},{Y},{W},{H})",
                    monitor.DeviceName, monitor.PhysicalBounds.X, monitor.PhysicalBounds.Y,
                    monitor.PhysicalBounds.Width, monitor.PhysicalBounds.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create overlay for monitor {Device}", monitor.DeviceName);
            }
        }
    }

    public void Show()
    {
        if (_isHiddenForDrag) return;
        int fadeMs = _appearance.AnimationsEnabled ? _appearance.FadeDurationMs : 0;
        foreach (var overlay in _overlayWindows.Values)
            overlay.ShowWithAnimation(fadeMs);
    }

    public void Hide()
    {
        int fadeMs = _appearance.AnimationsEnabled ? _appearance.FadeDurationMs : 0;
        foreach (var overlay in _overlayWindows.Values)
            overlay.HideWithAnimation(fadeMs);
    }

    public void HideImmediate()
    {
        foreach (var overlay in _overlayWindows.Values)
            overlay.HideImmediate();
    }

    public void ShowImmediate()
    {
        if (_isHiddenForDrag) return;
        foreach (var overlay in _overlayWindows.Values)
            overlay.ShowImmediate();
    }

    public void SetHiddenForDrag(bool hidden)
    {
        _isHiddenForDrag = hidden;
        if (hidden)
            HideImmediate();
    }

    /// <summary>
    /// Update focus rects. focusRects are in DIP virtual screen coordinates.
    /// </summary>
    public void UpdateFocusRects(List<Rect> focusRectsDip, OverlayMode mode)
    {
        _currentMode = mode;

        foreach (var (hMonitor, overlay) in _overlayWindows)
        {
            var monitor = _monitorManager.Monitors.FirstOrDefault(m => m.HMonitor == hMonitor);
            if (monitor == null) continue;

            // Clip focus rects to this monitor's DIP bounds
            List<Rect> rectsForMonitor;
            switch (mode)
            {
                case OverlayMode.CurrentMonitor:
                    if (focusRectsDip.Count > 0 && RectsIntersectMonitor(monitor.DipBounds, focusRectsDip))
                        rectsForMonitor = ClipRectsToMonitor(monitor.DipBounds, focusRectsDip);
                    else
                        rectsForMonitor = [];
                    break;

                case OverlayMode.AllMonitors:
                default:
                    rectsForMonitor = ClipRectsToMonitor(monitor.DipBounds, focusRectsDip);
                    break;
            }

            overlay.UpdateOverlay(rectsForMonitor, _appearance, monitor);
        }
    }

    public void UpdateAppearance(OverlayAppearance appearance)
    {
        _appearance = appearance;
    }

    private static bool RectsIntersectMonitor(Rect monitorDipBounds, List<Rect> rects)
    {
        foreach (var r in rects)
        {
            var intersection = r;
            intersection.Intersect(monitorDipBounds);
            if (intersection.Width > 0 && intersection.Height > 0)
                return true;
        }
        return false;
    }

    private static List<Rect> ClipRectsToMonitor(Rect monitorDipBounds, List<Rect> rects)
    {
        var result = new List<Rect>();
        foreach (var r in rects)
        {
            var clipped = r;
            clipped.Intersect(monitorDipBounds);
            if (clipped.Width > 0 && clipped.Height > 0)
                result.Add(clipped);
        }
        return result;
    }

    private void DestroyOverlays()
    {
        foreach (var overlay in _overlayWindows.Values)
        {
            try { overlay.Close(); } catch { }
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
