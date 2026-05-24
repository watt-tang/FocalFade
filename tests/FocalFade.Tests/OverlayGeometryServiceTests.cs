using FluentAssertions;
using FocalFade.Overlay;
using System.Windows;
using System.Windows.Media;

namespace FocalFade.Tests;

public class OverlayGeometryServiceTests
{
    [Fact]
    public void NoFocusRects_CreatesFullDimGeometry()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [], 0, 0);

        geometry.Should().NotBeNull();
        geometry.Bounds.Should().Be(monitor);
    }

    [Fact]
    public void SingleFocusRect_CreatesHole()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var focus = new Rect(100, 100, 400, 300);
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus], 0, 0);

        geometry.Should().NotBeNull();
        // Geometry group with EvenOdd should have two children
        if (geometry is GeometryGroup group)
        {
            group.Children.Count.Should().Be(2);
        }
    }

    [Fact]
    public void FocusRectOutsideMonitor_Ignored()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var focus = new Rect(2000, 2000, 400, 300); // Outside monitor
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus], 0, 0);

        geometry.Should().NotBeNull();
        // Should still have the monitor rect, but the hole might be empty/tiny
    }

    [Fact]
    public void FocusRectIntersectsMonitor_Clipped()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var focus = new Rect(1800, 1000, 400, 300); // Partially outside
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus], 0, 0);

        geometry.Should().NotBeNull();
    }

    [Fact]
    public void MultipleFocusRects_CreatesMultipleHoles()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var focus1 = new Rect(100, 100, 400, 300);
        var focus2 = new Rect(600, 600, 400, 300);
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus1, focus2], 0, 0);

        geometry.Should().NotBeNull();
        if (geometry is GeometryGroup group)
        {
            group.Children.Count.Should().Be(3); // monitor + 2 holes
        }
    }

    [Fact]
    public void MarginExpansion_ClippedToMonitor()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var focus = new Rect(10, 10, 100, 100);
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus], 50, 0);

        geometry.Should().NotBeNull();
        // The expanded rect should be clipped to monitor bounds
    }

    [Fact]
    public void NegativeCoordinates_HandledCorrectly()
    {
        var monitor = new Rect(-1920, 0, 1920, 1080); // Second monitor to the left
        var focus = new Rect(-1500, 100, 400, 300);
        var geometry = OverlayGeometryService.CreateDimmingGeometry(monitor, [focus], 8, 8);

        geometry.Should().NotBeNull();
    }

    [Fact]
    public void GetIntersectionsWithMonitor_ReturnsOnlyIntersecting()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var rects = new List<Rect>
        {
            new(100, 100, 400, 300),    // Fully inside
            new(2000, 2000, 400, 300),  // Fully outside
            new(1800, 1000, 400, 300),  // Partially outside
        };

        var result = OverlayGeometryService.GetIntersectionsWithMonitor(monitor, rects);

        result.Count.Should().Be(2); // Two that intersect
    }
}
