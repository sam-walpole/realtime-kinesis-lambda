using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

public class MetricsAggregator : IMetricsAggregator
{
    private readonly IStateStore _stateStore;

    public MetricsAggregator(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task AggregateAsync(EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default)
    {
        var timeWindow = GetTimeWindow(enrichedEvent.Timestamp);

        if (enrichedEvent.EventType == "page_view" && !string.IsNullOrEmpty(enrichedEvent.PagePath))
        {
            await IncrementPageViewAsync(enrichedEvent.PagePath, enrichedEvent.Timestamp, cancellationToken);
        }

        if (enrichedEvent.EventType == "conversion")
        {
            var value = 0m;
            if (enrichedEvent.Properties?.TryGetValue("value", out var valueObj) == true)
            {
                value = Convert.ToDecimal(valueObj);
            }
            await RecordConversionAsync(enrichedEvent.UserId, value, enrichedEvent.Timestamp, cancellationToken);
        }
    }

    public async Task IncrementPageViewAsync(string pagePath, long timestamp, CancellationToken cancellationToken = default)
    {
        var timeWindow = GetTimeWindow(timestamp);
        var metricId = $"metrics#{timeWindow}";

        var metrics = new EventMetrics
        {
            MetricId = metricId,
            TimeWindow = timeWindow,
            Timestamp = timestamp,
            TotalPageViews = 1
        };

        metrics.TopPages = new Dictionary<string, int> { [pagePath] = 1 };

        await _stateStore.UpdateMetricsAsync(metrics, cancellationToken);
    }

    public async Task RecordConversionAsync(string userId, decimal value, long timestamp, CancellationToken cancellationToken = default)
    {
        var timeWindow = GetTimeWindow(timestamp);
        var metricId = $"metrics#{timeWindow}";

        var metrics = new EventMetrics
        {
            MetricId = metricId,
            TimeWindow = timeWindow,
            Timestamp = timestamp,
            TotalConversions = 1,
            TotalRevenue = value
        };

        await _stateStore.UpdateMetricsAsync(metrics, cancellationToken);
    }

    private static string GetTimeWindow(long timestamp)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var roundedMinute = dt.Minute - (dt.Minute % 5);
        var window = new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, roundedMinute, 0, dt.Offset);
        return window.ToString("yyyy-MM-dd-HH:mm");
    }
}
