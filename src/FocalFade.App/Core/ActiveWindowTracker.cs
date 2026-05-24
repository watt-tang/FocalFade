using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;

namespace FocalFade.Core;

public sealed class ActiveWindowTracker : IActiveWindowTracker
{
    private readonly ILogger<ActiveWindowTracker> _logger;
    private readonly WindowTargetSelector _targetSelector;
    private readonly Dispatcher _dispatcher;

    // Keep delegates alive to prevent GC collection
    private User32.WinEventDelegate? _winEventDelegate;
    private readonly List<IntPtr> _hooks = [];
    private DispatcherTimer? _fallbackTimer;
    private bool _started;
    private bool _disposed;

    // Drag state tracking
    private bool _isDragging;
    public event EventHandler? DragStarted;
    public event EventHandler? DragEnded;

    // Throttle for move/size updates
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private static readonly TimeSpan UpdateThrottle = TimeSpan.FromMilliseconds(33);

    public ActiveWindowTracker(ILogger<ActiveWindowTracker> logger, WindowTargetSelector targetSelector)
    {
        _logger = logger;
        _targetSelector = targetSelector;
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
            _winEventDelegate = OnWinEvent;

            // Hook foreground changes
            RegisterHook(NativeConstants.EVENT_SYSTEM_FOREGROUND, NativeConstants.EVENT_SYSTEM_FOREGROUND);
            // Hook move/size start/end
            RegisterHook(NativeConstants.EVENT_SYSTEM_MOVESIZESTART, NativeConstants.EVENT_SYSTEM_MOVESIZESTART);
            RegisterHook(NativeConstants.EVENT_SYSTEM_MOVESIZEEND, NativeConstants.EVENT_SYSTEM_MOVESIZEEND);
            // Hook location changes for current target
            RegisterHook(NativeConstants.EVENT_OBJECT_LOCATIONCHANGE, NativeConstants.EVENT_OBJECT_LOCATIONCHANGE);

            if (_hooks.Count == 0)
            {
                _logger.LogWarning("Failed to set any WinEventHooks, using timer fallback only");
            }
            else
            {
                _logger.LogInformation("WinEventHooks registered: {Count} hooks", _hooks.Count);
            }

            // Fallback timer - 500ms sanity check only
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

        foreach (var hook in _hooks)
        {
            if (hook != IntPtr.Zero)
                User32.UnhookWinEvent(hook);
        }
        _hooks.Clear();
    }

    public List<WindowInfo> GetVisibleWindowsForProcess(int processId)
    {
        // Delegate to a simple EnumWindows for active-app mode
        var windows = new List<WindowInfo>();
        User32.EnumWindows((hWnd, _) =>
        {
            User32.GetWindowThreadProcessId(hWnd, out int pid);
            if (pid != processId) return true;

            var result = _targetSelector.Evaluate(hWnd);
            if (result.Window != null)
                windows.Add(result.Window);
            return true;
        }, IntPtr.Zero);
        return windows;
    }

    private void RegisterHook(uint eventMin, uint eventMax)
    {
        try
        {
            IntPtr hook = User32.SetWinEventHook(
                eventMin, eventMax,
                IntPtr.Zero, _winEventDelegate!,
                0, 0,
                NativeConstants.WINEVENT_OUTOFCONTEXT | NativeConstants.WINEVENT_SKIPOWNPROCESS);

            if (hook != IntPtr.Zero)
                _hooks.Add(hook);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register WinEventHook for events {Min}-{Max}", eventMin, eventMax);
        }
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            switch (eventType)
            {
                case NativeConstants.EVENT_SYSTEM_FOREGROUND:
                    if (hwnd != IntPtr.Zero)
                        _dispatcher.BeginInvoke(() => ProcessForegroundChange(hwnd));
                    break;

                case NativeConstants.EVENT_SYSTEM_MOVESIZESTART:
                    if (!_isDragging && IsTargetOrRelated(hwnd))
                    {
                        _isDragging = true;
                        _logger.LogDebug("Drag/resize started");
                        _dispatcher.BeginInvoke(() => DragStarted?.Invoke(this, EventArgs.Empty));
                    }
                    break;

                case NativeConstants.EVENT_SYSTEM_MOVESIZEEND:
                    if (_isDragging)
                    {
                        _isDragging = false;
                        _logger.LogDebug("Drag/resize ended");
                        _dispatcher.BeginInvoke(() =>
                        {
                            DragEnded?.Invoke(this, EventArgs.Empty);
                            // Re-evaluate after drag
                            CheckForegroundWindow();
                        });
                    }
                    break;

                case NativeConstants.EVENT_OBJECT_LOCATIONCHANGE:
                    // Only react to location changes of the current target window
                    if (idObject == 0 && idChild == 0 && !_isDragging && IsTargetOrRelated(hwnd))
                    {
                        // Throttle updates
                        var now = DateTime.UtcNow;
                        if (now - _lastUpdateTime > UpdateThrottle)
                        {
                            _lastUpdateTime = now;
                            _dispatcher.BeginInvoke(() => ProcessLocationChange(hwnd));
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in WinEvent callback");
        }
    }

    private bool IsTargetOrRelated(IntPtr hwnd)
    {
        if (CurrentWindow == null) return false;
        if (hwnd == CurrentWindow.Hwnd) return true;
        // Check if hwnd is a child/owned by current target
        IntPtr root = User32.GetAncestor(hwnd, NativeConstants.GA_ROOT);
        return root == CurrentWindow.Hwnd;
    }

    private void ProcessForegroundChange(IntPtr hwnd)
    {
        try
        {
            var result = _targetSelector.GetTarget(hwnd);

            switch (result.Decision)
            {
                case TargetDecision.Accepted:
                case TargetDecision.FallbackToLastValid:
                    if (result.Window != null)
                    {
                        CurrentWindow = result.Window;
                        ForegroundChanged?.Invoke(this, result.Window);
                    }
                    break;

                case TargetDecision.HideOverlay:
                    CurrentWindow = null;
                    ForegroundChanged?.Invoke(this, new WindowInfo { Hwnd = IntPtr.Zero });
                    break;

                case TargetDecision.Rejected:
                    // Don't change current state on rejection
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing foreground change");
        }
    }

    private void ProcessLocationChange(IntPtr hwnd)
    {
        // Re-evaluate current target if its bounds changed
        if (CurrentWindow != null && hwnd == CurrentWindow.Hwnd)
        {
            var result = _targetSelector.Evaluate(hwnd);
            if (result.Window != null)
            {
                CurrentWindow = result.Window;
                ForegroundChanged?.Invoke(this, result.Window);
            }
        }
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
