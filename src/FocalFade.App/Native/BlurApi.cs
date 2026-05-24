using System.Runtime.InteropServices;

namespace FocalFade.Native;

public static class BlurApi
{
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("user32.dll")]
    private static extern int GetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    private enum AccentState
    {
        AccentDisabled = 0,
        AccentEnableGradient = 1,
        AccentEnableTransparentGradient = 2,
        AccentEnableBlurBehind = 3,
        AccentEnableAcrylicBlurBehind = 4,
        AccentInvalidState = 5,
    }

    // Window composition attribute constants
    private const int WCA_ACCENT_POLICY = 19;

    /// <summary>
    /// Enable blur behind on a window using SetWindowCompositionAttribute.
    /// Returns true if successful.
    /// </summary>
    public static bool EnableBlurBehind(IntPtr hwnd, double intensity)
    {
        try
        {
            // Calculate gradient color: ARGB format
            // intensity 0.0 = nearly transparent, 1.0 = very blurred
            byte alpha = (byte)(intensity * 200); // Max alpha ~200 to stay translucent
            int gradientColor = (alpha << 24) | (0 << 16) | (0 << 8) | 0; // ARGB with black

            var accent = new AccentPolicy
            {
                AccentState = AccentState.AccentEnableAcrylicBlurBehind,
                AccentFlags = 2, // Enable color tint
                GradientColor = gradientColor,
            };

            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                int result = SetWindowCompositionAttribute(hwnd, ref data);
                return result != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Try standard blur behind (non-acrylic, more compatible).
    /// </summary>
    public static bool EnableBlurBehindFallback(IntPtr hwnd)
    {
        try
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.AccentEnableBlurBehind,
                AccentFlags = 0,
                GradientColor = 0x01000000, // Nearly transparent tint
            };

            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                int result = SetWindowCompositionAttribute(hwnd, ref data);
                return result != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Disable blur (restore to normal).
    /// </summary>
    public static bool DisableBlur(IntPtr hwnd)
    {
        try
        {
            var accent = new AccentPolicy
            {
                AccentState = AccentState.AccentDisabled,
            };

            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                int result = SetWindowCompositionAttribute(hwnd, ref data);
                return result != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
        catch
        {
            return false;
        }
    }
}
