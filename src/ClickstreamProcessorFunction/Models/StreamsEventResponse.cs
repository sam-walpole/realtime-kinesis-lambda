using System.Text.Json.Serialization;

namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// Lambda response for Kinesis batch processing with partial failures
/// </summary>
public record StreamsEventResponse(
    [property: JsonPropertyName("batchItemFailures")] List<BatchItemFailure> BatchItemFailures
);

public record BatchItemFailure(
    [property: JsonPropertyName("itemIdentifier")] string ItemIdentifier
);
