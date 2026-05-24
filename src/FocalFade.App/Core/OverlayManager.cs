using FocalFade.Models;
using FocalFade.Overlay;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Core;

public sealed class OverlayManager : IOverlayManager
{
    private readonly ILogger<OverlayManager> _logger;
    private readonly OverlayRenderer _renderer;
    private readonly IMonitorManager _monitorManager;
    private bool _isVisible;
    private OverlayAppearance _appearance = new();

    public OverlayManager(ILogger<OverlayManager> logger, ILoggerFactory loggerFactory, IMonitorManager monitorManager)
    {
        _logger = logger;
        _monitorManager = monitorManager;
        _renderer = new OverlayRenderer(loggerFactory.CreateLogger<OverlayRenderer>(), monitorManager);
    }

    public bool IsVisible => _isVisible;

    public void Show()
    {
        if (_isVisible) return;
        _isVisible = true;
        _renderer.Show();
    }

    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;
        _renderer.Hide();
    }

    public void UpdateFocusRects(List<Rect> focusRects, OverlayAppearance appearance)
    {
        _appearance = appearance;
        _renderer.UpdateAppearance(appearance);
        _renderer.UpdateFocusRects(focusRects, OverlayMode.AllMonitors);
    }

    public void UpdateAppearance(OverlayAppearance appearance)
    {
        _appearance = appearance;
        _renderer.UpdateAppearance(appearance);
    }

    public void RecreateOverlays()
    {
        _renderer.Initialize();
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
