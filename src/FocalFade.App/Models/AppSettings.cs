using System.Text.Json.Serialization;

namespace FocalFade.Models;

public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 2;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    [JsonPropertyName("startEnabled")]
    public bool StartEnabled { get; init; } = true;

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; init; }

    [JsonPropertyName("opacity")]
    public double Opacity { get; init; } = 0.45;

    [JsonPropertyName("dimColor")]
    public string DimColor { get; init; } = "#000000";

    [JsonPropertyName("focusMargin")]
    public double FocusMargin { get; init; } = 8.0;

    [JsonPropertyName("cornerRadius")]
    public double CornerRadius { get; init; } = 8.0;

    [JsonPropertyName("animationsEnabled")]
    public bool AnimationsEnabled { get; init; } = true;

    [JsonPropertyName("fadeDurationMs")]
    public int FadeDurationMs { get; init; } = 120;

    [JsonPropertyName("moveDurationMs")]
    public int MoveDurationMs { get; init; } = 80;

    [JsonPropertyName("overlayMode")]
    public OverlayMode OverlayMode { get; init; } = OverlayMode.ActiveWindow;

    [JsonPropertyName("dimmingBehavior")]
    public DimmingBehavior DimmingBehavior { get; init; } = DimmingBehavior.Normal;

    [JsonPropertyName("fullscreenBehavior")]
    public FullscreenBehavior FullscreenBehavior { get; init; } = FullscreenBehavior.Pause;

    [JsonPropertyName("pauseOnFullscreen")]
    public bool PauseOnFullscreen { get; init; } = true;

    [JsonPropertyName("presentationModeEnabled")]
    public bool PresentationModeEnabled { get; init; }

    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; init; }

    [JsonPropertyName("includeWindowTitles")]
    public bool IncludeWindowTitles { get; init; }

    [JsonPropertyName("showBorder")]
    public bool ShowBorder { get; init; }

    [JsonPropertyName("borderColor")]
    public string BorderColor { get; init; } = "#50FFFFFF";

    // New settings for stabilization
    [JsonPropertyName("hideOverlayWhileDragging")]
    public bool HideOverlayWhileDragging { get; init; } = true;

    [JsonPropertyName("ignoreTinyWindows")]
    public bool IgnoreTinyWindows { get; init; } = true;

    [JsonPropertyName("tinyWindowMinWidth")]
    public int TinyWindowMinWidth { get; init; } = Native.NativeConstants.TinyWindowMinWidth;

    [JsonPropertyName("tinyWindowMinHeight")]
    public int TinyWindowMinHeight { get; init; } = Native.NativeConstants.TinyWindowMinHeight;

    [JsonPropertyName("useLastValidTargetOnShellFocus")]
    public bool UseLastValidTargetOnShellFocus { get; init; } = true;

    [JsonPropertyName("lastValidTargetGracePeriodMs")]
    public int LastValidTargetGracePeriodMs { get; init; } = 1500;

    [JsonPropertyName("showOverlayDiagnostics")]
    public bool ShowOverlayDiagnostics { get; init; }

    // Blur settings
    [JsonPropertyName("blurEnabled")]
    public bool BlurEnabled { get; init; }

    [JsonPropertyName("blurIntensity")]
    public double BlurIntensity { get; init; } = 0.6;

    // Tray icon theme
    [JsonPropertyName("trayIconTheme")]
    public TrayIconTheme TrayIconTheme { get; init; } = TrayIconTheme.Auto;

    [JsonPropertyName("appRules")]
    public List<AppRule> AppRules { get; init; } = GetDefaultAppRules();

    [JsonPropertyName("hotkeys")]
    public Dictionary<string, string> Hotkeys { get; init; } = GetDefaultHotkeys();

    [JsonPropertyName("settingsWindowLeft")]
    public double? SettingsWindowLeft { get; init; }

    [JsonPropertyName("settingsWindowTop")]
    public double? SettingsWindowTop { get; init; }

    public static List<AppRule> GetDefaultAppRules() =>
    [
        new() { ProcessName = "POWERPNT.EXE", DisplayName = "PowerPoint", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "obs64.exe", DisplayName = "OBS Studio", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "Zoom.exe", DisplayName = "Zoom", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "Teams.exe", DisplayName = "Microsoft Teams", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "vlc.exe", DisplayName = "VLC Media Player", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "mpv.exe", DisplayName = "mpv", Behavior = DimmingBehavior.Ignore, Enabled = true },
        new() { ProcessName = "PotPlayerMini64.exe", DisplayName = "PotPlayer", Behavior = DimmingBehavior.Ignore, Enabled = true },
    ];

    public static Dictionary<string, string> GetDefaultHotkeys() =>
    new()
    {
        ["ToggleEnabled"] = "Ctrl+Alt+F",
        ["IncreaseOpacity"] = "Ctrl+Alt+Up",
        ["DecreaseOpacity"] = "Ctrl+Alt+Down",
        ["PresentationMode"] = "Ctrl+Alt+P",
        ["TemporaryPeek"] = "Ctrl+Alt+Space",
        ["OpenSettings"] = "Ctrl+Alt+S"
    };
}
