using System.Text.Json.Serialization;

namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// Derived user behavior patterns for personalization
/// </summary>
public class UserBehavior
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("totalSessions")]
    public int TotalSessions { get; set; }

    [JsonPropertyName("totalPageViews")]
    public int TotalPageViews { get; set; }

    [JsonPropertyName("totalEvents")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("lifetimeValue")]
    public decimal LifetimeValue { get; set; }

    [JsonPropertyName("avgSessionDuration")]
    public double AvgSessionDuration { get; set; }

    [JsonPropertyName("lastVisit")]
    public long LastVisit { get; set; }

    [JsonPropertyName("firstVisit")]
    public long FirstVisit { get; set; }

    [JsonPropertyName("favoritePages")]
    public List<string> FavoritePages { get; set; } = new();

    [JsonPropertyName("interests")]
    public List<string> Interests { get; set; } = new();

    [JsonPropertyName("conversionHistory")]
    public List<ConversionRecord> ConversionHistory { get; set; } = new();
}

public record ConversionRecord(
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("value")] decimal Value,
    [property: JsonPropertyName("type")] string Type
);
