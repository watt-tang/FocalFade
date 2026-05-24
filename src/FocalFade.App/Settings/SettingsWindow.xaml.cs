using FocalFade.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FocalFade.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(ISettingsStore settingsStore, IStartupManager startupManager,
        IOverlayManager overlayManager, IActiveWindowTracker activeWindowTracker)
    {
        _viewModel = new SettingsViewModel(settingsStore, startupManager, overlayManager, activeWindowTracker);
        DataContext = _viewModel;
        InitializeComponent();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    public static IValueConverter HexToColorConverter { get; } = new HexToColorValueConverter();

    private class HexToColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && Core.ColorService.TryParseHex(hex, out var color))
                return color;
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
                return Core.ColorService.ToHexRgb(color);
            return "#000000";
        }
    }
}
