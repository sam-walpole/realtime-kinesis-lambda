using System.Text.Json.Serialization;

namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// User session state for tracking journeys
/// </summary>
public class UserSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("lastActivityTime")]
    public long LastActivityTime { get; set; }

    [JsonPropertyName("pageViews")]
    public List<string> PageViews { get; set; } = new();

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    [JsonPropertyName("pageViewCount")]
    public int PageViewCount { get; set; }

    [JsonPropertyName("eventCount")]
    public int EventCount { get; set; }

    [JsonPropertyName("hasConverted")]
    public bool HasConverted { get; set; }

    [JsonPropertyName("conversionValue")]
    public decimal ConversionValue { get; set; }

    [JsonPropertyName("landingPage")]
    public string? LandingPage { get; set; }

    [JsonPropertyName("exitPage")]
    public string? ExitPage { get; set; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; set; }

    [JsonPropertyName("device")]
    public string? Device { get; set; }

    [JsonPropertyName("browser")]
    public string? Browser { get; set; }

    [JsonPropertyName("ttl")]
    public long Ttl { get; set; }
}
