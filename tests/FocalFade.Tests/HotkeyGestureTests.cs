using FluentAssertions;
using FocalFade.Models;

namespace FocalFade.Tests;

public class HotkeyGestureTests
{
    [Fact]
    public void ParseSimpleCtrlAltF()
    {
        var gesture = HotkeyGesture.FromString("Ctrl+Alt+F");

        gesture.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        gesture.Key.Should().Be('F');
    }

    [Fact]
    public void ParseArrowKey()
    {
        var gesture = HotkeyGesture.FromString("Ctrl+Alt+Up");

        gesture.Modifiers.Should().Be(HotkeyModifiers.Control | HotkeyModifiers.Alt);
        gesture.Key.Should().Be((int)System.Windows.Forms.Keys.Up);
    }

    [Fact]
    public void ToString_RoundTrips()
    {
        var original = "Ctrl+Alt+P";
        var gesture = HotkeyGesture.FromString(original);

        gesture.ToString().Should().Be(original);
    }

    [Fact]
    public void ParseEmptyString_ReturnsDefault()
    {
        var gesture = HotkeyGesture.FromString("");

        gesture.Modifiers.Should().Be(HotkeyModifiers.None);
        gesture.Key.Should().Be(0);
    }

    [Fact]
    public void ParseNull_ReturnsDefault()
    {
        var gesture = HotkeyGesture.FromString(null!);

        gesture.Modifiers.Should().Be(HotkeyModifiers.None);
        gesture.Key.Should().Be(0);
    }

    [Fact]
    public void ToString_IncludesAllModifiers()
    {
        var gesture = new HotkeyGesture
        {
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            Key = 'F'
        };

        gesture.ToString().Should().Be("Ctrl+Alt+Shift+F");
    }

    [Fact]
    public void ParseFunctionKey()
    {
        var gesture = HotkeyGesture.FromString("Ctrl+F5");

        gesture.Modifiers.Should().Be(HotkeyModifiers.Control);
        gesture.Key.Should().Be((int)System.Windows.Forms.Keys.F5);
    }
}
