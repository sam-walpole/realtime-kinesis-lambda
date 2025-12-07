using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

public class EventValidator : IEventValidator
{
    private static readonly HashSet<string> ValidEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "page_view", "click", "conversion", "form_submit", "video_play", "scroll"
    };

    public void Validate(ClickstreamEvent clickEvent)
    {
        if (string.IsNullOrWhiteSpace(clickEvent.UserId))
        {
            throw new ValidationException("UserId is required");
        }

        if (string.IsNullOrWhiteSpace(clickEvent.EventName))
        {
            throw new ValidationException("EventName is required");
        }

        if (string.IsNullOrWhiteSpace(clickEvent.EventType))
        {
            throw new ValidationException("EventType is required");
        }

        if (!ValidEventTypes.Contains(clickEvent.EventType))
        {
            throw new ValidationException($"Invalid EventType: {clickEvent.EventType}. Must be one of: {string.Join(", ", ValidEventTypes)}");
        }

        if (clickEvent.Timestamp <= 0)
        {
            throw new ValidationException("Timestamp must be a valid Unix timestamp");
        }

        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var maxAge = 86400;

        if (clickEvent.Timestamp > currentTime + 300)
        {
            throw new ValidationException("Timestamp cannot be in the future");
        }

        if (clickEvent.Timestamp < currentTime - maxAge)
        {
            throw new ValidationException($"Event is too old (>24 hours)");
        }

        if (!string.IsNullOrEmpty(clickEvent.Url) && !IsValidUrl(clickEvent.Url))
        {
            throw new ValidationException($"Invalid URL format: {clickEvent.Url}");
        }
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
