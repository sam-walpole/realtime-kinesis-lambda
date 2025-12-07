using ClickstreamProcessorFunction.Models;
using ClickstreamProcessorFunction.Services;
using Xunit;

namespace UnitTests;

public class EventValidatorTests
{
    private readonly EventValidator _validator;

    public EventValidatorTests()
    {
        _validator = new EventValidator();
    }

    [Fact]
    public void Validate_ValidEvent_DoesNotThrow()
    {
        var clickEvent = new ClickstreamEvent
        {
            UserId = "user123",
            EventName = "page_view",
            EventType = "page_view",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/products",
            UserAgent = "Mozilla/5.0"
        };

        _validator.Validate(clickEvent);
    }

    [Theory]
    [InlineData("", "page_view", "page_view")]
    [InlineData("user1", "", "page_view")]
    [InlineData("user1", "event", "")]
    [InlineData("user1", "event", "invalid_type")]
    public void Validate_MissingFields_ThrowsValidationException(
        string userId,
        string eventName,
        string eventType)
    {
        var clickEvent = new ClickstreamEvent
        {
            UserId = userId,
            EventName = eventName,
            EventType = eventType,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        Assert.Throws<ValidationException>(() => _validator.Validate(clickEvent));
    }
}
