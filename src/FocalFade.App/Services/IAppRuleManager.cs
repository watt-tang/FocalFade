using FocalFade.Models;

namespace FocalFade.Services;

public interface IAppRuleManager
{
    DimmingBehavior Evaluate(string processName);
    void SetRules(List<AppRule> rules);
}
