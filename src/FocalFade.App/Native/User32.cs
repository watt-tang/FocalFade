using System.Runtime.InteropServices;
using System.Text;

namespace FocalFade.Native;

public static class User32
{
    private const string DllName = "user32.dll";

    [DllImport(DllName)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport(DllName)]
    public static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport(DllName)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsZoomed(IntPtr hWnd);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsProc lpfnEnum, IntPtr dwData);

    [DllImport(DllName, EntryPoint = "GetWindowLongPtrW")]
    public static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

    [DllImport(DllName, EntryPoint = "SetWindowLongPtrW")]
    public static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // 32-bit fallback
    [DllImport(DllName, EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLongW32(IntPtr hWnd, int nIndex);

    [DllImport(DllName, EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLongW32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport(DllName)]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport(DllName)]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport(DllName)]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport(DllName)]
    public static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);

    [DllImport(DllName)]
    public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport(DllName)]
    public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport(DllName)]
    public static extern IntPtr GetDesktopWindow();

    [DllImport(DllName)]
    public static extern IntPtr GetShellWindow();

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowInfo(IntPtr hWnd, ref WINDOWINFO pwi);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

    [DllImport(DllName)]
    public static extern IntPtr GetFocus();

    [DllImport(DllName)]
    public static extern IntPtr GetCapture();

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport(DllName)]
    public static extern IntPtr GetTopWindow(IntPtr hWnd);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    public delegate bool EnumMonitorsProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    public static string GetWindowText(IntPtr hWnd)
    {
        int length = GetWindowTextLengthW(hWnd);
        if (length == 0) return string.Empty;
        var sb = new StringBuilder(length + 1);
        GetWindowTextW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassNameW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    // Cross-architecture helper: use 64-bit if available, else 32-bit
    public static IntPtr SafeGetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        try { return GetWindowLongPtrW(hWnd, nIndex); }
        catch { return new IntPtr(GetWindowLongW32(hWnd, nIndex)); }
    }

    public static IntPtr SafeSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        try { return SetWindowLongPtrW(hWnd, nIndex, dwNewLong); }
        catch { return new IntPtr(SetWindowLongW32(hWnd, nIndex, dwNewLong.ToInt32())); }
    }

    public static uint GetWindowStyle(IntPtr hWnd)
    {
        return (uint)SafeGetWindowLongPtr(hWnd, NativeConstants.GWL_STYLE).ToInt64();
    }

    public static uint GetWindowExStyle(IntPtr hWnd)
    {
        return (uint)SafeGetWindowLongPtr(hWnd, NativeConstants.GWL_EXSTYLE).ToInt64();
    }
}
