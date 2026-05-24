using FocalFade.Models;
using FocalFade.Native;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Overlay;

/// <summary>
/// Manages segmented blur panels that cover dimmed regions only,
/// leaving the focused window hole clear (no blur behind it).
/// </summary>
public sealed class BlurManager : IDisposable
{
    private readonly ILogger<BlurManager> _logger;
    private readonly Dictionary<IntPtr, List<BlurPanelWindow>> _panelsPerMonitor = new();
    private bool _disposed;
    private bool _blurSupported = true;

    public BlurManager(ILogger<BlurManager> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Update blur panels for a monitor given the monitor bounds and focus holes.
    /// All coordinates in physical pixels.
    /// </summary>
    public void UpdateForMonitor(IntPtr hMonitor, Rect monitorPhysicalBounds, List<Rect> focusHolesPhysical, double intensity)
    {
        if (!IsEnabled)
        {
            HideForMonitor(hMonitor);
            return;
        }

        // Compute the dimmed regions (monitor minus focus holes)
        var panels = ComputeBlurPanels(monitorPhysicalBounds, focusHolesPhysical);

        // Get or create panel windows for this monitor
        if (!_panelsPerMonitor.TryGetValue(hMonitor, out var existingPanels))
        {
            existingPanels = [];
            _panelsPerMonitor[hMonitor] = existingPanels;
        }

        // Reuse or create panels
        while (existingPanels.Count < panels.Count)
            existingPanels.Add(new BlurPanelWindow());

        // Update panel positions and blur
        for (int i = 0; i < panels.Count; i++)
        {
            var panel = existingPanels[i];
            var rect = panels[i];

            if (rect.Width < 2 || rect.Height < 2)
            {
                panel.HideImmediate();
                continue;
            }

            panel.PositionAt((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

            if (!panel.ApplyBlur(intensity))
            {
                if (_blurSupported)
                {
                    _logger.LogWarning("Acrylic blur not supported, trying fallback");
                    if (!panel.ApplyBlurFallback())
                    {
                        _logger.LogWarning("Blur not supported on this system, disabling");
                        _blurSupported = false;
                        IsEnabled = false;
                        HideForMonitor(hMonitor);
                        return;
                    }
                }
            }

            panel.ShowImmediate();
        }

        // Hide unused panels
        for (int i = panels.Count; i < existingPanels.Count; i++)
            existingPanels[i].HideImmediate();
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        if (!enabled)
            HideAll();
    }

    public void HideForMonitor(IntPtr hMonitor)
    {
        if (_panelsPerMonitor.TryGetValue(hMonitor, out var panels))
        {
            foreach (var p in panels)
                p.HideImmediate();
        }
    }

    public void HideAll()
    {
        foreach (var panels in _panelsPerMonitor.Values)
            foreach (var p in panels)
                p.HideImmediate();
    }

    /// <summary>
    /// Compute blur panel rectangles that cover the monitor minus the focus holes.
    /// Returns up to 4 panels: top, bottom, left, right of the combined focus area.
    /// </summary>
    public static List<Rect> ComputeBlurPanels(Rect monitorBounds, List<Rect> focusHoles)
    {
        if (focusHoles.Count == 0)
            return [monitorBounds];

        // Compute bounding box of all focus holes
        var focusBounds = focusHoles[0];
        foreach (var hole in focusHoles.Skip(1))
            focusBounds.Union(hole);

        // Expand by margin
        double margin = 8;
        focusBounds = new Rect(
            focusBounds.X - margin,
            focusBounds.Y - margin,
            focusBounds.Width + margin * 2,
            focusBounds.Height + margin * 2);

        // Clip to monitor
        focusBounds.Intersect(monitorBounds);

        var panels = new List<Rect>();

        // Top panel: from monitor top to focus top
        if (focusBounds.Y > monitorBounds.Y)
        {
            panels.Add(new Rect(
                monitorBounds.X,
                monitorBounds.Y,
                monitorBounds.Width,
                focusBounds.Y - monitorBounds.Y));
        }

        // Bottom panel: from focus bottom to monitor bottom
        double focusBottom = focusBounds.Y + focusBounds.Height;
        double monitorBottom = monitorBounds.Y + monitorBounds.Height;
        if (focusBottom < monitorBottom)
        {
            panels.Add(new Rect(
                monitorBounds.X,
                focusBottom,
                monitorBounds.Width,
                monitorBottom - focusBottom));
        }

        // Left panel: from monitor left to focus left, between top and bottom
        if (focusBounds.X > monitorBounds.X)
        {
            panels.Add(new Rect(
                monitorBounds.X,
                focusBounds.Y,
                focusBounds.X - monitorBounds.X,
                focusBounds.Height));
        }

        // Right panel: from focus right to monitor right, between top and bottom
        double focusRight = focusBounds.X + focusBounds.Width;
        double monitorRight = monitorBounds.X + monitorBounds.Width;
        if (focusRight < monitorRight)
        {
            panels.Add(new Rect(
                focusRight,
                focusBounds.Y,
                monitorRight - focusRight,
                focusBounds.Height));
        }

        return panels;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var panels in _panelsPerMonitor.Values)
            foreach (var p in panels)
                p.Close();
        _panelsPerMonitor.Clear();
    }
}
