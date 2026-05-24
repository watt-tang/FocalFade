using FocalFade.Services;
using FocalFade.Settings;
using FocalFade.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace FocalFade.Core;

public sealed class AppBootstrapper : IDisposable
{
    private readonly IHost _host;
    private readonly ILogger<AppBootstrapper> _logger;
    private readonly SingleInstanceGuard _singleInstance;
    private readonly ISettingsStore _settingsStore;
    private readonly IMonitorManager _monitorManager;
    private readonly IActiveWindowTracker _activeWindowTracker;
    private readonly IOverlayManager _overlayManager;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IStartupManager _startupManager;
    private readonly IAppRuleManager _appRuleManager;
    private readonly TrayService _trayService;
    private readonly AppLifecycleService _lifecycleService;
    private bool _disposed;

    public AppBootstrapper(IHost host)
    {
        _host = host;
        _logger = host.Services.GetRequiredService<ILogger<AppBootstrapper>>();
        _singleInstance = host.Services.GetRequiredService<SingleInstanceGuard>();
        _settingsStore = host.Services.GetRequiredService<ISettingsStore>();
        _monitorManager = host.Services.GetRequiredService<IMonitorManager>();
        _activeWindowTracker = host.Services.GetRequiredService<IActiveWindowTracker>();
        _overlayManager = host.Services.GetRequiredService<IOverlayManager>();
        _hotkeyManager = host.Services.GetRequiredService<IHotkeyManager>();
        _startupManager = host.Services.GetRequiredService<IStartupManager>();
        _appRuleManager = host.Services.GetRequiredService<IAppRuleManager>();
        _trayService = host.Services.GetRequiredService<TrayService>();
        _lifecycleService = host.Services.GetRequiredService<AppLifecycleService>();
    }

    public void Start()
    {
        try
        {
            // Single instance check
            if (!_singleInstance.TryAcquire())
            {
                _logger.LogInformation("Another instance is already running, exiting");
                MessageBox.Show("FocalFade is already running.", "FocalFade", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }

            // Load settings
            _settingsStore.Load();
            var settings = _settingsStore.Settings;

            // Apply app rules
            _appRuleManager.SetRules(settings.AppRules);

            // Initialize tray
            _trayService.Initialize();
            _trayService.ExitRequested += (_, _) => Shutdown();
            _trayService.ShowSettingsRequested += (_, _) => ShowSettingsWindow();
            UpdateTrayMenu();

            // Initialize overlay manager
            _overlayManager.RecreateOverlays();
            _overlayManager.UpdateAppearance(CreateAppearance(settings));

            // Initialize active window tracker
            _activeWindowTracker.ForegroundChanged += OnForegroundChanged;
            _activeWindowTracker.Start();

            // Register hotkeys
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.RegisterHotkeys(settings.Hotkeys);

            // Apply initial state
            if (settings.Enabled)
            {
                _overlayManager.Show();
            }

            // Listen for settings changes
            _settingsStore.SettingsChanged += OnSettingsChanged;

            _logger.LogInformation("FocalFade started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start FocalFade");
            MessageBox.Show($"Failed to start FocalFade:\n{ex.Message}", "FocalFade Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    private void OnForegroundChanged(object? sender, Models.WindowInfo window)
    {
        try
        {
            var settings = _settingsStore.Settings;

            if (!settings.Enabled)
            {
                _overlayManager.Hide();
                return;
            }

            // Check app rules
            var behavior = _appRuleManager.Evaluate(window.ProcessName);
            if (behavior == Models.DimmingBehavior.Ignore)
            {
                _overlayManager.Hide();
                return;
            }

            // Check fullscreen
            if (settings.PauseOnFullscreen && window.IsFullscreen)
            {
                _overlayManager.Hide();
                return;
            }

            // Compute focus rects
            var focusRects = new List<System.Windows.Rect>();
            if (settings.OverlayMode == Models.OverlayMode.ActiveApp)
            {
                var windows = _activeWindowTracker.GetVisibleWindowsForProcess(window.ProcessId);
                focusRects.AddRange(windows.Select(w => w.DipBounds));
            }
            else
            {
                focusRects.Add(window.DipBounds);
            }

            // Update overlay
            _overlayManager.UpdateFocusRects(focusRects, CreateAppearance(settings));
            _overlayManager.Show();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling foreground change");
        }
    }

    private void OnHotkeyPressed(object? sender, int hotkeyId)
    {
        try
        {
            var settings = _settingsStore.Settings;

            switch (hotkeyId)
            {
                case Native.NativeConstants.HOTKEY_TOGGLE_ENABLED:
                    _settingsStore.Update(s => s with { Enabled = !s.Enabled });
                    break;

                case Native.NativeConstants.HOTKEY_INCREASE_OPACITY:
                    _settingsStore.Update(s => s with { Opacity = Math.Min(0.90, s.Opacity + 0.05) });
                    break;

                case Native.NativeConstants.HOTKEY_DECREASE_OPACITY:
                    _settingsStore.Update(s => s with { Opacity = Math.Max(0.10, s.Opacity - 0.05) });
                    break;

                case Native.NativeConstants.HOTKEY_PRESENTATION_MODE:
                    _settingsStore.Update(s => s with { PresentationModeEnabled = !s.PresentationModeEnabled });
                    break;

                case Native.NativeConstants.HOTKEY_TEMPORARY_PEEK:
                    _overlayManager.Hide();
                    var timer = new System.Threading.Timer(_ =>
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (_settingsStore.Settings.Enabled)
                                _overlayManager.Show();
                        });
                    }, null, 10000, Timeout.Infinite);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling hotkey");
        }
    }

    private void OnSettingsChanged(object? sender, Models.AppSettings settings)
    {
        _appRuleManager.SetRules(settings.AppRules);
        _overlayManager.UpdateAppearance(CreateAppearance(settings));
        UpdateTrayMenu();

        if (settings.Enabled)
        {
            // Trigger a re-evaluation
            var currentWindow = _activeWindowTracker.CurrentWindow;
            if (currentWindow != null)
                OnForegroundChanged(this, currentWindow);
        }
        else
        {
            _overlayManager.Hide();
        }
    }

    private void UpdateTrayMenu()
    {
        var settings = _settingsStore.Settings;
        _trayService.UpdateMenu(
            settings.Enabled,
            settings.OverlayMode,
            settings.Opacity,
            settings.PresentationModeEnabled,
            onToggleEnabled: v => _settingsStore.Update(s => s with { Enabled = v }),
            onModeChanged: m => _settingsStore.Update(s => s with { OverlayMode = m }),
            onOpacityChanged: o => _settingsStore.Update(s => s with { Opacity = o }),
            onPresentationModeChanged: v => _settingsStore.Update(s => s with { PresentationModeEnabled = v }),
            onShowSettings: () => ShowSettingsWindow(),
            onPause5Min: () => _overlayManager.Hide(),
            onResume: () => { if (settings.Enabled) _overlayManager.Show(); });
    }

    private SettingsWindow? _settingsWindow;

    private void ShowSettingsWindow()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settingsStore, _startupManager, _overlayManager, _activeWindowTracker);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private static Models.OverlayAppearance CreateAppearance(Models.AppSettings settings)
    {
        var color = System.Windows.Media.ColorConverter.ConvertFromString(settings.DimColor) as System.Windows.Media.Color?
            ?? System.Windows.Media.Colors.Black;

        System.Windows.Media.Color borderColor;
        try
        {
            borderColor = (System.Windows.Media.ColorConverter.ConvertFromString(settings.BorderColor) as System.Windows.Media.Color?)
                ?? System.Windows.Media.Color.FromArgb(80, 255, 255, 255);
        }
        catch
        {
            borderColor = System.Windows.Media.Color.FromArgb(80, 255, 255, 255);
        }

        return new Models.OverlayAppearance
        {
            Opacity = settings.Opacity,
            DimColor = color,
            FocusMargin = settings.FocusMargin,
            CornerRadius = settings.CornerRadius,
            AnimationsEnabled = settings.AnimationsEnabled,
            FadeDurationMs = settings.FadeDurationMs,
            MoveDurationMs = settings.MoveDurationMs,
            ShowBorder = settings.ShowBorder || settings.PresentationModeEnabled,
            BorderColor = borderColor
        };
    }

    private void Shutdown()
    {
        _logger.LogInformation("Shutting down FocalFade");
        _activeWindowTracker.Stop();
        _overlayManager.Hide();
        _hotkeyManager.UnregisterAll();
        _trayService.Dispose();
        _settingsStore.Save();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _activeWindowTracker.Dispose();
        _overlayManager.Dispose();
        _hotkeyManager.Dispose();
        _monitorManager.Dispose();
        _singleInstance.Dispose();
    }
}
