using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;

namespace FocalFade.Core;

public sealed class ActiveWindowTracker : IActiveWindowTracker
{
    private readonly ILogger<ActiveWindowTracker> _logger;
    private readonly WindowInfoProvider _windowInfoProvider;
    private readonly Debouncer _debouncer;
    private readonly Dispatcher _dispatcher;

    // Keep delegates alive to prevent GC collection
    private User32.WinEventDelegate? _winEventDelegate;
    private IntPtr _foregroundHook = IntPtr.Zero;
    private DispatcherTimer? _fallbackTimer;
    private bool _started;
    private bool _disposed;

    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow", "DV2ControlHost",
        "TaskListThumbnailWnd", "Windows.UI.Core.CoreWindow"
    };

    public ActiveWindowTracker(ILogger<ActiveWindowTracker> logger, WindowInfoProvider windowInfoProvider)
    {
        _logger = logger;
        _windowInfoProvider = windowInfoProvider;
        _debouncer = new Debouncer(40);
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public event EventHandler<WindowInfo>? ForegroundChanged;
    public WindowInfo? CurrentWindow { get; private set; }

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;

        try
        {
            // Keep the delegate reference alive
            _winEventDelegate = OnWinEvent;

            _foregroundHook = User32.SetWinEventHook(
                NativeConstants.EVENT_SYSTEM_FOREGROUND,
                NativeConstants.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _winEventDelegate,
                0, 0,
                NativeConstants.WINEVENT_OUTOFCONTEXT | NativeConstants.WINEVENT_SKIPOWNPROCESS);

            if (_foregroundHook == IntPtr.Zero)
            {
                _logger.LogWarning("Failed to set WinEventHook for foreground changes, using timer fallback only");
            }
            else
            {
                _logger.LogInformation("WinEventHook registered for foreground changes");
            }

            // Fallback timer to catch missed events
            _fallbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _fallbackTimer.Tick += (_, _) => CheckForegroundWindow();
            _fallbackTimer.Start();

            // Initial check
            CheckForegroundWindow();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start ActiveWindowTracker");
        }
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;

        _fallbackTimer?.Stop();
        _fallbackTimer = null;

        if (_foregroundHook != IntPtr.Zero)
        {
            User32.UnhookWinEvent(_foregroundHook);
            _foregroundHook = IntPtr.Zero;
        }
    }

    public List<WindowInfo> GetVisibleWindowsForProcess(int processId)
    {
        return _windowInfoProvider.GetVisibleWindowsForProcess(processId);
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (eventType != NativeConstants.EVENT_SYSTEM_FOREGROUND) return;
        if (hwnd == IntPtr.Zero) return;

        // Debounce rapid changes
        _debouncer.Debounce(() => _dispatcher.BeginInvoke(() => ProcessForegroundChange(hwnd)));
    }

    private void CheckForegroundWindow()
    {
        try
        {
            IntPtr hwnd = User32.GetForegroundWindow();
            if (hwnd != IntPtr.Zero)
                ProcessForegroundChange(hwnd);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking foreground window");
        }
    }

    private void ProcessForegroundChange(IntPtr hwnd)
    {
        try
        {
            if (ShouldIgnoreWindow(hwnd))
            {
                return;
            }

            var windowInfo = _windowInfoProvider.GetWindowInfo(hwnd);
            if (windowInfo == null || windowInfo.IsOwnProcess)
            {
                return;
            }

            CurrentWindow = windowInfo;
            ForegroundChanged?.Invoke(this, windowInfo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing foreground change");
        }
    }

    private bool ShouldIgnoreWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return true;

        string className = User32.GetClassName(hwnd);
        if (IgnoredClasses.Contains(className)) return true;

        // Skip own process
        User32.GetWindowThreadProcessId(hwnd, out int processId);
        if (processId == Environment.ProcessId) return true;

        // Skip invisible
        if (!User32.IsWindowVisible(hwnd)) return true;

        // Skip minimized
        if (User32.IsIconic(hwnd)) return true;

        // Skip cloaked
        if (DwmApi.IsCloaked(hwnd)) return true;

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _debouncer.Dispose();
    }
}
