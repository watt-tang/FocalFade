using FocalFade.Models;
using FocalFade.Native;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace FocalFade.Overlay;

public partial class FocusOverlayWindow : Window
{
    private IntPtr _hwnd;
    private bool _isSetup;
    private MonitorInfo? _monitor;

    // Cached state to avoid unnecessary redraws
    private List<Rect>? _lastLocalRects;
    private OverlayAppearance? _lastAppearance;

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
        int exStyle = (int)User32.GetWindowExStyle(_hwnd);
        exStyle |= (int)(NativeConstants.WS_EX_TRANSPARENT
            | NativeConstants.WS_EX_LAYERED
            | NativeConstants.WS_EX_TOOLWINDOW
            | NativeConstants.WS_EX_NOACTIVATE);
        User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, new IntPtr(exStyle));

        // Set topmost
        User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST, 0, 0, 0, 0,
            NativeConstants.SWP_NOMOVE | NativeConstants.SWP_NOSIZE | NativeConstants.SWP_NOACTIVATE);

        // Position if monitor was already set
        if (_monitor != null)
            ApplyPosition();
    }

    public void PositionOnMonitor(MonitorInfo monitor)
    {
        _monitor = monitor;

        // Set WPF dimensions in DIPs for the WPF layout system
        Left = monitor.DipBounds.X;
        Top = monitor.DipBounds.Y;
        Width = monitor.DipBounds.Width;
        Height = monitor.DipBounds.Height;

        // Position with Win32 SetWindowPos using physical pixels
        if (_hwnd != IntPtr.Zero)
            ApplyPosition();
    }

    private void ApplyPosition()
    {
        if (_monitor == null || _hwnd == IntPtr.Zero) return;

        var pb = _monitor.PhysicalBounds;
        User32.SetWindowPos(_hwnd, NativeConstants.HWND_TOPMOST,
            (int)pb.X, (int)pb.Y, (int)pb.Width, (int)pb.Height,
            NativeConstants.SWP_NOACTIVATE | NativeConstants.SWP_SHOWWINDOW);
    }

    public void UpdateOverlay(List<Rect> focusRectsInDip, OverlayAppearance appearance, MonitorInfo monitor)
    {
        _monitor = monitor;

        // Convert DIP focus rects to overlay-local DIP coordinates
        // monitor.DipBounds is the monitor's position in DIP virtual screen space
        // Local coords = focusRect - monitor.DipBounds origin
        var localRects = focusRectsInDip.Select(r => new Rect(
            r.X - monitor.DipBounds.X,
            r.Y - monitor.DipBounds.Y,
            r.Width,
            r.Height)).ToList();

        // Skip redraw if nothing changed
        if (_lastLocalRects != null && _lastAppearance != null &&
            AreRectsEqual(_lastLocalRects, localRects) && _lastAppearance.Equals(appearance))
            return;

        _lastLocalRects = localRects;
        _lastAppearance = appearance;

        // Overlay canvas size in DIPs
        var overlayBounds = new Rect(0, 0, Width, Height);

        // Create dimming geometry with EvenOdd fill rule
        var geometry = OverlayGeometryService.CreateDimmingGeometry(
            overlayBounds, localRects, appearance.FocusMargin, appearance.CornerRadius);

        DimmingPath.Data = geometry;
        DimmingPath.Fill = new SolidColorBrush(appearance.DimColor);
        DimmingPath.Opacity = appearance.Opacity;

        // Update border
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

    public void ShowImmediate()
    {
        if (!IsVisible)
            Show();
        Opacity = 1.0;
    }

    public void HideImmediate()
    {
        if (IsVisible)
            Hide();
    }

    public void ShowWithAnimation(int fadeDurationMs)
    {
        if (!IsVisible)
        {
            Show();
            if (fadeDurationMs > 0)
                OverlayAnimationService.AnimateOpacity(this, 0, 1, fadeDurationMs);
            else
                Opacity = 1.0;
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

        int exStyle = (int)User32.GetWindowExStyle(_hwnd);
        if (clickThrough)
            exStyle |= (int)NativeConstants.WS_EX_TRANSPARENT;
        else
            exStyle &= ~(int)NativeConstants.WS_EX_TRANSPARENT;

        User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, new IntPtr(exStyle));
    }

    private static bool AreRectsEqual(List<Rect> a, List<Rect> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (Math.Abs(a[i].X - b[i].X) > 0.5 || Math.Abs(a[i].Y - b[i].Y) > 0.5 ||
                Math.Abs(a[i].Width - b[i].Width) > 0.5 || Math.Abs(a[i].Height - b[i].Height) > 0.5)
                return false;
        }
        return true;
    }
}
