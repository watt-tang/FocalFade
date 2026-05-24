using FocalFade.Models;
using System.Windows;

namespace FocalFade.Services;

public interface IOverlayManager : IDisposable
{
    bool IsVisible { get; }
    void Show();
    void Hide();
    void UpdateFocusRects(List<Rect> focusRects, OverlayAppearance appearance);
    void UpdateAppearance(OverlayAppearance appearance);
    void RecreateOverlays();
}
