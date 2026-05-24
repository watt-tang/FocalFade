using FocalFade.Models;
using FocalFade.Tray;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Windows;

namespace FocalFade.Core;

public sealed class TrayThemeService : IDisposable
{
    private readonly ILogger<TrayThemeService> _logger;
    private readonly TrayService _trayService;
    private TrayIconTheme _currentTheme = TrayIconTheme.Auto;
    private bool _disposed;

    public TrayThemeService(ILogger<TrayThemeService> logger, TrayService trayService)
    {
        _logger = logger;
        _trayService = trayService;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void Initialize()
    {
        ApplyTheme(TrayIconTheme.Auto);
    }

    public void ApplyTheme(TrayIconTheme theme)
    {
        _currentTheme = theme;
        bool isDark = IsDarkTheme(theme);

        try
        {
            _trayService.SetTheme(isDark);
            _logger.LogDebug("Applied tray theme: {Theme} (isDark={IsDark})", theme, isDark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply tray theme");
        }
    }

    public bool IsDarkTheme(TrayIconTheme theme)
    {
        return theme switch
        {
            TrayIconTheme.Light => false,
            TrayIconTheme.Dark => true,
            TrayIconTheme.Auto => DetectSystemDarkTheme(),
            _ => DetectSystemDarkTheme()
        };
    }

    private bool DetectSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                    return intValue == 0; // 0 = dark, 1 = light
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read theme from registry");
        }
        return false; // Default to light
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _currentTheme == TrayIconTheme.Auto)
        {
            Application.Current.Dispatcher.BeginInvoke(() => ApplyTheme(TrayIconTheme.Auto));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}
