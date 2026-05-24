using System.Runtime.InteropServices;

namespace FocalFade.Native;

public static class ShCore
{
    private const string DllName = "shcore.dll";

    public enum PROCESS_DPI_AWARENESS
    {
        PROCESS_DPI_UNAWARE = 0,
        PROCESS_SYSTEM_DPI_AWARE = 1,
        PROCESS_PER_MONITOR_DPI_AWARE = 2
    }

    public enum MONITOR_DPI_TYPE
    {
        MDT_EFFECTIVE_DPI = 0,
    }

    [DllImport(DllName)]
    public static extern int GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS value);

    [DllImport(DllName)]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    [DllImport(DllName)]
    public static extern int GetDpiForSystem();
}
