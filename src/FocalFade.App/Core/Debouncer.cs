namespace FocalFade.Core;

public sealed class Debouncer : IDisposable
{
    private System.Threading.Timer? _timer;
    private readonly int _delayMs;

    public Debouncer(int delayMs = 30)
    {
        _delayMs = delayMs;
    }

    public void Debounce(Action action)
    {
        _timer?.Dispose();
        _timer = new System.Threading.Timer(_ => action(), null, _delayMs, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
