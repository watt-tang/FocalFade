using FluentAssertions;
using FocalFade.Core;
using FocalFade.Models;
using System.Windows;

namespace FocalFade.Tests;

public class FullscreenDetectorTests
{
    private static MonitorInfo CreateMonitor(double x, double y, double w, double h) => new()
    {
        HMonitor = IntPtr.Zero,
        PhysicalBounds = new Rect(x, y, w, h),
        DipBounds = new Rect(x, y, w, h),
        WorkAreaPhysical = new Rect(x, y, w, h - 40), // Taskbar
        WorkAreaDip = new Rect(x, y, w, h - 40),
        IsPrimary = true,
        DpiScaleX = 1.0,
        DpiScaleY = 1.0,
        DeviceName = "Test"
    };

    [Fact]
    public void ExactMonitorCover_IsFullscreen()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(0, 0, 1920, 1080),
            IsVisible = true,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeTrue();
    }

    [Fact]
    public void NearlyFullscreen_WithinTolerance_IsFullscreen()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(1, 1, 1918, 1078), // 2px off
            IsVisible = true,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeTrue();
    }

    [Fact]
    public void NormalWindow_IsNotFullscreen()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(100, 100, 800, 600),
            IsVisible = true,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeFalse();
    }

    [Fact]
    public void InvisibleWindow_IsNotFullscreen()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(0, 0, 1920, 1080),
            IsVisible = false,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeFalse();
    }

    [Fact]
    public void MinimizedWindow_IsNotFullscreen()
    {
        var monitor = CreateMonitor(0, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(0, 0, 1920, 1080),
            IsVisible = true,
            IsMinimized = true
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeFalse();
    }

    [Fact]
    public void NegativeCoordinates_WorksCorrectly()
    {
        var monitor = CreateMonitor(-1920, 0, 1920, 1080);
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(-1920, 0, 1920, 1080),
            IsVisible = true,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreen(window, monitor).Should().BeTrue();
    }

    [Fact]
    public void IsFullscreenOnAnyMonitor_ChecksAllMonitors()
    {
        var monitors = new List<MonitorInfo>
        {
            CreateMonitor(0, 0, 1920, 1080),
            CreateMonitor(1920, 0, 1920, 1080)
        };
        var window = new WindowInfo
        {
            PhysicalBounds = new Rect(1920, 0, 1920, 1080), // Full second monitor
            IsVisible = true,
            IsMinimized = false
        };

        FullscreenDetector.IsFullscreenOnAnyMonitor(window, monitors).Should().BeTrue();
    }
}
