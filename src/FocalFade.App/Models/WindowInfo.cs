using System.Windows;

namespace FocalFade.Models;

public sealed record WindowInfo
{
    public IntPtr Hwnd { get; init; }
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public Rect PhysicalBounds { get; init; }
    public Rect DipBounds { get; init; }
    public bool IsVisible { get; init; }
    public bool IsMinimized { get; init; }
    public bool IsCloaked { get; init; }
    public bool IsFullscreen { get; init; }
    public bool IsOwnProcess { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
}
