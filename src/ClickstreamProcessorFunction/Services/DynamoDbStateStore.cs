using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using ClickstreamProcessorFunction.Models;
using System.Text.Json;

namespace ClickstreamProcessorFunction.Services;

public class DynamoDbStateStore : IStateStore
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IAmazonS3 _s3Client;
    private readonly string _sessionsTable;
    private readonly string _metricsTable;
    private readonly string _eventsBucket;

    public DynamoDbStateStore(
        IAmazonDynamoDB dynamoDb,
        IAmazonS3 s3Client,
        string sessionsTable,
        string metricsTable,
        string eventsBucket)
    {
        _dynamoDb = dynamoDb;
        _s3Client = s3Client;
        _sessionsTable = sessionsTable;
        _metricsTable = metricsTable;
        _eventsBucket = eventsBucket;
    }

    public async Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _sessionsTable,
            Key = new Dictionary<string, AttributeValue>
            {
                { "sessionId", new AttributeValue { S = sessionId } }
            },
            ConsistentRead = false
        };

        try
        {
            var response = await _dynamoDb.GetItemAsync(request, cancellationToken);

            if (response.Item is not { Count: > 0 })
                return null;

            return DeserializeSession(response.Item);
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
    }

    public async Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "sessionId", new AttributeValue { S = session.SessionId } },
            { "userId", new AttributeValue { S = session.UserId } },
            { "startTime", new AttributeValue { N = session.StartTime.ToString() } },
            { "lastActivityTime", new AttributeValue { N = session.LastActivityTime.ToString() } },
            { "pageViewCount", new AttributeValue { N = session.PageViewCount.ToString() } },
            { "eventCount", new AttributeValue { N = session.EventCount.ToString() } },
            { "hasConverted", new AttributeValue { BOOL = session.HasConverted } },
            { "conversionValue", new AttributeValue { N = session.ConversionValue.ToString("F2") } },
            { "ttl", new AttributeValue { N = session.Ttl.ToString() } }
        };

        if (session.PageViews.Any())
        {
            item["pageViews"] = new AttributeValue { SS = session.PageViews };
        }

        if (!string.IsNullOrEmpty(session.LandingPage))
        {
            item["landingPage"] = new AttributeValue { S = session.LandingPage };
        }

        if (!string.IsNullOrEmpty(session.ExitPage))
        {
            item["exitPage"] = new AttributeValue { S = session.ExitPage };
        }

        if (!string.IsNullOrEmpty(session.Referrer))
        {
            item["referrer"] = new AttributeValue { S = session.Referrer };
        }

        if (!string.IsNullOrEmpty(session.Device))
        {
            item["device"] = new AttributeValue { S = session.Device };
        }

        if (!string.IsNullOrEmpty(session.Browser))
        {
            item["browser"] = new AttributeValue { S = session.Browser };
        }

        var putRequest = new PutItemRequest
        {
            TableName = _sessionsTable,
            Item = item
        };

        await _dynamoDb.PutItemAsync(putRequest, cancellationToken);
    }

    public async Task BatchWriteSessionsAsync(List<UserSession> sessions, CancellationToken cancellationToken = default)
    {
        if (!sessions.Any())
            return;

        var batches = sessions.Chunk(25);

        foreach (var batch in batches)
        {
            var writeRequests = batch.Select(session => new WriteRequest
            {
                PutRequest = new PutRequest
                {
                    Item = SerializeSession(session)
                }
            }).ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    { _sessionsTable, writeRequests }
                }
            };

            var response = await _dynamoDb.BatchWriteItemAsync(request, cancellationToken);

            if (response.UnprocessedItems.Any())
            {
                await Task.Delay(100, cancellationToken);
                await RetryUnprocessedItemsAsync(response.UnprocessedItems, cancellationToken);
            }
        }
    }

    public async Task ArchiveEventAsync(EnrichedEvent enrichedEvent, CancellationToken cancellationToken = default)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(enrichedEvent.Timestamp);
        var key = $"events/{date:yyyy}/{date:MM}/{date:dd}/{enrichedEvent.EventId}.json";

        var json = JsonSerializer.Serialize(enrichedEvent, LambdaJsonContext.Default.EnrichedEvent);

        var putRequest = new PutObjectRequest
        {
            BucketName = _eventsBucket,
            Key = key,
            ContentBody = json,
            ContentType = "application/json",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
    }

    public async Task UpdateMetricsAsync(EventMetrics metrics, CancellationToken cancellationToken = default)
    {
        var updates = new List<string>();
        var values = new Dictionary<string, AttributeValue>();
        var needsZero = false;
        var needsZeroDecimal = false;

        if (metrics.TotalPageViews > 0)
        {
            updates.Add("totalPageViews = if_not_exists(totalPageViews, :zero) + :pageViews");
            values[":pageViews"] = new AttributeValue { N = metrics.TotalPageViews.ToString() };
            needsZero = true;
        }

        if (metrics.TotalConversions > 0)
        {
            updates.Add("totalConversions = if_not_exists(totalConversions, :zero) + :conversions");
            values[":conversions"] = new AttributeValue { N = metrics.TotalConversions.ToString() };
            needsZero = true;
        }

        if (metrics.TotalRevenue > 0)
        {
            updates.Add("totalRevenue = if_not_exists(totalRevenue, :zeroDecimal) + :revenue");
            values[":revenue"] = new AttributeValue { N = metrics.TotalRevenue.ToString("F2") };
            needsZeroDecimal = true;
        }

        if (!updates.Any())
            return;

        if (needsZero)
        {
            values[":zero"] = new AttributeValue { N = "0" };
        }

        if (needsZeroDecimal)
        {
            values[":zeroDecimal"] = new AttributeValue { N = "0.00" };
        }

        var request = new UpdateItemRequest
        {
            TableName = _metricsTable,
            Key = new Dictionary<string, AttributeValue>
            {
                { "metricId", new AttributeValue { S = metrics.MetricId } },
                { "timeWindow", new AttributeValue { S = metrics.TimeWindow } }
            },
            UpdateExpression = "SET " + string.Join(", ", updates),
            ExpressionAttributeValues = values
        };

        await _dynamoDb.UpdateItemAsync(request, cancellationToken);
    }

    private async Task RetryUnprocessedItemsAsync(Dictionary<string, List<WriteRequest>> unprocessedItems, CancellationToken cancellationToken = default)
    {
        var maxRetries = 3;
        var retryCount = 0;

        while (unprocessedItems.Any() && retryCount < maxRetries)
        {
            retryCount++;
            await Task.Delay(100 * (int)Math.Pow(2, retryCount), cancellationToken);

            var request = new BatchWriteItemRequest
            {
                RequestItems = unprocessedItems
            };

            var response = await _dynamoDb.BatchWriteItemAsync(request, cancellationToken);
            unprocessedItems = response.UnprocessedItems;
        }
    }

    private static Dictionary<string, AttributeValue> SerializeSession(UserSession session)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "sessionId", new AttributeValue { S = session.SessionId } },
            { "userId", new AttributeValue { S = session.UserId } },
            { "startTime", new AttributeValue { N = session.StartTime.ToString() } },
            { "lastActivityTime", new AttributeValue { N = session.LastActivityTime.ToString() } },
            { "pageViewCount", new AttributeValue { N = session.PageViewCount.ToString() } },
            { "eventCount", new AttributeValue { N = session.EventCount.ToString() } },
            { "hasConverted", new AttributeValue { BOOL = session.HasConverted } },
            { "conversionValue", new AttributeValue { N = session.ConversionValue.ToString("F2") } },
            { "ttl", new AttributeValue { N = session.Ttl.ToString() } }
        };

        if (session.PageViews.Any())
        {
            item["pageViews"] = new AttributeValue { SS = session.PageViews };
        }

        if (!string.IsNullOrEmpty(session.LandingPage))
        {
            item["landingPage"] = new AttributeValue { S = session.LandingPage };
        }

        return item;
    }

    private static UserSession DeserializeSession(Dictionary<string, AttributeValue> item)
    {
        return new UserSession
        {
            SessionId = item["sessionId"].S,
            UserId = item["userId"].S,
            StartTime = long.Parse(item["startTime"].N),
            LastActivityTime = long.Parse(item["lastActivityTime"].N),
            PageViewCount = int.Parse(item["pageViewCount"].N),
            EventCount = int.Parse(item["eventCount"].N),
            HasConverted = item.ContainsKey("hasConverted") && item["hasConverted"].BOOL,
            ConversionValue = item.ContainsKey("conversionValue") ? decimal.Parse(item["conversionValue"].N) : 0,
            PageViews = item.ContainsKey("pageViews") ? item["pageViews"].SS.ToList() : new List<string>(),
            LandingPage = item.ContainsKey("landingPage") ? item["landingPage"].S : null,
            ExitPage = item.ContainsKey("exitPage") ? item["exitPage"].S : null,
            Referrer = item.ContainsKey("referrer") ? item["referrer"].S : null,
            Device = item.ContainsKey("device") ? item["device"].S : null,
            Browser = item.ContainsKey("browser") ? item["browser"].S : null,
            Ttl = item.ContainsKey("ttl") ? long.Parse(item["ttl"].N) : 0
        };
    }
}
