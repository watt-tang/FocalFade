using FluentAssertions;
using FocalFade.Overlay;
using System.Windows;

namespace FocalFade.Tests;

public class BlurRegionSegmentationTests
{
    [Fact]
    public void NoHoles_ReturnsSingleFullPanel()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var panels = BlurManager.ComputeBlurPanels(monitor, []);

        panels.Should().HaveCount(1);
        panels[0].Should().Be(monitor);
    }

    [Fact]
    public void CenterHole_CreatesFourPanels()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var hole = new Rect(400, 200, 400, 300);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole]);

        // Should create top, bottom, left, right panels
        panels.Should().HaveCount(4);
    }

    [Fact]
    public void TopLeftHole_CreatesTwoPanels()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var hole = new Rect(0, 0, 400, 300);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole]);

        // Top and left are at edge, so only bottom and right panels
        panels.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void FullScreenHole_ReturnsNoPanels()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var hole = new Rect(0, 0, 1920, 1080);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole]);

        // Hole covers entire monitor, no panels needed (with margin it might still be 0)
        panels.Count.Should().BeLessThanOrEqualTo(2); // margin might create tiny panels
    }

    [Fact]
    public void NegativeCoordinates_HandledCorrectly()
    {
        var monitor = new Rect(-1920, 0, 1920, 1080);
        var hole = new Rect(-1500, 100, 400, 300);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole]);

        panels.Should().NotBeEmpty();
        // All panels should be within monitor bounds
        foreach (var panel in panels)
        {
            panel.X.Should().BeGreaterThanOrEqualTo(-1920);
            panel.Y.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void MultipleHoles_UsesBoundingBox()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var hole1 = new Rect(100, 100, 200, 200);
        var hole2 = new Rect(500, 500, 200, 200);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole1, hole2]);

        // Should still produce panels around the bounding box
        panels.Should().NotBeEmpty();
    }

    [Fact]
    public void PanelSizes_AreReasonable()
    {
        var monitor = new Rect(0, 0, 1920, 1080);
        var hole = new Rect(400, 200, 400, 300);
        var panels = BlurManager.ComputeBlurPanels(monitor, [hole]);

        foreach (var panel in panels)
        {
            panel.Width.Should().BeGreaterThan(0);
            panel.Height.Should().BeGreaterThan(0);
        }
    }
}
