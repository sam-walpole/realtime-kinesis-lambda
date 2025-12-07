using Amazon.CDK;
using Amazon.CDK.AWS.Kinesis;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.Logs;
using Constructs;
using System.Collections.Generic;
using Stream = Amazon.CDK.AWS.Kinesis.Stream;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Infrastructure;

public class ClickstreamStack : Stack
{
    public ClickstreamStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var stream = new Stream(this, "ClickstreamDataStream", new StreamProps
        {
            StreamName = "clickstream-events",
            ShardCount = 2,
            RetentionPeriod = Duration.Hours(24),
            StreamMode = StreamMode.PROVISIONED
        });

        var sessionsTable = new Table(this, "SessionsTable", new TableProps
        {
            TableName = "clickstream-sessions",
            PartitionKey = new Attribute
            {
                Name = "sessionId",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            TimeToLiveAttribute = "ttl",
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var metricsTable = new Table(this, "MetricsTable", new TableProps
        {
            TableName = "clickstream-metrics",
            PartitionKey = new Attribute
            {
                Name = "metricId",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
            {
                Name = "timeWindow",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var eventsBucket = new Bucket(this, "EventsBucket", new BucketProps
        {
            BucketName = $"clickstream-events-{this.Account}",
            Versioned = false,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            LifecycleRules = new[]
            {
                new LifecycleRule
                {
                    Enabled = true,
                    Transitions = new[]
                    {
                        new Transition
                        {
                            StorageClass = StorageClass.INTELLIGENT_TIERING,
                            TransitionAfter = Duration.Days(30)
                        },
                        new Transition
                        {
                            StorageClass = StorageClass.GLACIER,
                            TransitionAfter = Duration.Days(90)
                        }
                    }
                }
            }
        });

        var function = new Function(this, "ClickstreamProcessor", new FunctionProps
        {
            FunctionName = "clickstream-processor",
            Runtime = Runtime.PROVIDED_AL2023,
            Handler = "bootstrap",
            Code = Code.FromAsset("../src/ClickstreamProcessorFunction/bin/Release/net8.0/linux-x64/publish"),
            MemorySize = 512,
            Timeout = Duration.Seconds(30),
            LogRetention = RetentionDays.ONE_WEEK,
            Environment = new Dictionary<string, string>
            {
                { "SESSIONS_TABLE", sessionsTable.TableName },
                { "METRICS_TABLE", metricsTable.TableName },
                { "EVENTS_BUCKET", eventsBucket.BucketName }
            }
        });

        sessionsTable.GrantReadWriteData(function);
        metricsTable.GrantReadWriteData(function);
        eventsBucket.GrantReadWrite(function);

        function.AddEventSourceMapping("KinesisEventSource", new EventSourceMappingOptions
        {
            EventSourceArn = stream.StreamArn,
            StartingPosition = StartingPosition.LATEST,
            BatchSize = 100,
            ParallelizationFactor = 10,
            ReportBatchItemFailures = true
        });

        stream.GrantRead(function);

        new CfnOutput(this, "StreamName", new CfnOutputProps
        {
            Value = stream.StreamName,
            Description = "Kinesis stream name for event ingestion"
        });

        new CfnOutput(this, "StreamArn", new CfnOutputProps
        {
            Value = stream.StreamArn,
            Description = "Kinesis stream ARN"
        });

        new CfnOutput(this, "FunctionName", new CfnOutputProps
        {
            Value = function.FunctionName,
            Description = "Lambda function name"
        });

        new CfnOutput(this, "SessionsTableName", new CfnOutputProps
        {
            Value = sessionsTable.TableName,
            Description = "DynamoDB sessions table name"
        });

        new CfnOutput(this, "MetricsTableName", new CfnOutputProps
        {
            Value = metricsTable.TableName,
            Description = "DynamoDB metrics table name"
        });

        new CfnOutput(this, "EventsBucketName", new CfnOutputProps
        {
            Value = eventsBucket.BucketName,
            Description = "S3 bucket for event archives"
        });
    }
}
