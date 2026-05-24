using FocalFade.Models;
using FocalFade.Native;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FocalFade.Overlay;

public partial class FocusOverlayWindow : Window
{
    private readonly OverlayState _state = new();
    private IntPtr _hwnd;
    private bool _isSetup;

    public FocusOverlayWindow()
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
        int exStyle = (int)User32.SafeGetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE);
        exStyle |= (int)(NativeConstants.WS_EX_TRANSPARENT
            | NativeConstants.WS_EX_LAYERED
            | NativeConstants.WS_EX_TOOLWINDOW
            | NativeConstants.WS_EX_NOACTIVATE);
        User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, new IntPtr(exStyle));

        // Set topmost
        User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST, 0, 0, 0, 0,
            NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOACTIVATE);
    }

    public void PositionOnMonitor(MonitorInfo monitor)
    {
        var bounds = monitor.DipBounds;
        Left = bounds.X;
        Top = bounds.Y;
        Width = bounds.Width;
        Height = bounds.Height;

        if (_hwnd != IntPtr.Zero)
        {
            User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST,
                (int)monitor.PhysicalBounds.X, (int)monitor.PhysicalBounds.Y,
                (int)monitor.PhysicalBounds.Width, (int)monitor.PhysicalBounds.Height,
                NativeConstants.SWP_NOACTIVATE | NativeConstants.SWP_SHOWWINDOW);
        }
    }

    public void UpdateOverlay(List<Rect> focusRects, OverlayAppearance appearance, MonitorInfo monitor)
    {
        _state.FocusRects = focusRects;
        _state.Appearance = appearance;
        _state.LastUpdate = DateTime.Now;

        var monitorBounds = new Rect(0, 0, Width, Height);

        // Transform focus rects to local coordinates
        var localRects = focusRects.Select(r => new Rect(
            r.X - monitor.DipBounds.X,
            r.Y - monitor.DipBounds.Y,
            r.Width,
            r.Height)).ToList();

        // Create dimming geometry
        var geometry = OverlayGeometryService.CreateDimmingGeometry(
            monitorBounds, localRects, appearance.FocusMargin, appearance.CornerRadius);

        DimmingPath.Data = geometry;
        DimmingPath.Fill = new SolidColorBrush(appearance.DimColor);
        DimmingPath.Opacity = appearance.Opacity;

        // Update border if enabled
        if (appearance.ShowBorder && localRects.Count > 0)
        {
            BorderPath.Visibility = Visibility.Visible;
            var borderGeometry = new GeometryGroup();
            foreach (var rect in localRects)
            {
                var borderRect = new Rect(
                    rect.X - appearance.FocusMargin,
                    rect.Y - appearance.FocusMargin,
                    rect.Width + appearance.FocusMargin * 2,
                    rect.Height + appearance.FocusMargin * 2);
                borderGeometry.Children.Add(new RectangleGeometry(borderRect, appearance.CornerRadius, appearance.CornerRadius));
            }
            BorderPath.Data = borderGeometry;
            BorderPath.Stroke = new SolidColorBrush(appearance.BorderColor);
            BorderPath.StrokeThickness = appearance.BorderThickness;
        }
        else
        {
            BorderPath.Visibility = Visibility.Collapsed;
        }
    }

    public void ShowWithAnimation(int fadeDurationMs)
    {
        if (!IsVisible)
        {
            Show();
            if (fadeDurationMs > 0)
            {
                OverlayAnimationService.AnimateOpacity(this, 0, 1, fadeDurationMs);
            }
        }
    }

    public void HideWithAnimation(int fadeDurationMs, Action? onComplete = null)
    {
        if (!IsVisible) { onComplete?.Invoke(); return; }

        if (fadeDurationMs > 0)
        {
            OverlayAnimationService.AnimateOpacity(this, Opacity, 0, fadeDurationMs, () =>
            {
                Hide();
                onComplete?.Invoke();
            });
        }
        else
        {
            Hide();
            onComplete?.Invoke();
        }
    }

    public void SetClickThrough(bool clickThrough)
    {
        if (_hwnd == IntPtr.Zero) return;

        int exStyle = (int)User32.SafeGetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE);
        if (clickThrough)
            exStyle |= (int)NativeConstants.WS_EX_TRANSPARENT;
        else
            exStyle &= ~(int)NativeConstants.WS_EX_TRANSPARENT;

        User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, new IntPtr(exStyle));
    }
}
