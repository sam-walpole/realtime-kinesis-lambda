using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

/// <summary>
/// Persists session state and event data to DynamoDB and S3
/// </summary>
public interface IStateStore
{
    /// <summary>
    /// Retrieves user session from DynamoDB
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if exists, null otherwise</returns>
    Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates user session in DynamoDB
    /// </summary>
    /// <param name="session">Session to persist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives enriched event to S3
    /// </summary>
    /// <param name="enrichedEvent">Event to archive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ArchiveEventAsync(EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch writes multiple sessions to DynamoDB
    /// </summary>
    /// <param name="sessions">Sessions to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BatchWriteSessionsAsync(List<UserSession> sessions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates aggregated metrics in DynamoDB
    /// </summary>
    /// <param name="metrics">Metrics to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateMetricsAsync(EventMetrics metrics, CancellationToken cancellationToken = default);
}
