using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FocalFade.Settings;

public partial class HotkeyCaptureControl : UserControl
{
    public static readonly DependencyProperty BindingProperty =
        DependencyProperty.Register(nameof(Binding), typeof(HotkeyBindingViewModel), typeof(HotkeyCaptureControl),
            new PropertyMetadata(null, OnBindingChanged));

    public static readonly DependencyProperty AllBindingsProperty =
        DependencyProperty.Register(nameof(AllBindings), typeof(IEnumerable<HotkeyBindingViewModel>), typeof(HotkeyCaptureControl),
            new PropertyMetadata(null));

    public HotkeyBindingViewModel? Binding
    {
        get => (HotkeyBindingViewModel?)GetValue(BindingProperty);
        set => SetValue(BindingProperty, value);
    }

    public IEnumerable<HotkeyBindingViewModel>? AllBindings
    {
        get => (IEnumerable<HotkeyBindingViewModel>?)GetValue(AllBindingsProperty);
        set => SetValue(AllBindingsProperty, value);
    }

    public HotkeyCaptureControl()
    {
        InitializeComponent();
    }

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyCaptureControl control)
        {
            if (e.OldValue is HotkeyBindingViewModel oldVm)
                oldVm.PropertyChanged -= control.OnBindingPropertyChanged;
            if (e.NewValue is HotkeyBindingViewModel newVm)
                newVm.PropertyChanged += control.OnBindingPropertyChanged;
        }
    }

    private void OnBindingPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (Binding == null) return;

        if (Binding.IsCapturing)
        {
            GestureText.Visibility = Visibility.Collapsed;
            PromptText.Visibility = Visibility.Visible;
            ValidationText.Visibility = string.IsNullOrEmpty(Binding.ValidationMessage)
                ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            GestureText.Visibility = Visibility.Visible;
            PromptText.Visibility = Visibility.Collapsed;
            ValidationText.Visibility = string.IsNullOrEmpty(Binding.ValidationMessage)
                ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void OnBorderClick(object sender, MouseButtonEventArgs e)
    {
        if (Binding == null) return;

        Binding.StartCapture();
        Focus();
        Keyboard.Focus(this);
        UpdateVisualState();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Binding?.IsCapturing != true) { base.OnKeyDown(e); return; }

        var all = AllBindings?.ToList() ?? [];
        bool applied = Binding.TryApplyCapture(e, all);

        // Always consume the key while capturing
        e.Handled = true;

        if (applied)
        {
            Keyboard.ClearFocus();
        }

        UpdateVisualState();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        if (Binding?.IsCapturing == true)
        {
            Binding.CancelCapture();
            UpdateVisualState();
        }
        base.OnLostKeyboardFocus(e);
    }
}
