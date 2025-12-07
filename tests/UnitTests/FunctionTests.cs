using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.TestUtilities;
using ClickstreamProcessorFunction;
using ClickstreamProcessorFunction.Models;
using ClickstreamProcessorFunction.Services;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace UnitTests;

public class FunctionTests
{
    private readonly Mock<IEventValidator> _mockValidator;
    private readonly Mock<ISessionEnrichmentService> _mockEnrichment;
    private readonly Mock<IMetricsAggregator> _mockMetrics;
    private readonly Mock<IStateStore> _mockStateStore;
    private readonly TestLambdaContext _context;
    private readonly TestLambdaLogger _logger;

    public FunctionTests()
    {
        _mockValidator = new Mock<IEventValidator>();
        _mockEnrichment = new Mock<ISessionEnrichmentService>();
        _mockMetrics = new Mock<IMetricsAggregator>();
        _mockStateStore = new Mock<IStateStore>();
        _logger = new TestLambdaLogger();
        _context = new TestLambdaContext
        {
            FunctionName = "clickstream-processor",
            FunctionVersion = "1",
            MemoryLimitInMB = 512,
            Logger = _logger
        };
    }

    [Fact]
    public async Task FunctionHandler_ValidBatch_ProcessesAllEvents()
    {
        var function = new Function(
            _mockValidator.Object,
            _mockEnrichment.Object,
            _mockMetrics.Object,
            _mockStateStore.Object,
            _logger
        );

        var kinesisEvent = CreateKinesisEvent(new[]
        {
            CreateClickstreamEvent("user1", "page_view"),
            CreateClickstreamEvent("user2", "click")
        });

        _mockEnrichment.Setup(e => e.EnrichAsync(It.IsAny<ClickstreamEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrichedEvent
            {
                EventId = "evt1",
                UserId = "user1",
                SessionId = "sess1",
                EventName = "test",
                EventType = "page_view",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

        var response = await function.FunctionHandler(kinesisEvent, _context);

        Assert.Empty(response.BatchItemFailures);
        _mockValidator.Verify(v => v.Validate(It.IsAny<ClickstreamEvent>()), Times.Exactly(2));
        _mockEnrichment.Verify(e => e.EnrichAsync(It.IsAny<ClickstreamEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockStateStore.Verify(s => s.ArchiveEventAsync(It.IsAny<EnrichedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockMetrics.Verify(m => m.AggregateAsync(It.IsAny<EnrichedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FunctionHandler_InvalidEvents_ReportsBatchFailures()
    {
        var function = new Function(
            _mockValidator.Object,
            _mockEnrichment.Object,
            _mockMetrics.Object,
            _mockStateStore.Object,
            _logger
        );

        var kinesisEvent = CreateKinesisEvent(new[]
        {
            CreateClickstreamEvent("", "page_view"),
            CreateClickstreamEvent("user2", "invalid_type")
        });

        _mockValidator.Setup(v => v.Validate(It.IsAny<ClickstreamEvent>()))
            .Throws(new ValidationException("UserId is required"));

        var response = await function.FunctionHandler(kinesisEvent, _context);

        Assert.Equal(2, response.BatchItemFailures.Count);
        _mockEnrichment.Verify(e => e.EnrichAsync(It.IsAny<ClickstreamEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FunctionHandler_MixedBatch_ProcessesValidReportsInvalid()
    {
        var function = new Function(
            _mockValidator.Object,
            _mockEnrichment.Object,
            _mockMetrics.Object,
            _mockStateStore.Object,
            _logger
        );

        var kinesisEvent = CreateKinesisEvent(new[]
        {
            CreateClickstreamEvent("user1", "page_view"),
            CreateClickstreamEvent("", "click"),
            CreateClickstreamEvent("user3", "conversion")
        });

        var callCount = 0;
        _mockValidator.Setup(v => v.Validate(It.IsAny<ClickstreamEvent>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new ValidationException("UserId is required");
                }
            });

        _mockEnrichment.Setup(e => e.EnrichAsync(It.IsAny<ClickstreamEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnrichedEvent
            {
                EventId = "evt1",
                UserId = "user1",
                SessionId = "sess1",
                EventName = "test",
                EventType = "page_view",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

        var response = await function.FunctionHandler(kinesisEvent, _context);

        Assert.Single(response.BatchItemFailures);
        _mockValidator.Verify(v => v.Validate(It.IsAny<ClickstreamEvent>()), Times.Exactly(3));
        _mockEnrichment.Verify(e => e.EnrichAsync(It.IsAny<ClickstreamEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static KinesisEvent CreateKinesisEvent(ClickstreamEvent[] events)
    {
        var records = events.Select((evt, index) => new KinesisEvent.KinesisEventRecord
        {
            Kinesis = new KinesisEvent.Record
            {
                SequenceNumber = $"seq{index}",
                Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt))),
                PartitionKey = evt.UserId
            }
        }).ToList();

        return new KinesisEvent
        {
            Records = records
        };
    }

    private static ClickstreamEvent CreateClickstreamEvent(string userId, string eventType)
    {
        return new ClickstreamEvent
        {
            UserId = userId,
            EventName = "test_event",
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/page",
            UserAgent = "Mozilla/5.0"
        };
    }
}
