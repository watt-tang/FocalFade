using FocalFade.Models;
using FocalFade.Native;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Interop;

namespace FocalFade.Core;

public sealed class HotkeyManager : IHotkeyManager
{
    private readonly ILogger<HotkeyManager> _logger;
    private readonly List<SafeHotKeyRegistration> _registrations = [];
    private readonly Dictionary<int, int> _idToAction = [];
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _disposed;

    public HotkeyManager(ILogger<HotkeyManager> logger)
    {
        _logger = logger;
    }

    public event EventHandler<int>? HotkeyPressed;

    public bool RegisterHotkeys(Dictionary<string, string> hotkeyConfig)
    {
        UnregisterAll();

        // Create a hidden message-only window for WM_HOTKEY
        CreateMessageWindow();

        bool allSucceeded = true;
        var actionMap = new Dictionary<string, int>
        {
            ["ToggleEnabled"] = NativeConstants.HOTKEY_TOGGLE_ENABLED,
            ["IncreaseOpacity"] = NativeConstants.HOTKEY_INCREASE_OPACITY,
            ["DecreaseOpacity"] = NativeConstants.HOTKEY_DECREASE_OPACITY,
            ["PresentationMode"] = NativeConstants.HOTKEY_PRESENTATION_MODE,
            ["TemporaryPeek"] = NativeConstants.HOTKEY_TEMPORARY_PEEK
        };

        foreach (var (action, id) in actionMap)
        {
            if (!hotkeyConfig.TryGetValue(action, out var gestureStr))
                continue;

            var gesture = HotkeyGesture.FromString(gestureStr);
            if (gesture.Key == 0) continue;

            try
            {
                bool success = User32.RegisterHotKey(_hwnd, id, (uint)gesture.Modifiers, (uint)gesture.Key);
                if (success)
                {
                    _registrations.Add(new SafeHotKeyRegistration(_hwnd, id));
                    _idToAction[id] = id;
                    _logger.LogInformation("Registered hotkey {Action}: {Gesture}", action, gestureStr);
                }
                else
                {
                    _logger.LogWarning("Failed to register hotkey {Action}: {Gesture} (may be in use)", action, gestureStr);
                    allSucceeded = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception registering hotkey {Action}", action);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }

    public void UnregisterAll()
    {
        foreach (var reg in _registrations)
            reg.Dispose();
        _registrations.Clear();
        _idToAction.Clear();

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        if (_hwnd != IntPtr.Zero)
        {
            // Destroy the hidden window
            User32.SafeSetWindowLongPtr(_hwnd, NativeConstants.GWL_EXSTYLE, IntPtr.Zero);
            _hwnd = IntPtr.Zero;
        }
    }

    private void CreateMessageWindow()
    {
        if (_hwndSource != null) return;

        // Create a simple window for receiving WM_HOTKEY
        var parameters = new HwndSourceParameters("FocalFade_HotkeyReceiver")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0, // invisible
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _hwnd = _hwndSource.Handle;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeConstants.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            HotkeyPressed?.Invoke(this, id);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
    }
}
