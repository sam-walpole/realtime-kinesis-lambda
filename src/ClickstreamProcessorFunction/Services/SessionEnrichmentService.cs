using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

public class SessionEnrichmentService : ISessionEnrichmentService
{
    private readonly IStateStore _stateStore;
    private const int SessionTimeoutSeconds = 1800; // 30 minutes

    public SessionEnrichmentService(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task<EnrichedEvent> EnrichAsync(ClickstreamEvent clickEvent, CancellationToken cancellationToken = default)
    {
        var session = await GetOrCreateSessionAsync(clickEvent.UserId, clickEvent.SessionId, cancellationToken);

        var (device, browser, os) = ParseUserAgent(clickEvent.UserAgent);

        var enrichedEvent = new EnrichedEvent
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = clickEvent.UserId,
            SessionId = session.SessionId,
            EventName = clickEvent.EventName,
            EventType = clickEvent.EventType,
            Timestamp = clickEvent.Timestamp,
            Url = clickEvent.Url,
            PagePath = ExtractPath(clickEvent.Url),
            Referrer = clickEvent.Referrer,
            IsNewSession = session.PageViewCount == 0,
            SessionStartTime = session.StartTime,
            SessionDuration = (int)(clickEvent.Timestamp - session.StartTime),
            PageViewCount = session.PageViewCount + 1,
            Device = device,
            Browser = browser,
            Os = os,
            Properties = clickEvent.Properties
        };

        await UpdateSessionAsync(session, enrichedEvent, cancellationToken);

        return enrichedEvent;
    }

    public async Task<UserSession> GetOrCreateSessionAsync(string userId, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            var existing = await _stateStore.GetSessionAsync(sessionId, cancellationToken);
            if (existing != null && !IsSessionExpired(existing))
            {
                return existing;
            }
        }

        var newSession = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserId = userId,
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            LastActivityTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Ttl = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
        };

        return newSession;
    }

    public async Task UpdateSessionAsync(UserSession session, EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default)
    {
        session.LastActivityTime = enrichedEvent.Timestamp;
        session.PageViewCount++;
        session.EventCount++;

        if (session.PageViews.Count == 0)
        {
            session.LandingPage = enrichedEvent.PagePath;
            session.Referrer = enrichedEvent.Referrer;
            session.Device = enrichedEvent.Device;
            session.Browser = enrichedEvent.Browser;
        }

        if (!string.IsNullOrEmpty(enrichedEvent.PagePath) &&
            !session.PageViews.Contains(enrichedEvent.PagePath))
        {
            session.PageViews.Add(enrichedEvent.PagePath);
        }

        session.ExitPage = enrichedEvent.PagePath;

        if (enrichedEvent.EventType == "conversion")
        {
            session.HasConverted = true;
            if (enrichedEvent.Properties?.TryGetValue("value", out var valueObj) == true)
            {
                session.ConversionValue += Convert.ToDecimal(valueObj);
            }
        }

        await _stateStore.SaveSessionAsync(session, cancellationToken);
    }

    private static bool IsSessionExpired(UserSession session)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (currentTime - session.LastActivityTime) > SessionTimeoutSeconds;
    }

    private static string? ExtractPath(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.PathAndQuery;
        }

        return url;
    }

    private static (string? Device, string? Browser, string? Os) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return (null, null, null);

        var device = DetermineDevice(userAgent);
        var browser = DetermineBrowser(userAgent);
        var os = DetermineOS(userAgent);

        return (device, browser, os);
    }

    private static string DetermineDevice(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile")) return "mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad")) return "tablet";
        return "desktop";
    }

    private static string DetermineBrowser(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("edg/")) return "Edge";
        if (ua.Contains("chrome/")) return "Chrome";
        if (ua.Contains("firefox/")) return "Firefox";
        if (ua.Contains("safari/") && !ua.Contains("chrome")) return "Safari";
        return "Other";
    }

    private static string DetermineOS(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("windows")) return "Windows";
        if (ua.Contains("mac os")) return "macOS";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("iphone") || ua.Contains("ipad")) return "iOS";
        if (ua.Contains("linux")) return "Linux";
        return "Other";
    }
}
