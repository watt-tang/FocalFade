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
        lock (_lock)
        {
            _rules = rules;
        }
        _logger.LogDebug("App rules updated: {Count} rules", rules.Count);
    }

    public DimmingBehavior Evaluate(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return DimmingBehavior.Normal;

        lock (_lock)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Enabled) continue;

                if (string.Equals(rule.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("App rule matched: {Process} -> {Behavior}", processName, rule.Behavior);
                    return rule.Behavior;
                }

                // Wildcard support (future-proofing)
                if (rule.ProcessName.EndsWith(".*", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = rule.ProcessName[..^2];
                    if (processName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("App rule wildcard matched: {Process} -> {Behavior}", processName, rule.Behavior);
                        return rule.Behavior;
                    }
                }
            }
        }

        return DimmingBehavior.Normal;
    }
}
