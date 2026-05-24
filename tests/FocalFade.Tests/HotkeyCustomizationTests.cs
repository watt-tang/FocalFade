using FluentAssertions;
using FocalFade.Models;
using FocalFade.Settings;

namespace FocalFade.Tests;

public class HotkeyCustomizationTests
{
    [Fact]
    public void HotkeyBinding_DefaultGesture_IsSet()
    {
        var binding = new HotkeyBindingViewModel("ToggleEnabled", "Toggle", "Ctrl+Alt+F", "Ctrl+Alt+F");
        binding.GestureString.Should().Be("Ctrl+Alt+F");
        binding.IsModified.Should().BeFalse();
    }

    [Fact]
    public void HotkeyBinding_ModifiedGesture_Detected()
    {
        var binding = new HotkeyBindingViewModel("ToggleEnabled", "Toggle", "Ctrl+Shift+T", "Ctrl+Alt+F");
        binding.IsModified.Should().BeTrue();
    }

    [Fact]
    public void HotkeyBinding_ResetToDefault_RestoresDefault()
    {
        var binding = new HotkeyBindingViewModel("ToggleEnabled", "Toggle", "Ctrl+Shift+T", "Ctrl+Alt+F");
        binding.ResetToDefault();
        binding.GestureString.Should().Be("Ctrl+Alt+F");
        binding.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyGesture_Invalid()
    {
        var (valid, _) = HotkeyBindingViewModel.Validate("");
        valid.Should().BeFalse();
    }

    [Fact]
    public void Validate_BareKey_Invalid()
    {
        var (valid, error) = HotkeyBindingViewModel.Validate("F");
        valid.Should().BeFalse();
        error.Should().Contain("modifier");
    }

    [Fact]
    public void Validate_ValidGesture_Valid()
    {
        var (valid, _) = HotkeyBindingViewModel.Validate("Ctrl+Alt+F");
        valid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithShift_Valid()
    {
        var (valid, _) = HotkeyBindingViewModel.Validate("Ctrl+Shift+T");
        valid.Should().BeTrue();
    }

    [Fact]
    public void HotkeyAction_AllActions_HaveUniqueKeys()
    {
        var keys = HotkeyAction.AllActions.Select(a => a.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void HotkeyAction_AllActions_HaveDisplayNames()
    {
        foreach (var action in HotkeyAction.AllActions)
        {
            action.DisplayName.Should().NotBeNullOrWhiteSpace();
            action.DefaultGesture.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void HotkeyAction_DefaultGestures_AreValid()
    {
        foreach (var action in HotkeyAction.AllActions)
        {
            var (valid, _) = HotkeyBindingViewModel.Validate(action.DefaultGesture);
            valid.Should().BeTrue($"default gesture for '{action.Key}' should be valid");
        }
    }

    [Fact]
    public void HotkeyAction_DefaultGestures_AreUnique()
    {
        var gestures = HotkeyAction.AllActions.Select(a => a.DefaultGesture).ToList();
        gestures.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void RoundTrip_Serialization()
    {
        var original = new HotkeyGesture { Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt, Key = 0x46 }; // Ctrl+Alt+F
        var str = original.ToString();
        var parsed = HotkeyGesture.FromString(str);

        parsed.Modifiers.Should().Be(original.Modifiers);
        parsed.Key.Should().Be(original.Key);
    }
}
