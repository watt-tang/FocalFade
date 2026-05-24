using FocalFade.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FocalFade.Settings;

public partial class QuickPanelWindow : Window
{
    private readonly QuickPanelViewModel _viewModel;
    private readonly ISettingsStore _settingsStore;

    // Converters
    public static IValueConverter BoolToToggleColor { get; } = new BoolToColorConverter(
        Color.FromRgb(0x5B, 0x9C, 0xF6), // on - accent blue
        Color.FromRgb(0x44, 0x44, 0x44)); // off - dark gray

    public static IValueConverter BoolToAccentColor { get; } = new BoolToColorConverter(
        Color.FromRgb(0x5B, 0x9C, 0xF6),
        Color.FromRgb(0x66, 0x66, 0x66));

    public static IValueConverter BoolToOnOff { get; } = new BoolToStringConverter("On", "Off");

    public static IValueConverter BoolToToggleOffset { get; } = new BoolToDoubleConverter(16, 0);

    public QuickPanelWindow(ISettingsStore settingsStore, IOverlayManager overlayManager)
    {
        _settingsStore = settingsStore;
        _viewModel = new QuickPanelViewModel(settingsStore, overlayManager);
        DataContext = _viewModel;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Position near cursor, clamped to screen
        Native.User32.GetCursorPos(out var cursorPt);
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        double left = cursorPt.X - Width / 2;
        double top = cursorPt.Y - Height - 20;

        // Clamp to screen
        left = Math.Max(8, Math.Min(left, screenWidth - Width - 8));
        top = Math.Max(8, Math.Min(top, screenHeight - Height - 8));

        Left = left;
        Top = top;

        // Sync with current settings
        _viewModel.SyncFromSettings(_settingsStore.Settings);

        // Listen for external settings changes
        _settingsStore.SettingsChanged += OnExternalSettingsChanged;
    }

    private void OnExternalSettingsChanged(object? sender, Models.AppSettings settings)
    {
        Dispatcher.BeginInvoke(() => _viewModel.SyncFromSettings(settings));
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Auto-hide when clicking away
        _settingsStore.SettingsChanged -= OnExternalSettingsChanged;
        Close();
    }

    private void OnToggleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.ToggleEnabledCommand.Execute(null);
        e.Handled = true;
    }

    private void OnPresentationClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.TogglePresentationCommand.Execute(null);
        e.Handled = true;
    }

    private void OnBlurClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.ToggleBlurCommand.Execute(null);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    // --- Converters ---

    private class BoolToColorConverter : IValueConverter
    {
        private readonly Color _trueColor, _falseColor;
        public BoolToColorConverter(Color trueColor, Color falseColor) { _trueColor = trueColor; _falseColor = falseColor; }
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? _trueColor : _falseColor;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    private class BoolToStringConverter : IValueConverter
    {
        private readonly string _trueStr, _falseStr;
        public BoolToStringConverter(string trueStr, string falseStr) { _trueStr = trueStr; _falseStr = falseStr; }
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? _trueStr : _falseStr;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }

    private class BoolToDoubleConverter : IValueConverter
    {
        private readonly double _trueVal, _falseVal;
        public BoolToDoubleConverter(double trueVal, double falseVal) { _trueVal = trueVal; _falseVal = falseVal; }
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? _trueVal : _falseVal;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }
}
