using FocalFade.Models;
using FocalFade.Services;
using Microsoft.Extensions.Logging;

namespace FocalFade.Core;

public sealed class AppRuleManager : IAppRuleManager
{
    private readonly ILogger<AppRuleManager> _logger;
    private List<AppRule> _rules = [];
    private readonly object _lock = new();

    public AppRuleManager(ILogger<AppRuleManager> logger)
    {
        _logger = logger;
    }

    public void SetRules(List<AppRule> rules)
    {
        lock (_lock) { _rules = rules; }
        _logger.LogDebug("App rules updated: {Count} rules", rules.Count);
    }

    public AppRuleResult Evaluate(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return new AppRuleResult();

        lock (_lock)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Enabled) continue;

                if (string.Equals(rule.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("App rule matched: {Process} -> {Behavior}", processName, rule.Behavior);
                    return new AppRuleResult
                    {
                        Behavior = rule.Behavior,
                        OpacityOverride = rule.OpacityOverride,
                        DimColorOverride = rule.DimColorOverride,
                        FocusMarginOverride = rule.FocusMarginOverride,
                        CornerRadiusOverride = rule.CornerRadiusOverride
                    };
                }

                // Wildcard support
                if (rule.ProcessName.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = rule.ProcessName[..^2];
                    if (processName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return new AppRuleResult
                        {
                            Behavior = rule.Behavior,
                            OpacityOverride = rule.OpacityOverride,
                            DimColorOverride = rule.DimColorOverride,
                            FocusMarginOverride = rule.FocusMarginOverride,
                            CornerRadiusOverride = rule.CornerRadiusOverride
                        };
                    }
                }
            }
        }

        return new AppRuleResult();
    }
}
