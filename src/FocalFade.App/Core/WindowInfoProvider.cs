using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;

namespace FocalFade.Core;

public sealed class WindowInfoProvider
{
    private readonly ILogger<WindowInfoProvider> _logger;
    private readonly IMonitorManager _monitorManager;
    private readonly HashSet<string> _shellClasses;
    private readonly int _ownProcessId;
    private readonly bool _includeWindowTitles;

    private static readonly HashSet<string> DefaultShellClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
        "NotifyIconOverflowWindow", "DV2ControlHost",
        "Windows.UI.Core.CoreWindow", "Shell_CharmWindow",
        "ImmersiveLauncher", "ImmersiveSwitchListPaneWindow",
        "MultitaskingViewFrame", "ForegroundStaging"
    };

    public WindowInfoProvider(ILogger<WindowInfoProvider> logger, IMonitorManager monitorManager, bool includeWindowTitles = false)
    {
        _logger = logger;
        _monitorManager = monitorManager;
        _shellClasses = new HashSet<string>(DefaultShellClasses, StringComparer.OrdinalIgnoreCase);
        _ownProcessId = Environment.ProcessId;
        _includeWindowTitles = includeWindowTitles;
    }

    public WindowInfo? GetWindowInfo(IntPtr hwnd, bool skipTitle = false)
    {
        if (hwnd == IntPtr.Zero) return null;

        try
        {
            // Check visibility
            if (!User32.IsWindowVisible(hwnd)) return null;

            // Check minimized
            bool isMinimized = User32.IsIconic(hwnd);
            if (isMinimized) return null;

            // Check cloaked
            bool isCloaked = DwmApi.IsCloaked(hwnd);
            if (isCloaked) return null;

            // Get class name
            string className = User32.GetClassName(hwnd);
            if (_shellClasses.Contains(className)) return null;

            // Get process info
            User32.GetWindowThreadProcessId(hwnd, out int processId);
            string processName = string.Empty;
            try
            {
                using var process = Process.GetProcessById(processId);
                processName = process.ProcessName;
            }
            catch { }

            // Skip own process windows (overlay, settings, etc.)
            bool isOwnProcess = processId == _ownProcessId;

            // Get window bounds - prefer DWM extended frame bounds
            RECT bounds;
            if (!DwmApi.TryGetExtendedFrameBounds(hwnd, out bounds))
            {
                User32.GetWindowRect(hwnd, out bounds);
            }

            var physicalBounds = bounds.ToRect();

            // Skip windows with empty or tiny bounds
            if (physicalBounds.Width < 4 || physicalBounds.Height < 4)
                return null;

            // Get monitor for DPI scaling
            var monitor = _monitorManager.GetMonitorContaining(
                new Rect(physicalBounds.X, physicalBounds.Y, physicalBounds.Width, physicalBounds.Height));
            double dpiScale = monitor?.DpiScaleX ?? DpiCoordinator.GetSystemDpiScale();

            var dipBounds = DpiCoordinator.PhysicalToDip(physicalBounds, dpiScale, dpiScale);

            // Get title only if allowed
            string title = (_includeWindowTitles && !skipTitle) ? User32.GetWindowText(hwnd) : string.Empty;

            // Check extended style for tool windows (often transient)
            var exStyle = (uint)User32.SafeGetWindowLongPtr(hwnd, NativeConstants.GWL_EXSTYLE).ToInt64();
            bool isToolWindow = (exStyle & NativeConstants.WS_EX_TOOLWINDOW) != 0;

            // Check if fullscreen
            bool isFullscreen = false;
            if (monitor != null)
            {
                isFullscreen = FullscreenDetector.IsFullscreen(new WindowInfo
                {
                    PhysicalBounds = physicalBounds,
                    IsVisible = true,
                    IsMinimized = false
                }, monitor);
            }

            return new WindowInfo
            {
                Hwnd = hwnd,
                ProcessId = processId,
                ProcessName = processName,
                Title = title,
                ClassName = className,
                PhysicalBounds = physicalBounds,
                DipBounds = new Rect(dipBounds.X, dipBounds.Y, dipBounds.Width, dipBounds.Height),
                IsVisible = true,
                IsMinimized = false,
                IsCloaked = false,
                IsFullscreen = isFullscreen,
                IsOwnProcess = isOwnProcess,
                CapturedAt = DateTimeOffset.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get window info for hwnd {Hwnd}", hwnd);
            return null;
        }
    }

    public List<WindowInfo> GetVisibleTopLevelWindows()
    {
        var windows = new List<WindowInfo>();

        User32.EnumWindows((hWnd, _) =>
        {
            var info = GetWindowInfo(hWnd);
            if (info != null && !info.IsOwnProcess)
                windows.Add(info);
            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public List<WindowInfo> GetVisibleWindowsForProcess(int processId)
    {
        var windows = new List<WindowInfo>();

        User32.EnumWindows((hWnd, _) =>
        {
            User32.GetWindowThreadProcessId(hWnd, out int pid);
            if (pid != processId) return true;

            var info = GetWindowInfo(hWnd);
            if (info != null)
                windows.Add(info);
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
