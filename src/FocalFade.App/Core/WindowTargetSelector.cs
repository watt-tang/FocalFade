using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FocalFade.Core;

public enum TargetDecision
{
    Accepted,
    Rejected,
    FallbackToLastValid,
    HideOverlay
}

public sealed record TargetResult(TargetDecision Decision, WindowInfo? Window, string Reason);

public sealed class WindowTargetSelector
{
    private readonly ILogger<WindowTargetSelector> _logger;
    private readonly IMonitorManager _monitorManager;
    private readonly int _ownProcessId;
    private readonly bool _includeWindowTitles;

    // Grace period for last valid target
    private WindowInfo? _lastValidTarget;
    private DateTime _lastValidTargetTime;
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMilliseconds(1500);

    // Known shell/non-work class names to reject
    private static readonly HashSet<string> RejectClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman",
        "WorkerW",
        "SHELLDLL_DefView",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow",
        "DV2ControlHost",
        "tooltips_class32",
        "TaskListThumbnailWnd",
        "ForegroundStaging",
        "Xaml_WindowedPopupClass",
        "OperationStatusWindow",
        "Windows.UI.Core.CoreWindow",
        "Shell_CharmWindow",
        "ImmersiveLauncher",
        "ImmersiveSwitchListPaneWindow",
        "MultitaskingViewFrame",
        "WindowsInternal.ComposableShell.Experiences.TextInput.InputSite.WindowClass",
    };

    // Classes that are menu/popup/transient
    private static readonly HashSet<string> TransientClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "#32768", // Menu
        "#32769", // Desktop
        "#32770", // Dialog (sometimes transient)
        " tooltips_class32",
    };

    // Known explorer classes that ARE valid (folder windows)
    private static readonly HashSet<string> ValidExplorerClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "CabinetWClass",
        "ExploreWClass",
    };

    public WindowTargetSelector(ILogger<WindowTargetSelector> logger, IMonitorManager monitorManager, bool includeWindowTitles = false)
    {
        _logger = logger;
        _monitorManager = monitorManager;
        _ownProcessId = Environment.ProcessId;
        _includeWindowTitles = includeWindowTitles;
    }

    public TargetResult Evaluate(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return new TargetResult(TargetDecision.Rejected, null, "hwnd is zero");

        // 1. Normalize to top-level root
        IntPtr rootHwnd = User32.GetAncestor(hwnd, NativeConstants.GA_ROOT);
        if (rootHwnd == IntPtr.Zero) rootHwnd = hwnd;

        // 2. Get basic info
        uint style = User32.GetWindowStyle(rootHwnd);
        uint exStyle = User32.GetWindowExStyle(rootHwnd);
        string className = User32.GetClassName(rootHwnd);

        // 3. Get process info
        User32.GetWindowThreadProcessId(rootHwnd, out int processId);
        string processName = GetProcessName(processId);

        // 4. Own process check
        if (processId == _ownProcessId)
            return new TargetResult(TargetDecision.Rejected, null, "own process");

        // 5. Visibility check
        if (!User32.IsWindowVisible(rootHwnd))
            return new TargetResult(TargetDecision.Rejected, null, "not visible");

        // 6. Minimized check
        if (User32.IsIconic(rootHwnd))
            return new TargetResult(TargetDecision.Rejected, null, "minimized");

        // 7. DWM cloaked check
        if (DwmApi.IsCloaked(rootHwnd))
            return new TargetResult(TargetDecision.Rejected, null, "DWM cloaked");

        // 8. WS_CHILD check
        if ((style & NativeConstants.WS_CHILD) != 0)
            return new TargetResult(TargetDecision.Rejected, null, "has WS_CHILD");

        // 9. Class-based rejection
        if (RejectClasses.Contains(className))
            return HandleRejectedClass(className, processName);

        // 10. Desktop SysListView32/SHELLDLL_DefView check (explorer.exe desktop icons)
        if (IsDesktopIconSurface(rootHwnd, className, processName))
            return new TargetResult(TargetDecision.Rejected, null, "desktop icon surface");

        // 11. Transient class check
        if (TransientClasses.Contains(className))
            return new TargetResult(TargetDecision.Rejected, null, $"transient class: {className}");

        // 12. WS_EX_TOOLWINDOW check (but allow known dialogs)
        if ((exStyle & NativeConstants.WS_EX_TOOLWINDOW) != 0)
        {
            // Allow tool windows that have WS_EX_APPWINDOW (they want to be in taskbar)
            if ((exStyle & NativeConstants.WS_EX_APPWINDOW) == 0)
                return new TargetResult(TargetDecision.Rejected, null, "WS_EX_TOOLWINDOW without APPWINDOW");
        }

        // 13. WS_EX_NOACTIVATE + WS_EX_TRANSPARENT combo (shell overlay surfaces)
        if ((exStyle & NativeConstants.WS_EX_NOACTIVATE) != 0 && (exStyle & NativeConstants.WS_EX_TRANSPARENT) != 0)
            return new TargetResult(TargetDecision.Rejected, null, "NOACTIVATE + TRANSPARENT (shell overlay)");

        // 14. Get bounds
        RECT bounds;
        if (!DwmApi.TryGetExtendedFrameBounds(rootHwnd, out bounds))
            User32.GetWindowRect(rootHwnd, out bounds);

        int width = bounds.Width;
        int height = bounds.Height;

        // 15. Empty bounds check
        if (width <= 0 || height <= 0)
            return new TargetResult(TargetDecision.Rejected, null, "empty bounds");

        // 16. Tiny window check (likely a shell icon, tooltip, notification)
        if (width < NativeConstants.TinyWindowMinWidth || height < NativeConstants.TinyWindowMinHeight)
            return new TargetResult(TargetDecision.Rejected, null, $"tiny window: {width}x{height}");

        int area = width * height;
        if (area < NativeConstants.TinyWindowMinArea)
            return new TargetResult(TargetDecision.Rejected, null, $"tiny area: {area}");

        // 17. Mostly offscreen check
        if (IsMostlyOffscreen(bounds))
            return new TargetResult(TargetDecision.Rejected, null, "mostly offscreen");

        // 18. Build WindowInfo
        var physicalBounds = new System.Windows.Rect(bounds.Left, bounds.Top, width, height);
        var monitor = _monitorManager.GetMonitorContainingPhysical(physicalBounds);
        double dpiScale = monitor?.DpiScaleX ?? DpiCoordinator.GetSystemDpiScale();
        var dipBounds = DpiCoordinator.PhysicalToDip(physicalBounds, dpiScale, dpiScale);

        string title = _includeWindowTitles ? User32.GetWindowText(rootHwnd) : string.Empty;

        bool isFullscreen = monitor != null && FullscreenDetector.IsFullscreen(
            new WindowInfo { PhysicalBounds = physicalBounds, IsVisible = true, IsMinimized = false }, monitor);

        var windowInfo = new WindowInfo
        {
            Hwnd = rootHwnd,
            ProcessId = processId,
            ProcessName = processName,
            Title = title,
            ClassName = className,
            PhysicalBounds = physicalBounds,
            DipBounds = dipBounds,
            IsVisible = true,
            IsMinimized = false,
            IsCloaked = false,
            IsFullscreen = isFullscreen,
            IsOwnProcess = false,
            CapturedAt = DateTimeOffset.Now
        };

        // Update last valid target
        _lastValidTarget = windowInfo;
        _lastValidTargetTime = DateTime.UtcNow;

        return new TargetResult(TargetDecision.Accepted, windowInfo, "accepted");
    }

    public TargetResult GetTarget(IntPtr hwnd)
    {
        var result = Evaluate(hwnd);

        if (result.Decision == TargetDecision.Accepted)
            return result;

        // Try fallback to last valid target
        if (_lastValidTarget != null)
        {
            var elapsed = DateTime.UtcNow - _lastValidTargetTime;
            if (elapsed <= GracePeriod)
            {
                // Verify the last valid target is still valid
                if (IsStillValid(_lastValidTarget))
                {
                    _logger.LogDebug("Falling back to last valid target {Process} (reason: {Reason})",
                        _lastValidTarget.ProcessName, result.Reason);
                    return new TargetResult(TargetDecision.FallbackToLastValid, _lastValidTarget, result.Reason);
                }
            }
        }

        return new TargetResult(TargetDecision.HideOverlay, null, result.Reason);
    }

    public void InvalidateLastValid()
    {
        _lastValidTarget = null;
    }

    private bool IsStillValid(WindowInfo window)
    {
        if (!User32.IsWindowVisible(window.Hwnd)) return false;
        if (User32.IsIconic(window.Hwnd)) return false;
        if (DwmApi.IsCloaked(window.Hwnd)) return false;
        return true;
    }

    private TargetResult HandleRejectedClass(string className, string processName)
    {
        // For explorer.exe, distinguish between desktop shell and folder windows
        if (string.Equals(processName, "explorer", StringComparison.OrdinalIgnoreCase))
        {
            // Desktop icon surfaces are rejected; CabinetWClass folder windows are OK
            _logger.LogDebug("Rejected explorer class {Class}", className);
        }

        return new TargetResult(TargetDecision.Rejected, null, $"rejected class: {className}");
    }

    private bool IsDesktopIconSurface(IntPtr hwnd, string className, string processName)
    {
        // Check if this is the desktop SysListView32 or SHELLDLL_DefView
        if (!string.Equals(processName, "explorer", StringComparison.OrdinalIgnoreCase))
            return false;

        // SHELLDLL_DefView is the desktop icon container
        if (string.Equals(className, "SHELLDLL_DefView", StringComparison.OrdinalIgnoreCase))
            return true;

        // SysListView32 hosted by the desktop shell
        if (string.Equals(className, "SysListView32", StringComparison.OrdinalIgnoreCase))
        {
            IntPtr parent = User32.GetParent(hwnd);
            if (parent != IntPtr.Zero)
            {
                string parentClass = User32.GetClassName(parent);
                if (string.Equals(parentClass, "SHELLDLL_DefView", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parentClass, "Progman", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parentClass, "WorkerW", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private bool IsMostlyOffscreen(RECT bounds)
    {
        // Check if window center is offscreen
        int centerX = (bounds.Left + bounds.Right) / 2;
        int centerY = (bounds.Top + bounds.Bottom) / 2;

        var monitors = _monitorManager.Monitors;
        foreach (var m in monitors)
        {
            if (m.PhysicalBounds.Contains(new System.Windows.Point(centerX, centerY)))
                return false;
        }

        return true;
    }

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }
}
