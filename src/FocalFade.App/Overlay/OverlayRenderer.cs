using FocalFade.Core;
using FocalFade.Models;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Overlay;

public sealed class OverlayRenderer : IDisposable
{
    private readonly ILogger<OverlayRenderer> _logger;
    private readonly IMonitorManager _monitorManager;
    private readonly BlurManager _blurManager;
    private readonly Dictionary<IntPtr, FocusOverlayWindow> _overlayWindows = new();
    private OverlayAppearance _appearance = new();
    private OverlayMode _currentMode = OverlayMode.AllMonitors;
    private bool _disposed;
    private bool _isHiddenForDrag;
    private bool _blurEnabled;
    private double _blurIntensity = 0.6;

    public OverlayRenderer(ILogger<OverlayRenderer> logger, ILoggerFactory loggerFactory, IMonitorManager monitorManager)
    {
        _logger = logger;
        _monitorManager = monitorManager;
        _blurManager = new BlurManager(loggerFactory.CreateLogger<BlurManager>());
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
        _blurManager.HideAll();
    }

    public void HideImmediate()
    {
        foreach (var overlay in _overlayWindows.Values)
            overlay.HideImmediate();
        _blurManager.HideAll();
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
        {
            HideImmediate();
            _blurManager.HideAll();
        }
    }

    public void SetBlur(bool enabled, double intensity)
    {
        _blurEnabled = enabled;
        _blurIntensity = intensity;
        _blurManager.SetEnabled(enabled);
        if (!enabled)
            _blurManager.HideAll();
    }

    public void UpdateFocusRects(List<Rect> focusRectsDip, OverlayMode mode)
    {
        _currentMode = mode;

        foreach (var (hMonitor, overlay) in _overlayWindows)
        {
            var monitor = _monitorManager.Monitors.FirstOrDefault(m => m.HMonitor == hMonitor);
            if (monitor == null) continue;

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

            // Update blur panels (convert DIP rects to physical for blur panels)
            if (_blurEnabled)
            {
                var physicalRects = rectsForMonitor.Select(r => DpiCoordinator.DipToPhysical(r, monitor.DpiScaleX, monitor.DpiScaleY)).ToList();
                _blurManager.UpdateForMonitor(hMonitor, monitor.PhysicalBounds, physicalRects, _blurIntensity);
            }
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
            try { overlay.Close(); } catch { }
        _overlayWindows.Clear();
        _blurManager.HideAll();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyOverlays();
        _blurManager.Dispose();
    }
}
