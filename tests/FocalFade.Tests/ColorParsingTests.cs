using FluentAssertions;
using FocalFade.Core;
using System.Windows.Media;

namespace FocalFade.Tests;

public class ColorParsingTests
{
    [Theory]
    [InlineData("#000000", 255, 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255, 255)]
    [InlineData("#FF0000", 255, 255, 0, 0)]
    [InlineData("#00FF00", 255, 0, 255, 0)]
    [InlineData("#0000FF", 255, 0, 0, 255)]
    [InlineData("#112233", 255, 0x11, 0x22, 0x33)]
    public void ParseHex6_ParsesCorrectly(string hex, byte a, byte r, byte g, byte b)
    {
        ColorService.TryParseHex(hex, out var color).Should().BeTrue();
        color.A.Should().Be(a);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }

    [Theory]
    [InlineData("#FFF", 255, 255, 255, 255)]
    [InlineData("#000", 255, 0, 0, 0)]
    [InlineData("#F00", 255, 255, 0, 0)]
    public void ParseHex3_ExpandsToFull(string hex, byte a, byte r, byte g, byte b)
    {
        ColorService.TryParseHex(hex, out var color).Should().BeTrue();
        color.A.Should().Be(a);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }

    [Theory]
    [InlineData("#80112233", 128, 0x11, 0x22, 0x33)]
    [InlineData("#AA112233", 170, 0x11, 0x22, 0x33)]
    public void ParseHex8_WithAlpha(string hex, byte a, byte r, byte g, byte b)
    {
        ColorService.TryParseHex(hex, out var color).Should().BeTrue();
        color.A.Should().Be(a);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    [InlineData("#GGG")]
    [InlineData("#12345")]
    public void ParseHex_Invalid_ReturnsFalse(string hex)
    {
        ColorService.TryParseHex(hex, out _).Should().BeFalse();
    }

    [Fact]
    public void ToHex_RoundTrips()
    {
        var color = Color.FromArgb(0xFF, 0x11, 0x22, 0x33);
        var hex = ColorService.ToHex(color);
        hex.Should().Be("#FF112233");
    }

    [Fact]
    public void ToHexRgb_OmitsAlpha()
    {
        var color = Color.FromArgb(0xFF, 0x11, 0x22, 0x33);
        var hex = ColorService.ToHexRgb(color);
        hex.Should().Be("#112233");
    }

    [Fact]
    public void Presets_AreNonEmpty()
    {
        ColorService.Presets.Should().NotBeEmpty();
        foreach (var (name, hex) in ColorService.Presets)
        {
            name.Should().NotBeNullOrWhiteSpace();
            ColorService.TryParseHex(hex, out _).Should().BeTrue($"preset '{name}' hex '{hex}' should be valid");
        }
    }
}
