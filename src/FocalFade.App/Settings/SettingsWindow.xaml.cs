using FocalFade.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
        // Hide instead of close so it can be reopened
        e.Cancel = true;
        Hide();
    }

    // Color converters for radio buttons
    public static IValueConverter BlackColorConverter { get; } = new ColorMatchConverter("#000000");
    public static IValueConverter DarkGrayConverter { get; } = new ColorMatchConverter("#333333");
    public static IValueConverter DarkBlueConverter { get; } = new ColorMatchConverter("#1A2744");

    private class ColorMatchConverter : IValueConverter
    {
        private readonly string _matchColor;
        public ColorMatchConverter(string matchColor) => _matchColor = matchColor;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Equals(value as string, _matchColor, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? _matchColor : Binding.DoNothing;
        }
    }
}
