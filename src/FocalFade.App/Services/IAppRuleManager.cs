using FocalFade.Models;

namespace FocalFade.Services;

public interface IAppRuleManager
{
    AppRuleResult Evaluate(string processName);
    void SetRules(List<AppRule> rules);
}

public sealed record AppRuleResult
{
    public DimmingBehavior Behavior { get; init; } = DimmingBehavior.Normal;
    public double? OpacityOverride { get; init; }
    public string? DimColorOverride { get; init; }
    public double? FocusMarginOverride { get; init; }
    public double? CornerRadiusOverride { get; init; }
}
