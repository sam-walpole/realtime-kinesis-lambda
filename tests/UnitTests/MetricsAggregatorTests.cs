using ClickstreamProcessorFunction.Models;
using ClickstreamProcessorFunction.Services;
using Moq;
using Xunit;

namespace UnitTests;

public class MetricsAggregatorTests
{
    private readonly Mock<IStateStore> _mockStateStore;
    private readonly MetricsAggregator _metricsAggregator;

    public MetricsAggregatorTests()
    {
        _mockStateStore = new Mock<IStateStore>();
        _metricsAggregator = new MetricsAggregator(_mockStateStore.Object);
    }

    [Fact]
    public async Task Aggregate_PageView_IncrementsCounter()
    {
        var enrichedEvent = new EnrichedEvent
        {
            EventId = "evt123",
            UserId = "user1",
            SessionId = "sess1",
            EventName = "home_view",
            EventType = "page_view",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            PagePath = "/products"
        };

        await _metricsAggregator.AggregateAsync(enrichedEvent, CancellationToken.None);

        _mockStateStore.Verify(s => s.UpdateMetricsAsync(It.Is<EventMetrics>(
            m => m.TotalPageViews == 1 &&
                 m.TopPages != null &&
                 m.TopPages.ContainsKey("/products") &&
                 m.TopPages["/products"] == 1
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aggregate_Conversion_UpdatesMetrics()
    {
        var enrichedEvent = new EnrichedEvent
        {
            EventId = "evt456",
            UserId = "user2",
            SessionId = "sess2",
            EventName = "purchase",
            EventType = "conversion",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Properties = new Dictionary<string, object>
            {
                { "value", 99.99m }
            }
        };

        await _metricsAggregator.AggregateAsync(enrichedEvent, CancellationToken.None);

        _mockStateStore.Verify(s => s.UpdateMetricsAsync(It.Is<EventMetrics>(
            m => m.TotalConversions == 1 && m.TotalRevenue == 99.99m
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
