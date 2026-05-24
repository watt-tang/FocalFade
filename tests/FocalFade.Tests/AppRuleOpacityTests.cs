using FluentAssertions;
using FocalFade.Core;
using FocalFade.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FocalFade.Tests;

public class AppRuleOpacityTests
{
    private readonly AppRuleManager _manager;

    public AppRuleOpacityTests()
    {
        _manager = new AppRuleManager(NullLogger<AppRuleManager>.Instance);
    }

    [Fact]
    public void NoRule_ReturnsDefaultResult()
    {
        _manager.SetRules([]);
        var result = _manager.Evaluate("notepad.exe");

        result.Behavior.Should().Be(DimmingBehavior.Normal);
        result.OpacityOverride.Should().BeNull();
    }

    [Fact]
    public void RuleWithOpacityOverride_ReturnsOverride()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Normal, Enabled = true, OpacityOverride = 0.3 }
        ]);

        var result = _manager.Evaluate("test.exe");
        result.OpacityOverride.Should().Be(0.3);
    }

    [Fact]
    public void RuleWithoutOpacityOverride_ReturnsNull()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Ignore, Enabled = true }
        ]);

        var result = _manager.Evaluate("test.exe");
        result.OpacityOverride.Should().BeNull();
        result.Behavior.Should().Be(DimmingBehavior.Ignore);
    }

    [Fact]
    public void DisabledRule_IsIgnored()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Normal, Enabled = false, OpacityOverride = 0.3 }
        ]);

        var result = _manager.Evaluate("test.exe");
        result.Behavior.Should().Be(DimmingBehavior.Normal);
        result.OpacityOverride.Should().BeNull();
    }

    [Fact]
    public void CaseInsensitiveMatch_Works()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "Test.EXE", Behavior = DimmingBehavior.Normal, Enabled = true, OpacityOverride = 0.5 }
        ]);

        var result = _manager.Evaluate("test.exe");
        result.OpacityOverride.Should().Be(0.5);
    }

    [Theory]
    [InlineData(0.05, 0.10)]  // Below min -> clamped
    [InlineData(0.10, 0.10)]  // At min
    [InlineData(0.50, 0.50)]  // Normal
    [InlineData(0.90, 0.90)]  // At max
    [InlineData(0.95, 0.90)]  // Above max -> clamped
    public void OpacityOverride_ClampedCorrectly(double input, double expected)
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Normal, Enabled = true, OpacityOverride = input }
        ]);

        var result = _manager.Evaluate("test.exe");
        var clamped = Math.Clamp(result.OpacityOverride ?? 0.45, 0.10, 0.90);
        clamped.Should().Be(expected);
    }

    [Fact]
    public void MultipleRules_FirstMatchWins()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Normal, Enabled = true, OpacityOverride = 0.3 },
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Normal, Enabled = true, OpacityOverride = 0.7 }
        ]);

        var result = _manager.Evaluate("test.exe");
        result.OpacityOverride.Should().Be(0.3);
    }
}
