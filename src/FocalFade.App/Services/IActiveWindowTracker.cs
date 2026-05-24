using FocalFade.Models;

namespace FocalFade.Services;

public interface IActiveWindowTracker : IDisposable
{
    event EventHandler<WindowInfo>? ForegroundChanged;
    WindowInfo? CurrentWindow { get; }
    void Start();
    void Stop();
    List<WindowInfo> GetVisibleWindowsForProcess(int processId);
}
