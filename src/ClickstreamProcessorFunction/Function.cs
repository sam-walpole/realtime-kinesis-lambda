using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using ClickstreamProcessorFunction.Models;
using ClickstreamProcessorFunction.Services;
using System.Text;
using System.Text.Json;

namespace ClickstreamProcessorFunction;

public class Function
{
    private readonly IEventValidator _validator;
    private readonly ISessionEnrichmentService _enrichmentService;
    private readonly IMetricsAggregator _metricsAggregator;
    private readonly IStateStore _stateStore;
    private readonly ILambdaLogger? _logger;

    public Function()
    {
        var sessionsTable = Environment.GetEnvironmentVariable("SESSIONS_TABLE") ?? "clickstream-sessions";
        var metricsTable = Environment.GetEnvironmentVariable("METRICS_TABLE") ?? "clickstream-metrics";
        var eventsBucket = Environment.GetEnvironmentVariable("EVENTS_BUCKET") ?? "clickstream-events";

        var dynamoDb = new Amazon.DynamoDBv2.AmazonDynamoDBClient();
        var s3Client = new Amazon.S3.AmazonS3Client();

        _stateStore = new DynamoDbStateStore(dynamoDb, s3Client, sessionsTable, metricsTable, eventsBucket);
        _validator = new EventValidator();
        _enrichmentService = new SessionEnrichmentService(_stateStore);
        _metricsAggregator = new MetricsAggregator(_stateStore);
    }

    public Function(
        IEventValidator validator,
        ISessionEnrichmentService enrichmentService,
        IMetricsAggregator metricsAggregator,
        IStateStore stateStore,
        ILambdaLogger logger)
    {
        _validator = validator;
        _enrichmentService = enrichmentService;
        _metricsAggregator = metricsAggregator;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<Models.StreamsEventResponse> FunctionHandler(
        KinesisEvent kinesisEvent,
        ILambdaContext context)
    {
        var logger = _logger ?? context.Logger;
        var batchItemFailures = new List<BatchItemFailure>();

        logger.LogInformation($"Processing batch of {kinesisEvent.Records.Count} records");

        foreach (var record in kinesisEvent.Records)
        {
            try
            {
                await ProcessRecord(record, logger);
            }
            catch (ValidationException ex)
            {
                logger.LogError($"Validation error for record {record.Kinesis.SequenceNumber}: {ex.Message}");

                batchItemFailures.Add(new BatchItemFailure(record.Kinesis.SequenceNumber));
            }
            catch (Exception ex)
            {
                logger.LogError($"Transient error processing record {record.Kinesis.SequenceNumber}: {ex.Message}");
                throw;
            }
        }

        logger.LogInformation($"Processed {kinesisEvent.Records.Count - batchItemFailures.Count} records successfully, {batchItemFailures.Count} failures");

        return new Models.StreamsEventResponse(batchItemFailures);
    }

    private async Task ProcessRecord(KinesisEvent.KinesisEventRecord record, ILambdaLogger logger)
    {
        var data = Encoding.UTF8.GetString(record.Kinesis.Data.ToArray());
        ClickstreamEvent? clickEvent;

        try
        {
            clickEvent = JsonSerializer.Deserialize(data, LambdaJsonContext.Default.ClickstreamEvent);

            if (clickEvent == null)
            {
                throw new ValidationException("Failed to deserialize event");
            }
        }
        catch (JsonException ex)
        {
            throw new ValidationException($"Invalid JSON format: {ex.Message}");
        }

        _validator.Validate(clickEvent);

        var enrichedEvent = await _enrichmentService.EnrichAsync(clickEvent, CancellationToken.None);

        await Task.WhenAll(
            _stateStore.ArchiveEventAsync(enrichedEvent, CancellationToken.None),
            _metricsAggregator.AggregateAsync(enrichedEvent, CancellationToken.None)
        );

        logger.LogInformation($"{{\"eventType\":\"ClickstreamProcessed\",\"userId\":\"{clickEvent.UserId}\",\"sessionId\":\"{enrichedEvent.SessionId}\",\"eventName\":\"{clickEvent.EventName}\",\"isNewSession\":{enrichedEvent.IsNewSession.ToString().ToLower()}}}");
    }
}
