using System.Text.Json.Serialization;

namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// Event enriched with session and behavioral data
/// </summary>
public class EnrichedEvent
{
    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("eventName")]
    public required string EventName { get; init; }

    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("pagePath")]
    public string? PagePath { get; init; }

    [JsonPropertyName("pageTitle")]
    public string? PageTitle { get; init; }

    [JsonPropertyName("referrer")]
    public string? Referrer { get; init; }

    [JsonPropertyName("isNewSession")]
    public bool IsNewSession { get; init; }

    [JsonPropertyName("sessionStartTime")]
    public long SessionStartTime { get; init; }

    [JsonPropertyName("sessionDuration")]
    public int SessionDuration { get; init; }

    [JsonPropertyName("pageViewCount")]
    public int PageViewCount { get; init; }

    [JsonPropertyName("device")]
    public string? Device { get; init; }

    [JsonPropertyName("browser")]
    public string? Browser { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }
}
