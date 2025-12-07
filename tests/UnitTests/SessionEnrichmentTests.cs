using ClickstreamProcessorFunction.Models;
using ClickstreamProcessorFunction.Services;
using Moq;
using Xunit;

namespace UnitTests;

public class SessionEnrichmentTests
{
    private readonly Mock<IStateStore> _mockStateStore;
    private readonly SessionEnrichmentService _enrichmentService;

    public SessionEnrichmentTests()
    {
        _mockStateStore = new Mock<IStateStore>();
        _enrichmentService = new SessionEnrichmentService(_mockStateStore.Object);
    }

    [Fact]
    public async Task Enrich_NewSession_CreatesSessionRecord()
    {
        var clickEvent = new ClickstreamEvent
        {
            UserId = "user123",
            SessionId = null,
            EventName = "page_view",
            EventType = "page_view",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/home",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0"
        };

        _mockStateStore.Setup(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);

        var enrichedEvent = await _enrichmentService.EnrichAsync(clickEvent, CancellationToken.None);

        Assert.NotNull(enrichedEvent);
        Assert.Equal("user123", enrichedEvent.UserId);
        Assert.NotEmpty(enrichedEvent.SessionId);
        Assert.True(enrichedEvent.IsNewSession);
        Assert.Equal("/home", enrichedEvent.PagePath);
        Assert.Equal("desktop", enrichedEvent.Device);
        Assert.Equal("Chrome", enrichedEvent.Browser);
        Assert.Equal("Windows", enrichedEvent.Os);

        _mockStateStore.Verify(s => s.SaveSessionAsync(It.Is<UserSession>(
            session => session.UserId == "user123" && session.PageViewCount == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enrich_ExistingSession_UpdatesSessionData()
    {
        var existingSession = new UserSession
        {
            SessionId = "sess-existing",
            UserId = "user123",
            StartTime = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(),
            LastActivityTime = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(),
            PageViewCount = 3,
            EventCount = 5,
            PageViews = new List<string> { "/home", "/products" },
            LandingPage = "/home"
        };

        var clickEvent = new ClickstreamEvent
        {
            UserId = "user123",
            SessionId = "sess-existing",
            EventName = "page_view",
            EventType = "page_view",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/checkout",
            UserAgent = "Mozilla/5.0"
        };

        _mockStateStore.Setup(s => s.GetSessionAsync("sess-existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        var enrichedEvent = await _enrichmentService.EnrichAsync(clickEvent, CancellationToken.None);

        Assert.NotNull(enrichedEvent);
        Assert.Equal("sess-existing", enrichedEvent.SessionId);
        Assert.False(enrichedEvent.IsNewSession);
        Assert.Equal(4, enrichedEvent.PageViewCount);

        _mockStateStore.Verify(s => s.SaveSessionAsync(It.Is<UserSession>(
            session => session.SessionId == "sess-existing" &&
                       session.PageViewCount == 4 &&
                       session.EventCount == 6 &&
                       session.PageViews.Contains("/checkout")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
