using System.Text.Json.Serialization;

namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// Aggregated metrics for real-time dashboards
/// </summary>
public class EventMetrics
{
    [JsonPropertyName("metricId")]
    public string MetricId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("timeWindow")]
    public string TimeWindow { get; set; } = string.Empty;

    [JsonPropertyName("totalPageViews")]
    public int TotalPageViews { get; set; }

    [JsonPropertyName("totalClicks")]
    public int TotalClicks { get; set; }

    [JsonPropertyName("totalConversions")]
    public int TotalConversions { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("activeSessions")]
    public int ActiveSessions { get; set; }

    [JsonPropertyName("bounceRate")]
    public double BounceRate { get; set; }

    [JsonPropertyName("avgSessionDuration")]
    public double AvgSessionDuration { get; set; }

    [JsonPropertyName("conversionRate")]
    public double ConversionRate { get; set; }

    [JsonPropertyName("topPages")]
    public Dictionary<string, int>? TopPages { get; set; }

    [JsonPropertyName("topEvents")]
    public Dictionary<string, int>? TopEvents { get; set; }
}
