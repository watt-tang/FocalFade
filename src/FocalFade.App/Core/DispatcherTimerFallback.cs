using System.Windows.Threading;

namespace FocalFade.Core;

public sealed class DispatcherTimerFallback : IDisposable
{
    private DispatcherTimer? _timer;

    public event EventHandler? Tick;

    public DispatcherTimerFallback(int intervalMs = 500)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public void Start() => _timer?.Start();
    public void Stop() => _timer?.Stop();

    public void Dispose()
    {
        _timer?.Stop();
        _timer = null;
    }
}
