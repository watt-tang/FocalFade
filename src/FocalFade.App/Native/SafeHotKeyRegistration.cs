namespace FocalFade.Native;

public sealed class SafeHotKeyRegistration : IDisposable
{
    private readonly IntPtr _hWnd;
    private readonly int _id;
    private bool _disposed;

    public SafeHotKeyRegistration(IntPtr hWnd, int id)
    {
        _hWnd = hWnd;
        _id = id;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            User32.UnregisterHotKey(_hWnd, _id);
            _disposed = true;
        }
    }
}
