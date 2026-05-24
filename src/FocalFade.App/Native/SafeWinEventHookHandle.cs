using Microsoft.Win32.SafeHandles;

namespace FocalFade.Native;

public sealed class SafeWinEventHookHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeWinEventHookHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
        return User32.UnhookWinEvent(handle);
    }
}
