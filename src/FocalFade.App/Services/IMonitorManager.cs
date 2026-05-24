using FocalFade.Models;

namespace FocalFade.Services;

public interface IMonitorManager : IDisposable
{
    IReadOnlyList<MonitorInfo> Monitors { get; }
    void Refresh();
    MonitorInfo? GetMonitorContaining(System.Windows.Rect bounds);
    MonitorInfo? GetMonitorContainingPhysical(System.Windows.Rect physicalBounds);
    MonitorInfo? GetMonitorFromPoint(System.Windows.Point point);
    MonitorInfo? GetMonitorFromPhysicalPoint(int x, int y);
}
