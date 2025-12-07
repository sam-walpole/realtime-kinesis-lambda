using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

/// <summary>
/// Enriches events with session tracking and user journey data
/// </summary>
public interface ISessionEnrichmentService
{
    /// <summary>
    /// Enriches event with session information
    /// </summary>
    /// <param name="clickEvent">Raw event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enriched event with session data</returns>
    Task<EnrichedEvent> EnrichAsync(ClickstreamEvent clickEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates user session
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current or new session</returns>
    Task<UserSession> GetOrCreateSessionAsync(string userId, string? sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates session with new activity
    /// </summary>
    /// <param name="session">Session to update</param>
    /// <param name="enrichedEvent">Event to add to session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionAsync(UserSession session, EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default);
}
