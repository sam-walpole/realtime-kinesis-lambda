# Architecture Diagram

```mermaid
graph LR
    A[Web/Mobile Apps] -->|Event Stream| B[Kinesis Data Stream<br/>2 shards, 24hr retention]
    B -->|Batch of 100| C[Lambda Function<br/>.NET 8 Native AOT<br/>512MB, 30s timeout]

    C -->|Store Sessions| D[DynamoDB Sessions Table<br/>TTL enabled]
    C -->|Store Metrics| E[DynamoDB Metrics Table<br/>5-min windows]
    C -->|Archive Events| F[S3 Events Bucket<br/>Glacier after 90 days]
    C -->|Logs| G[CloudWatch Logs<br/>7-day retention]

    D -.->|Read Session State| C

    C -->|Batch Failures| B

    H[Monitoring Dashboard] -.->|Queries| D
    H -.->|Queries| E

    style C fill:#FF9900,stroke:#232F3E,stroke-width:3px,color:#fff
    style B fill:#8C4FFF,stroke:#232F3E,stroke-width:2px,color:#fff
    style D fill:#527FFF,stroke:#232F3E,stroke-width:2px,color:#fff
    style E fill:#527FFF,stroke:#232F3E,stroke-width:2px,color:#fff
    style F fill:#569A31,stroke:#232F3E,stroke-width:2px,color:#fff
    style G fill:#FF4F8B,stroke:#232F3E,stroke-width:2px,color:#fff
```

## Data Flow

1. **Event Ingestion**: Web and mobile applications send clickstream events to Kinesis Data Stream
2. **Batch Processing**: Lambda function processes events in batches of up to 100 records
3. **Validation & Enrichment**: Each event is validated and enriched with session data
4. **State Management**: User sessions are stored in DynamoDB with 7-day TTL
5. **Metrics Aggregation**: Real-time metrics are aggregated in 5-minute windows
6. **Event Archival**: Raw events are archived to S3 for historical analysis
7. **Partial Batch Failures**: Invalid events are reported back to Kinesis for retry
8. **Observability**: All processing is logged to CloudWatch for monitoring

## Key Features

- **Native AOT Compilation**: Cold starts < 100ms (86% improvement)
- **Batch Failure Reporting**: Only retry failed records, not entire batches
- **Session Tracking**: 30-minute timeout with automatic expiration
- **Cost Optimization**: 10-second batching window reduces invocations by 80%
- **Data Lifecycle**: S3 objects transition to Glacier after 90 days
