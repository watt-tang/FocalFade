using FocalFade.Models;

namespace FocalFade.Services;

public interface IMonitorManager : IDisposable
{
    IReadOnlyList<MonitorInfo> Monitors { get; }
    void Refresh();
    MonitorInfo? GetMonitorContaining(System.Windows.Rect bounds);
    MonitorInfo? GetMonitorFromPoint(System.Windows.Point point);
}
