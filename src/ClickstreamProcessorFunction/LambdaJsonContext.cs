using System.Text.Json.Serialization;
using ClickstreamProcessorFunction.Models;
using Amazon.Lambda.KinesisEvents;

namespace ClickstreamProcessorFunction;

/// <summary>
/// Source-generated JSON serialization context for Native AOT compatibility
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(ClickstreamEvent))]
[JsonSerializable(typeof(EnrichedEvent))]
[JsonSerializable(typeof(UserSession))]
[JsonSerializable(typeof(EventMetrics))]
[JsonSerializable(typeof(UserBehavior))]
[JsonSerializable(typeof(ProcessingResult))]
[JsonSerializable(typeof(Models.StreamsEventResponse))]
[JsonSerializable(typeof(BatchItemFailure))]
[JsonSerializable(typeof(KinesisEvent))]
[JsonSerializable(typeof(KinesisEvent.KinesisEventRecord))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class LambdaJsonContext : JsonSerializerContext
{
}
