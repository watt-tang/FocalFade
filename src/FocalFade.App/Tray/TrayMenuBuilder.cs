using FocalFade.Models;
using System.Windows.Controls;

namespace FocalFade.Tray;

public static class TrayMenuBuilder
{
    public static ContextMenu BuildTrayMenu(
        bool isEnabled,
        OverlayMode currentMode,
        double currentOpacity,
        bool presentationMode,
        Action<bool> onToggleEnabled,
        Action<OverlayMode> onModeChanged,
        Action<double> onOpacityChanged,
        Action<bool> onPresentationModeChanged,
        Action onShowSettings,
        Action onPause5Min,
        Action onResume,
        Action onExit)
    {
        var menu = new ContextMenu();

        // Header
        var header = new MenuItem { Header = "FocalFade", IsEnabled = false };
        header.FontWeight = System.Windows.FontWeights.SemiBold;
        menu.Items.Add(header);
        menu.Items.Add(new Separator());

        // Enabled toggle
        var enabledItem = new MenuItem
        {
            Header = isEnabled ? "Enabled" : "Disabled",
            IsCheckable = true,
            IsChecked = isEnabled
        };
        enabledItem.Click += (_, _) => onToggleEnabled(!isEnabled);
        menu.Items.Add(enabledItem);
        menu.Items.Add(new Separator());

        // Mode submenu
        var modeMenu = new MenuItem { Header = "Mode" };
        foreach (var mode in Enum.GetValues<OverlayMode>())
        {
            var modeItem = new MenuItem
            {
                Header = GetModeDisplayName(mode),
                IsCheckable = true,
                IsChecked = mode == currentMode
            };
            var capturedMode = mode;
            modeItem.Click += (_, _) => onModeChanged(capturedMode);
            modeMenu.Items.Add(modeItem);
        }
        menu.Items.Add(modeMenu);

        // Opacity submenu
        var opacityMenu = new MenuItem { Header = "Opacity" };
        double[] presets = [0.20, 0.35, 0.45, 0.55, 0.70];
        foreach (var preset in presets)
        {
            var opacityItem = new MenuItem
            {
                Header = $"{preset:P0}",
                IsCheckable = true,
                IsChecked = Math.Abs(currentOpacity - preset) < 0.01
            };
            var capturedOpacity = preset;
            opacityItem.Click += (_, _) => onOpacityChanged(capturedOpacity);
            opacityMenu.Items.Add(opacityItem);
        }
        menu.Items.Add(opacityMenu);
        menu.Items.Add(new Separator());

        // Presentation Mode
        var presItem = new MenuItem
        {
            Header = "Presentation Mode",
            IsCheckable = true,
            IsChecked = presentationMode
        };
        presItem.Click += (_, _) => onPresentationModeChanged(!presentationMode);
        menu.Items.Add(presItem);
        menu.Items.Add(new Separator());

        // Pause / Resume
        var pauseItem = new MenuItem { Header = "Pause for 5 minutes" };
        pauseItem.Click += (_, _) => onPause5Min();
        menu.Items.Add(pauseItem);

        var resumeItem = new MenuItem { Header = "Resume" };
        resumeItem.Click += (_, _) => onResume();
        menu.Items.Add(resumeItem);
        menu.Items.Add(new Separator());

        // Settings
        var settingsItem = new MenuItem { Header = "Settings..." };
        settingsItem.Click += (_, _) => onShowSettings();
        menu.Items.Add(settingsItem);
        menu.Items.Add(new Separator());

        // Exit
        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => onExit();
        menu.Items.Add(exitItem);

        return menu;
    }

    public static string GetTooltipText(bool isEnabled, OverlayMode mode)
    {
        var status = isEnabled ? "On" : "Off";
        var modeStr = mode switch
        {
            OverlayMode.ActiveWindow => "Active Window",
            OverlayMode.ActiveApp => "Active App",
            _ => ""
        };
        return string.IsNullOrEmpty(modeStr) ? $"FocalFade: {status}" : $"FocalFade: {status} ({modeStr})";
    }

    private static string GetModeDisplayName(OverlayMode mode) => mode switch
    {
        OverlayMode.ActiveWindow => "Active Window",
        OverlayMode.ActiveApp => "Active App",
        OverlayMode.CurrentMonitor => "Current Monitor Only",
        OverlayMode.AllMonitors => "All Monitors",
        _ => mode.ToString()
    };
}
