using FluentAssertions;
using FocalFade.Core;
using FocalFade.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FocalFade.Tests;

public class AppRuleManagerTests
{
    private readonly AppRuleManager _manager;

    public AppRuleManagerTests()
    {
        _manager = new AppRuleManager(NullLogger<AppRuleManager>.Instance);
    }

    [Fact]
    public void ExactProcessMatch_ReturnsCorrectBehavior()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "notepad.exe", Behavior = DimmingBehavior.Ignore, Enabled = true }
        ]);

        _manager.Evaluate("notepad.exe").Should().Be(DimmingBehavior.Ignore);
    }

    [Fact]
    public void CaseInsensitive_MatchesCorrectly()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "Notepad.EXE", Behavior = DimmingBehavior.Ignore, Enabled = true }
        ]);

        _manager.Evaluate("notepad.exe").Should().Be(DimmingBehavior.Ignore);
        _manager.Evaluate("NOTEPAD.EXE").Should().Be(DimmingBehavior.Ignore);
    }

    [Fact]
    public void DisabledRule_IsIgnored()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "notepad.exe", Behavior = DimmingBehavior.Ignore, Enabled = false }
        ]);

        _manager.Evaluate("notepad.exe").Should().Be(DimmingBehavior.Normal);
    }

    [Fact]
    public void NoMatchingRule_ReturnsNormal()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "notepad.exe", Behavior = DimmingBehavior.Ignore, Enabled = true }
        ]);

        _manager.Evaluate("chrome.exe").Should().Be(DimmingBehavior.Normal);
    }

    [Fact]
    public void EmptyProcessName_ReturnsNormal()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "notepad.exe", Behavior = DimmingBehavior.Ignore, Enabled = true }
        ]);

        _manager.Evaluate("").Should().Be(DimmingBehavior.Normal);
    }

    [Fact]
    public void MultipleRules_FirstMatchWins()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.Ignore, Enabled = true },
            new AppRule { ProcessName = "test.exe", Behavior = DimmingBehavior.ForceDim, Enabled = true }
        ]);

        _manager.Evaluate("test.exe").Should().Be(DimmingBehavior.Ignore);
    }

    [Fact]
    public void PresentationRule_ReturnsPresentationBehavior()
    {
        _manager.SetRules([
            new AppRule { ProcessName = "powerpnt.exe", Behavior = DimmingBehavior.Presentation, Enabled = true }
        ]);

        _manager.Evaluate("powerpnt.exe").Should().Be(DimmingBehavior.Presentation);
    }
}
