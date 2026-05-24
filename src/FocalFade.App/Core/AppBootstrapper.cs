using FocalFade.Models;
using FocalFade.Overlay;
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
    private readonly TrayThemeService _trayThemeService;
    private bool _disposed;
    private bool _isPausedForDrag;

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
        _trayThemeService = host.Services.GetRequiredService<TrayThemeService>();
    }

    public void Start()
    {
        try
        {
            if (!_singleInstance.TryAcquire())
            {
                MessageBox.Show("FocalFade is already running.", "FocalFade", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }

            _settingsStore.Load();
            var settings = _settingsStore.Settings;

            _appRuleManager.SetRules(settings.AppRules);

            // Initialize tray with theme support
            _trayService.Initialize();
            _trayThemeService.Initialize();
            _trayService.ExitRequested += (_, _) => Shutdown();
            _trayService.ShowSettingsRequested += (_, _) => ShowSettingsWindow();
            UpdateTrayMenu();

            // Initialize overlay manager
            _overlayManager.RecreateOverlays();
            _overlayManager.UpdateAppearance(CreateAppearance(settings));

            // Wire up active window tracker
            _activeWindowTracker.ForegroundChanged += OnForegroundChanged;
            _activeWindowTracker.DragStarted += OnDragStarted;
            _activeWindowTracker.DragEnded += OnDragEnded;
            _activeWindowTracker.Start();

            // Register hotkeys
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.RegisterHotkeys(settings.Hotkeys);

            if (settings.Enabled)
                _overlayManager.Show();

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

    private void OnForegroundChanged(object? sender, WindowInfo window)
    {
        try
        {
            var settings = _settingsStore.Settings;

            if (!settings.Enabled || _isPausedForDrag)
            {
                if (!settings.Enabled)
                    _overlayManager.Hide();
                return;
            }

            // Invalid/hidden target
            if (window.Hwnd == IntPtr.Zero)
            {
                _overlayManager.Hide();
                return;
            }

            // Check app rules
            var ruleResult = _appRuleManager.Evaluate(window.ProcessName);
            if (ruleResult.Behavior == DimmingBehavior.Ignore)
            {
                _overlayManager.Hide();
                return;
            }

            // Check fullscreen
            if (settings.PauseOnFullscreen && window.IsFullscreen &&
                settings.FullscreenBehavior == FullscreenBehavior.Pause)
            {
                _overlayManager.Hide();
                return;
            }

            // Compute effective appearance (with per-app overrides)
            var appearance = CreateAppearance(settings);
            if (ruleResult.OpacityOverride.HasValue)
                appearance = appearance with { Opacity = Math.Clamp(ruleResult.OpacityOverride.Value, 0.10, 0.90) };
            if (ruleResult.DimColorOverride != null)
            {
                try
                {
                    var c = System.Windows.Media.ColorConverter.ConvertFromString(ruleResult.DimColorOverride) as System.Windows.Media.Color?;
                    if (c.HasValue) appearance = appearance with { DimColor = c.Value };
                }
                catch { }
            }
            if (ruleResult.FocusMarginOverride.HasValue)
                appearance = appearance with { FocusMargin = ruleResult.FocusMarginOverride.Value };
            if (ruleResult.CornerRadiusOverride.HasValue)
                appearance = appearance with { CornerRadius = ruleResult.CornerRadiusOverride.Value };

            // Compute focus rects
            var focusRects = new List<Rect>();
            if (settings.OverlayMode == OverlayMode.ActiveApp)
            {
                var windows = _activeWindowTracker.GetVisibleWindowsForProcess(window.ProcessId);
                focusRects.AddRange(windows.Select(w => w.DipBounds));
            }
            else
            {
                focusRects.Add(window.DipBounds);
            }

            _overlayManager.UpdateFocusRects(focusRects, appearance);
            _overlayManager.Show();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling foreground change");
        }
    }

    private void OnDragStarted(object? sender, EventArgs e)
    {
        var settings = _settingsStore.Settings;
        if (!settings.HideOverlayWhileDragging) return;

        _isPausedForDrag = true;
        _overlayManager.Hide();
    }

    private void OnDragEnded(object? sender, EventArgs e)
    {
        _isPausedForDrag = false;
        // Re-evaluation will happen from the tracker's CheckForegroundWindow call
    }

    private void OnHotkeyPressed(object? sender, int hotkeyId)
    {
        try
        {
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
                    new System.Threading.Timer(_ =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (_settingsStore.Settings.Enabled)
                                _overlayManager.Show();
                        });
                    }, null, 10000, Timeout.Infinite);
                    break;
                case Native.NativeConstants.HOTKEY_OPEN_SETTINGS:
                    Application.Current.Dispatcher.BeginInvoke(() => ShowSettingsWindow());
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error handling hotkey");
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        _appRuleManager.SetRules(settings.AppRules);
        _overlayManager.UpdateAppearance(CreateAppearance(settings));
        _trayThemeService.ApplyTheme(settings.TrayIconTheme);
        UpdateTrayMenu();

        if (settings.Enabled)
        {
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

    internal static OverlayAppearance CreateAppearance(AppSettings settings)
    {
        var color = System.Windows.Media.ColorConverter.ConvertFromString(settings.DimColor) as System.Windows.Media.Color?
            ?? System.Windows.Media.Colors.Black;

        System.Windows.Media.Color borderColor;
        try
        {
            borderColor = (System.Windows.Media.ColorConverter.ConvertFromString(settings.BorderColor) as System.Windows.Media.Color?)
                ?? System.Windows.Media.Color.FromArgb(80, 255, 255, 255);
        }
        catch { borderColor = System.Windows.Media.Color.FromArgb(80, 255, 255, 255); }

        return new OverlayAppearance
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
        _trayThemeService.Dispose();
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
