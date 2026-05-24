using FocalFade.Models;

namespace FocalFade.Services;

public interface IHotkeyManager : IDisposable
{
    event EventHandler<int>? HotkeyPressed;
    bool RegisterHotkeys(Dictionary<string, string> hotkeyConfig);
    void UnregisterAll();
}
