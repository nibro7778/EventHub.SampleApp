# Azure Event Hubs (Kafka) .NET Sample: Consumer and Producer

## Overview
- Sample solution demonstrating a resilient .NET consumer and a simple producer using `Confluent.Kafka` against Azure Event Hubs Kafka endpoint.
- Focus on production readiness for the consumer: configuration, security, retries, error handling, poison-message strategy, graceful shutdown, observability, and deployment.

## Projects
- `ConsumerApp` (net8.0): Background service consuming from Event Hubs via Kafka protocol.
- `ProducerApp` (net8.0): Console app producing sample messages.

## Prerequisites
- Azure Event Hubs instance with Kafka endpoint enabled.
- .NET 8 SDK.
- Event Hubs connection string with appropriate rights.

## Configuration
Common settings via `appsettings.json` or environment variables:
- Kafka: `BootstrapServers`, `EventHubsConnectionString`, `Topic`, `ConsumerGroup` (consumer), `EnableAutoCommit`, `MaxInFlight`.
- Producer: optional `MessageTimeoutMs`.
- Retry: `MaxAttempts`, `InitialDelayMs`, `MaxDelayMs`.
- Quarantine: Blob/Cosmos or file path.

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
1. Update `ConsumerApp/appsettings.json` with the Kafka section and consumer group.
2. Run locally:
```bash
cd ConsumerApp
dotnet run
```

## Key concepts for a production-ready consumer
- Connectivity: Event Hubs Kafka endpoint (SASL/SSL). Username `$ConnectionString`, password is the Event Hubs connection string.
- Security: TLS 1.2, rotate secrets, consider Key Vault for secret management.
- Consumer groups: Map to Event Hubs consumer groups.
- Partitions: Scale consumers horizontally with replicas in the same group.
- Offset management: Auto-commit or explicit commit after successful processing.
- Idempotency and ordering: Per-partition ordering; make handlers idempotent.
- Error handling: Retries with backoff; classify transient vs permanent; poison-message quarantine.
- Backpressure: Limit max in-flight; optionally pause/resume partitions.
- Observability: Structured logs and metrics; emit lag metrics.
- Health: Liveness/readiness endpoints.
- Graceful shutdown: Finish in-flight work and commit offsets.
- Resilience: Circuit breakers around downstream dependencies.
- Data contracts: Validate schema and versioning.

## Deployment notes
- Containerize and deploy to AKS/ACA/App Service.
- Use managed identity to retrieve secrets from Key Vault.
- Scale via replicas; ensure a single consumer group per app.
- Monitor lag with Kafka metrics and Azure Monitor.

## References
- Azure Event Hubs Kafka endpoint: https://learn.microsoft.com/azure/event-hubs/event-hubs-for-kafka-ecosystem-overview
- Confluent.Kafka client: https://github.com/confluentinc/confluent-kafka-dotnet
- Event Hubs auth for Kafka: https://learn.microsoft.com/azure/event-hubs/event-hubs-quickstart-kafka-dotnet
