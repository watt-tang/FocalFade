using FocalFade.Models;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Tray;

public sealed class TrayService : IDisposable
{
    private readonly ILogger<TrayService> _logger;
    private TaskbarIcon? _trayIcon;
    private bool _disposed;

    public event EventHandler? ShowSettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayService(ILogger<TrayService> logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        try
        {
            _trayIcon = new TaskbarIcon();
            _trayIcon.ToolTipText = "FocalFade: Off";

            // Try to load icon from resources
            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/focalfade.ico", UriKind.Absolute);
                _trayIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(iconUri);
            }
            catch
            {
                // Fallback: use a simple generated icon
                _logger.LogWarning("Could not load tray icon, using fallback");
            }

            _trayIcon.TrayMouseDoubleClick += (_, _) => ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
            _trayIcon.Visibility = Visibility.Visible;
            _logger.LogInformation("Tray icon initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tray icon");
        }
    }

    public void UpdateMenu(bool isEnabled, OverlayMode mode, double opacity, bool presentationMode,
        Action<bool> onToggleEnabled,
        Action<OverlayMode> onModeChanged,
        Action<double> onOpacityChanged,
        Action<bool> onPresentationModeChanged,
        Action onShowSettings,
        Action onPause5Min,
        Action onResume)
    {
        if (_trayIcon == null) return;

        _trayIcon.ContextMenu = TrayMenuBuilder.BuildTrayMenu(
            isEnabled, mode, opacity, presentationMode,
            onToggleEnabled, onModeChanged, onOpacityChanged,
            onPresentationModeChanged, onShowSettings, onPause5Min, onResume,
            () => ExitRequested?.Invoke(this, EventArgs.Empty));

        _trayIcon.ToolTipText = TrayMenuBuilder.GetTooltipText(isEnabled, mode);
    }

    public void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        _trayIcon?.ShowBalloonTip(title, message, icon);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _trayIcon?.Dispose();
    }
}
