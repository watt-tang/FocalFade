using System.Runtime.InteropServices;

namespace FocalFade.Native;

public static class DwmApi
{
    private const string DllName = "dwmapi.dll";

    [DllImport(DllName)]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport(DllName)]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport(DllName)]
    public static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled);

    public static bool TryGetExtendedFrameBounds(IntPtr hwnd, out RECT bounds)
    {
        bounds = default;
        try
        {
            int hr = DwmGetWindowAttribute(hwnd, NativeConstants.DWMWA_EXTENDED_FRAME_BOUNDS, out bounds, Marshal.SizeOf<RECT>());
            return hr == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsCloaked(IntPtr hwnd)
    {
        try
        {
            int hr = DwmGetWindowAttribute(hwnd, NativeConstants.DWMWA_CLOAKED, out int cloaked, sizeof(int));
            return hr == 0 && cloaked != 0;
        }
        catch
        {
            return false;
        }
    }
}
