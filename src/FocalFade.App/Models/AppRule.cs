using System.Text.Json.Serialization;

namespace FocalFade.Models;

public sealed record AppRule
{
    [JsonPropertyName("processName")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("behavior")]
    public DimmingBehavior Behavior { get; init; } = DimmingBehavior.Ignore;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
}
