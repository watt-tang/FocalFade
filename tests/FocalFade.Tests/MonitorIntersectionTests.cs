using FluentAssertions;
using FocalFade.Overlay;
using System.Windows;

namespace FocalFade.Tests;

public class MonitorIntersectionTests
{
    [Fact]
    public void SingleMonitor_FullyInside_ReturnsRect()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var rects = new List<Rect> { new(100, 100, 400, 300) };

        var result = OverlayGeometryService.GetIntersectionsWithMonitor(monitor, rects);

        result.Should().HaveCount(1);
        result[0].Should().Be(new Rect(100, 100, 400, 300));
    }

    [Fact]
    public void TwoMonitorsSideBySide_FiltersCorrectly()
    {
        var monitor1 = new Rect(0, 0, 1920, 1080);
        var monitor2 = new Rect(1920, 0, 1920, 1080);

        var rects = new List<Rect>
        {
            new(100, 100, 400, 300),    // Monitor 1
            new(2000, 100, 400, 300),   // Monitor 2
        };

        var result1 = OverlayGeometryService.GetIntersectionsWithMonitor(monitor1, rects);
        var result2 = OverlayGeometryService.GetIntersectionsWithMonitor(monitor2, rects);

        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
    }

    [Fact]
    public void NegativeX_Monitor_HandledCorrectly()
    {
        var monitor = new Rect(-1920, 0, 1920, 1080);
        var rects = new List<Rect> { new(-1500, 100, 400, 300) };

        var result = OverlayGeometryService.GetIntersectionsWithMonitor(monitor, rects);

        result.Should().HaveCount(1);
        result[0].Should().Be(new Rect(-1500, 100, 400, 300));
    }

    [Fact]
    public void WindowSpanningTwoMonitors_Clipped()
    {
        var monitor1 = new Rect(0, 0, 1920, 1080);
        var monitor2 = new Rect(1920, 0, 1920, 1080);

        // Window spans both monitors
        var window = new Rect(1800, 100, 400, 300);

        var result1 = OverlayGeometryService.GetIntersectionsWithMonitor(monitor1, [window]);
        var result2 = OverlayGeometryService.GetIntersectionsWithMonitor(monitor2, [window]);

        result1.Should().HaveCount(1);
        result1[0].X.Should().Be(1800);
        result1[0].Width.Should().Be(120); // 1920 - 1800

        result2.Should().HaveCount(1);
        result2[0].X.Should().Be(1920);
        result2[0].Width.Should().Be(280); // 400 - 120
    }

    [Fact]
    public void NoIntersection_EmptyResult()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var rects = new List<Rect> { new(2000, 2000, 400, 300) };

        var result = OverlayGeometryService.GetIntersectionsWithMonitor(monitor, rects);

        result.Should().BeEmpty();
    }

    [Fact]
    public void HighDpi_ConversionSanityCheck()
    {
        // At 150% DPI, physical pixels are 1.5x DIPs
        double dpiScale = 1.5;
        var physicalBounds = new Rect(0, 0, 2880, 1620);
        var dipBounds = new Rect(
            physicalBounds.X / dpiScale,
            physicalBounds.Y / dpiScale,
            physicalBounds.Width / dpiScale,
            physicalBounds.Height / dpiScale);

        dipBounds.Width.Should().Be(1920);
        dipBounds.Height.Should().Be(1080);
    }
}
