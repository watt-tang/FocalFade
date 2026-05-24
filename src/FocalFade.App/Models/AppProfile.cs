using System.Text.Json.Serialization;

namespace FocalFade.Models;

public sealed record AppProfile
{
    [JsonPropertyName("processName")]
    public string ProcessName { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("customOpacity")]
    public double? CustomOpacity { get; init; }

    [JsonPropertyName("behavior")]
    public DimmingBehavior Behavior { get; init; } = DimmingBehavior.Normal;
}
