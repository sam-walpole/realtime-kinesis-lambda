using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

/// <summary>
/// Aggregates real-time metrics for dashboards and analytics
/// </summary>
public interface IMetricsAggregator
{
    /// <summary>
    /// Updates real-time metrics based on enriched event
    /// </summary>
    /// <param name="enrichedEvent">Event to aggregate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AggregateAsync(EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments page view counter
    /// </summary>
    /// <param name="pagePath">Page URL path</param>
    /// <param name="timestamp">Event timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task IncrementPageViewAsync(string pagePath, long timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments conversion counter
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="value">Conversion value</param>
    /// <param name="timestamp">Event timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordConversionAsync(string userId, decimal value, long timestamp, CancellationToken cancellationToken = default);
}
