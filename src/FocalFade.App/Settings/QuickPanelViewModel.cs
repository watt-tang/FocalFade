using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocalFade.Core;
using FocalFade.Models;
using FocalFade.Services;
using System.Windows.Media;

namespace FocalFade.Settings;

public partial class QuickPanelViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IOverlayManager _overlayManager;
    private bool _suppress;

    public QuickPanelViewModel(ISettingsStore settingsStore, IOverlayManager overlayManager)
    {
        _settingsStore = settingsStore;
        _overlayManager = overlayManager;
        SyncFromSettings(settingsStore.Settings);
    }

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private double _opacity = 0.45;
    [ObservableProperty] private double _focusMargin = 8;
    [ObservableProperty] private double _cornerRadius = 8;
    [ObservableProperty] private bool _presentationMode;
    [ObservableProperty] private bool _blurEnabled;
    [ObservableProperty] private double _blurIntensity = 0.6;
    [ObservableProperty] private bool _showBorder;
    [ObservableProperty] private OverlayMode _overlayMode;
    [ObservableProperty] private string _dimColor = "#000000";

    public OverlayMode[] OverlayModes => Enum.GetValues<OverlayMode>();

    public string OpacityLabel => $"{(int)(Opacity * 100)}%";
    public string MarginLabel => $"{(int)FocusMargin} px";
    public string RadiusLabel => $"{(int)CornerRadius} px";
    public string BlurLabel => $"{(int)(BlurIntensity * 100)}%";

    public Color DimColorPreview
    {
        get
        {
            if (ColorService.TryParseHex(DimColor, out var c)) return c;
            return Colors.Black;
        }
    }

    // Quick color presets
    public record QuickColor(string Name, string Hex, Color Color);
    public QuickColor[] QuickColors =>
    [
        new("Black", "#000000", Colors.Black),
        new("Gray", "#2D3436", Color.FromRgb(0x2D, 0x34, 0x36)),
        new("Navy", "#1A2744", Color.FromRgb(0x1A, 0x27, 0x44)),
        new("Sepia", "#3E2C1C", Color.FromRgb(0x3E, 0x2C, 0x1C)),
        new("Purple", "#1B0A2E", Color.FromRgb(0x1B, 0x0A, 0x2E)),
        new("Teal", "#0D2B2B", Color.FromRgb(0x0D, 0x2B, 0x2B)),
    ];

    partial void OnEnabledChanged(bool value) => Push();
    partial void OnOpacityChanged(double value) { OnPropertyChanged(nameof(OpacityLabel)); Push(); }
    partial void OnFocusMarginChanged(double value) { OnPropertyChanged(nameof(MarginLabel)); Push(); }
    partial void OnCornerRadiusChanged(double value) { OnPropertyChanged(nameof(RadiusLabel)); Push(); }
    partial void OnPresentationModeChanged(bool value) => Push();
    partial void OnBlurEnabledChanged(bool value) => Push();
    partial void OnBlurIntensityChanged(double value) { OnPropertyChanged(nameof(BlurLabel)); Push(); }
    partial void OnShowBorderChanged(bool value) => Push();
    partial void OnOverlayModeChanged(OverlayMode value) => Push();
    partial void OnDimColorChanged(string value) { OnPropertyChanged(nameof(DimColorPreview)); Push(); }

    [RelayCommand]
    private void ApplyQuickColor(string? hex)
    {
        if (hex != null) DimColor = hex;
    }

    [RelayCommand]
    private void ToggleEnabled() => Enabled = !Enabled;

    [RelayCommand]
    private void TogglePresentation() => PresentationMode = !PresentationMode;

    [RelayCommand]
    private void ToggleBlur() => BlurEnabled = !BlurEnabled;

    public void SyncFromSettings(AppSettings s)
    {
        _suppress = true;
        Enabled = s.Enabled;
        Opacity = s.Opacity;
        FocusMargin = s.FocusMargin;
        CornerRadius = s.CornerRadius;
        PresentationMode = s.PresentationModeEnabled;
        BlurEnabled = s.BlurEnabled;
        BlurIntensity = s.BlurIntensity;
        ShowBorder = s.ShowBorder;
        OverlayMode = s.OverlayMode;
        DimColor = s.DimColor;
        _suppress = false;
    }

    private void Push()
    {
        if (_suppress) return;

        _settingsStore.Update(s => s with
        {
            Enabled = Enabled,
            Opacity = Opacity,
            FocusMargin = FocusMargin,
            CornerRadius = CornerRadius,
            PresentationModeEnabled = PresentationMode,
            BlurEnabled = BlurEnabled,
            BlurIntensity = BlurIntensity,
            ShowBorder = ShowBorder,
            OverlayMode = OverlayMode,
            DimColor = DimColor,
        });
    }
}
