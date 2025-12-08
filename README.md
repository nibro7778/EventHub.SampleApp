# Azure Event Hubs .NET Sample: Consumer and Producer

## Overview
- Sample solution demonstrating a resilient .NET consumer using Azure Event Hubs SDK and a simple producer.
- Focus on production readiness for the consumer: configuration, security, retries, error handling with DLQ, idempotency, graceful shutdown, observability, and deployment.

## Projects
- `ConsumerApp` (net8.0): Background service consuming from Event Hubs via Azure SDK (`EventProcessorClient`) with Blob Storage checkpointing.
- `ProducerApp` (net8.0): Console app producing sample messages.

## Prerequisites
- Azure Event Hubs instance.
- .NET 8 SDK.
- Event Hubs connection string with appropriate rights.
- Azure Storage account (Blob container for checkpoints).
- Azure Service Bus (for DLQ handling).
- Redis instance (for idempotency keys).

## Configuration
Common settings via `appsettings.json` or environment variables:
- Event Hubs: `EventHubs:ConnectionString`, `EventHubs:EventHubName`, `EventHubs:ConsumerGroup`.
- Blob Storage: `BlobStorage:ConnectionString` (SAS or connection string), `BlobStorage:ContainerName`.
- Service Bus: `ServiceBus:ConnectionString`, `ServiceBus:DlqQueueName`.
- Redis: `Redis:ConnectionString`, `Redis:ProcessedKeyPrefix`.
- Processing: `Processing:MaxRetryAttempts`, `Processing:RetryInitialDelayMs`, `Processing:BatchSize`.
- Logging: `Logging` section.
- Optional Application Insights: `ApplicationInsights:InstrumentationKey`.

### Example `ProducerApp/appsettings.json`
```json
{
  "Kafka": {
    "BootstrapServers": "<namespace>.servicebus.windows.net:9093",
    "EventHubsConnectionString": "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>",
    "Topic": "<eventhub-name>",
    "MessageTimeoutMs": 30000
  }
}
```

## Producer quick start
1. Update `ProducerApp/appsettings.json` values.
2. Run:
```bash
cd ProducerApp
dotnet run
```

## Consumer quick start
1. Update `ConsumerApp/appsettings.json` with Event Hubs, Blob Storage, Service Bus, and Redis settings.
2. Ensure Blob container for checkpointing exists; the app will create it if missing.
3. Optionally set `ApplicationInsights:InstrumentationKey`.
4. Run locally:
```bash
cd ConsumerApp
dotnet run
```

## Key concepts for the new consumer
- Event processing via Azure SDK `EventProcessorClient` with Blob Storage checkpointing.
- Security: use SAS tokens or connection strings; consider Azure Key Vault for secret management.
- Consumer groups: `EventHubs:ConsumerGroup` aligns with Event Hubs consumer groups.
- Ordering: per-partition ordering preserved; implement idempotency with Redis keys (`ProcessedKeyPrefix`).
- Error handling: transient retries; permanent failures sent to Service Bus DLQ (`DlqQueueName`).
- Observability: optional Application Insights (`AddApplicationInsightsTelemetryWorkerService`), structured logging.
- Health: health checks for Redis and self; extend as needed.
- Graceful shutdown: processor stops and checkpoints; hosted service uses `UseConsoleLifetime`.
- Data contracts: validate payload schema (`ConsumerApp.Domain.MyPayload`).

## Deployment notes
- Containerize and deploy to AKS/ACA/App Service.
- Use managed identity to retrieve secrets from Key Vault.
- Scale via replicas; ensure a single consumer group per app.
- Monitor with Azure Monitor and Application Insights.

## References
- Azure Event Hubs: https://learn.microsoft.com/azure/event-hubs/
- EventProcessorClient: https://learn.microsoft.com/azure/event-hubs/event-processor-client
- Blob Storage SAS: https://learn.microsoft.com/azure/storage/common/storage-sas-overview
- Azure Service Bus: https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-overview
- Application Insights for Worker: https://learn.microsoft.com/azure/azure-monitor/app/worker-service
