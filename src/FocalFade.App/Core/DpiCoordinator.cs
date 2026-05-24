using FocalFade.Native;
using System.Windows;

namespace FocalFade.Core;

public static class DpiCoordinator
{
    public static double GetDpiScaleForMonitor(IntPtr hMonitor)
    {
        try
        {
            int hr = ShCore.GetDpiForMonitor(hMonitor, ShCore.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
            if (hr == 0 && dpiX > 0)
                return dpiX / 96.0;
        }
        catch { }
        return 1.0;
    }

    public static double GetSystemDpiScale()
    {
        try
        {
            int dpi = ShCore.GetDpiForSystem();
            return dpi > 0 ? dpi / 96.0 : 1.0;
        }
        catch
        {
            return 1.0;
        }
    }

    public static Rect PhysicalToDip(Rect physicalBounds, double dpiScaleX, double dpiScaleY)
    {
        return new Rect(
            physicalBounds.X / dpiScaleX,
            physicalBounds.Y / dpiScaleY,
            physicalBounds.Width / dpiScaleX,
            physicalBounds.Height / dpiScaleY);
    }

    public static Rect DipToPhysical(Rect dipBounds, double dpiScaleX, double dpiScaleY)
    {
        return new Rect(
            dipBounds.X * dpiScaleX,
            dipBounds.Y * dpiScaleY,
            dipBounds.Width * dpiScaleX,
            dipBounds.Height * dpiScaleY);
    }

    public static double PhysicalToDip(double physicalValue, double dpiScale)
    {
        return physicalValue / dpiScale;
    }

    public static double DipToPhysical(double dipValue, double dpiScale)
    {
        return dipValue * dpiScale;
    }
}
