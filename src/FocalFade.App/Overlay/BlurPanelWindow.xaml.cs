using FocalFade.Native;
using System.Windows;
using System.Windows.Interop;

namespace FocalFade.Overlay;

public partial class BlurPanelWindow : Window
{
    private IntPtr _hwnd;
    private bool _isSetup;

    public BlurPanelWindow()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupWindow();
    }

    private void SetupWindow()
    {
        if (_isSetup) return;
        _isSetup = true;

        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (hwndSource == null) return;

        _hwnd = hwndSource.Handle;

        // Set extended window style: transparent, layered, no-activate, tool window, topmost
        int exStyle = (int)User32.GetWindowExStyle(_hwnd);
        exStyle |= (int)(NativeConstants.WS_EX_TRANSPARENT
            | NativeConstants.WS_EX_LAYERED
            | NativeConstants.WS_EX_TOOLWINDOW
            | NativeConstants.WS_EX_NOACTIVATE);
        User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, new IntPtr(exStyle));

        User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST, 0, 0, 0, 0,
            NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOACTIVATE);
    }

    public void PositionAt(int physicalX, int physicalY, int physicalWidth, int physicalHeight)
    {
        Left = physicalX;
        Top = physicalY;
        Width = physicalWidth;
        Height = physicalHeight;

        if (_hwnd != IntPtr.Zero)
        {
            User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST,
                physicalX, physicalY, physicalWidth, physicalHeight,
                NativeConstants.SWP_NOACTIVATE | NativeConstants.SWP_SHOWWINDOW);
        }
    }

    public bool ApplyBlur(double intensity)
    {
        if (_hwnd == IntPtr.Zero) return false;
        return BlurApi.EnableBlurBehind(_hwnd, intensity);
    }

    public bool ApplyBlurFallback()
    {
        if (_hwnd == IntPtr.Zero) return false;
        return BlurApi.EnableBlurBehindFallback(_hwnd);
    }

    public void DisableBlur()
    {
        if (_hwnd != IntPtr.Zero)
            BlurApi.DisableBlur(_hwnd);
    }

    public void ShowImmediate()
    {
        if (!IsVisible) Show();
    }

    public void HideImmediate()
    {
        if (IsVisible) Hide();
    }
}
