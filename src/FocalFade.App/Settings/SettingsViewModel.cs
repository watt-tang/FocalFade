using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocalFade.Core;
using FocalFade.Models;
using FocalFade.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FocalFade.Settings;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IStartupManager _startupManager;
    private readonly IOverlayManager _overlayManager;
    private readonly IActiveWindowTracker _activeWindowTracker;
    private bool _suppressSave;

    public SettingsViewModel(ISettingsStore settingsStore, IStartupManager startupManager,
        IOverlayManager overlayManager, IActiveWindowTracker activeWindowTracker)
    {
        _settingsStore = settingsStore;
        _startupManager = startupManager;
        _overlayManager = overlayManager;
        _activeWindowTracker = activeWindowTracker;

        LoadFromSettings(_settingsStore.Settings);
        AppRules = new ObservableCollection<AppRule>(_settingsStore.Settings.AppRules);
        ColorPresets = new ObservableCollection<ColorPreset>(
            ColorService.Presets.Select(p => new ColorPreset(p.Name, p.Hex)));
    }

    // General
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private bool _startEnabled;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _pauseOnFullscreen = true;
    [ObservableProperty] private bool _presentationModeEnabled;
    [ObservableProperty] private bool _hideOverlayWhileDragging = true;

    // Appearance
    [ObservableProperty] private double _opacity = 0.45;
    [ObservableProperty] private string _dimColor = "#000000";
    [ObservableProperty] private double _focusMargin = 8;
    [ObservableProperty] private double _cornerRadius = 8;
    [ObservableProperty] private bool _animationsEnabled = true;
    [ObservableProperty] private int _fadeDurationMs = 120;
    [ObservableProperty] private bool _showBorder;
    [ObservableProperty] private string _borderColor = "#50FFFFFF";
    [ObservableProperty] private bool _blurEnabled;
    [ObservableProperty] private double _blurIntensity = 0.6;

    // Focus behavior
    [ObservableProperty] private OverlayMode _overlayMode;
    [ObservableProperty] private FullscreenBehavior _fullscreenBehavior;

    // Tray theme
    [ObservableProperty] private TrayIconTheme _trayIconTheme;

    // Diagnostics
    [ObservableProperty] private bool _verboseLogging;
    [ObservableProperty] private bool _showOverlayDiagnostics;
    [ObservableProperty] private string _version = "";

    // App rules
    public ObservableCollection<AppRule> AppRules { get; }

    // Color presets
    public ObservableCollection<ColorPreset> ColorPresets { get; }

    public OverlayMode[] OverlayModes => Enum.GetValues<OverlayMode>();
    public FullscreenBehavior[] FullscreenBehaviors => Enum.GetValues<FullscreenBehavior>();
    public TrayIconTheme[] TrayIconThemes => Enum.GetValues<TrayIconTheme>();

    public string OpacityPercent
    {
        get => $"{(int)(Opacity * 100)}%";
        set
        {
            if (int.TryParse(value.Replace("%", ""), out int pct))
                Opacity = Math.Clamp(pct / 100.0, 0.10, 0.90);
        }
    }

    // Change callbacks
    partial void OnEnabledChanged(bool value) => SaveSettings();
    partial void OnStartEnabledChanged(bool value) => SaveSettings();
    partial void OnOpacityChanged(double value) => SaveSettings();
    partial void OnDimColorChanged(string value) => SaveSettings();
    partial void OnFocusMarginChanged(double value) => SaveSettings();
    partial void OnCornerRadiusChanged(double value) => SaveSettings();
    partial void OnAnimationsEnabledChanged(bool value) => SaveSettings();
    partial void OnFadeDurationMsChanged(int value) => SaveSettings();
    partial void OnOverlayModeChanged(OverlayMode value) => SaveSettings();
    partial void OnFullscreenBehaviorChanged(FullscreenBehavior value) => SaveSettings();
    partial void OnPauseOnFullscreenChanged(bool value) => SaveSettings();
    partial void OnPresentationModeEnabledChanged(bool value) => SaveSettings();
    partial void OnVerboseLoggingChanged(bool value) => SaveSettings();
    partial void OnShowBorderChanged(bool value) => SaveSettings();
    partial void OnBorderColorChanged(string value) => SaveSettings();
    partial void OnHideOverlayWhileDraggingChanged(bool value) => SaveSettings();
    partial void OnBlurEnabledChanged(bool value) => SaveSettings();
    partial void OnBlurIntensityChanged(double value) => SaveSettings();
    partial void OnTrayIconThemeChanged(TrayIconTheme value) => SaveSettings();
    partial void OnShowOverlayDiagnosticsChanged(bool value) => SaveSettings();

    partial void OnStartWithWindowsChanged(bool value)
    {
        _startupManager.SetStartupRegistration(value);
        SaveSettings();
    }

    [RelayCommand]
    private void ApplyColorPreset(ColorPreset? preset)
    {
        if (preset != null)
            DimColor = preset.Hex;
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _suppressSave = true;
        _settingsStore.ResetToDefaults();
        LoadFromSettings(_settingsStore.Settings);
        AppRules.Clear();
        foreach (var rule in _settingsStore.Settings.AppRules)
            AppRules.Add(rule);
        _suppressSave = false;
    }

    [RelayCommand]
    private void AddCurrentApp()
    {
        var current = _activeWindowTracker.CurrentWindow;
        if (current == null) return;

        if (AppRules.Any(r => string.Equals(r.ProcessName, current.ProcessName, StringComparison.OrdinalIgnoreCase)))
            return;

        AppRules.Add(new AppRule
        {
            ProcessName = current.ProcessName,
            DisplayName = current.ProcessName,
            Behavior = DimmingBehavior.Ignore,
            Enabled = true
        });
        SaveSettings();
    }

    [RelayCommand]
    private void RemoveSelectedRule(AppRule? rule)
    {
        if (rule == null) return;
        AppRules.Remove(rule);
        SaveSettings();
    }

    [RelayCommand]
    private void SetRuleOpacity(AppRule? rule)
    {
        if (rule == null) return;
        // Cycle through: null -> 0.3 -> 0.5 -> 0.7 -> null
        double? newOpacity = rule.OpacityOverride switch
        {
            null => 0.3,
            < 0.4 => 0.5,
            < 0.6 => 0.7,
            _ => null
        };

        var index = AppRules.IndexOf(rule);
        if (index >= 0)
        {
            AppRules[index] = rule with { OpacityOverride = newOpacity };
            SaveSettings();
        }
    }

    private void LoadFromSettings(AppSettings s)
    {
        _suppressSave = true;
        Enabled = s.Enabled;
        StartEnabled = s.StartEnabled;
        StartWithWindows = s.StartWithWindows || _startupManager.IsRegisteredForStartup();
        Opacity = s.Opacity;
        DimColor = s.DimColor;
        FocusMargin = s.FocusMargin;
        CornerRadius = s.CornerRadius;
        AnimationsEnabled = s.AnimationsEnabled;
        FadeDurationMs = s.FadeDurationMs;
        OverlayMode = s.OverlayMode;
        FullscreenBehavior = s.FullscreenBehavior;
        PauseOnFullscreen = s.PauseOnFullscreen;
        PresentationModeEnabled = s.PresentationModeEnabled;
        VerboseLogging = s.VerboseLogging;
        ShowBorder = s.ShowBorder;
        BorderColor = s.BorderColor;
        HideOverlayWhileDragging = s.HideOverlayWhileDragging;
        BlurEnabled = s.BlurEnabled;
        BlurIntensity = s.BlurIntensity;
        TrayIconTheme = s.TrayIconTheme;
        ShowOverlayDiagnostics = s.ShowOverlayDiagnostics;
        Version = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        _suppressSave = false;
    }

    private void SaveSettings()
    {
        if (_suppressSave) return;

        _settingsStore.Update(s => s with
        {
            Enabled = Enabled,
            StartEnabled = StartEnabled,
            StartWithWindows = StartWithWindows,
            Opacity = Opacity,
            DimColor = DimColor,
            FocusMargin = FocusMargin,
            CornerRadius = CornerRadius,
            AnimationsEnabled = AnimationsEnabled,
            FadeDurationMs = FadeDurationMs,
            OverlayMode = OverlayMode,
            FullscreenBehavior = FullscreenBehavior,
            PauseOnFullscreen = PauseOnFullscreen,
            PresentationModeEnabled = PresentationModeEnabled,
            VerboseLogging = VerboseLogging,
            ShowBorder = ShowBorder,
            BorderColor = BorderColor,
            HideOverlayWhileDragging = HideOverlayWhileDragging,
            BlurEnabled = BlurEnabled,
            BlurIntensity = BlurIntensity,
            TrayIconTheme = TrayIconTheme,
            ShowOverlayDiagnostics = ShowOverlayDiagnostics,
            AppRules = AppRules.ToList()
        });
    }
}

public sealed record ColorPreset(string Name, string Hex);
