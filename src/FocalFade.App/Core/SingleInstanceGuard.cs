using Microsoft.Extensions.Logging;

namespace FocalFade.Core;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly ILogger<SingleInstanceGuard> _logger;
    private Mutex? _mutex;
    private bool _disposed;

    public bool IsFirstInstance { get; private set; }

    public SingleInstanceGuard(ILogger<SingleInstanceGuard> logger)
    {
        _logger = logger;
    }

    public bool TryAcquire()
    {
        try
        {
            _mutex = new Mutex(true, "FocalFade_SingleInstance_Mutex", out bool createdNew);
            IsFirstInstance = createdNew;
            return createdNew;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create single instance mutex");
            IsFirstInstance = true; // Assume first if mutex fails
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
