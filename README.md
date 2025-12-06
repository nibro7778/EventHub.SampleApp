Azure Event Hubs (Kafka endpoint) .NET sample: Consumer and Producer

Overview
- Sample solution demonstrating a resilient .NET consumer and a simple producer using `Confluent.Kafka` against Azure Event Hubs Kafka endpoint.
- Focuses on production readiness for consumer: configuration, security, retries, error handling, poison-message strategy, graceful shutdown, observability, and deployment.

Projects
- ConsumerApp (net8.0): Background service consuming from Event Hubs via Kafka protocol.
- ProducerApp (net8.0): Console app producing sample messages.

Prerequisites
- Azure Event Hubs instance with Kafka endpoint enabled.
- .NET 8 SDK.
- Connection string with appropriate rights.

Configuration
- Common settings (appsettings.json or environment variables):
  - Kafka: `BootstrapServers`, `EventHubsConnectionString`, `Topic`, `ConsumerGroup` (consumer), `EnableAutoCommit`, `MaxInFlight`.
  - Producer: optional `MessageTimeoutMs`.
  - Retry: `MaxAttempts`, `InitialDelayMs`, `MaxDelayMs`.
  - Quarantine: Blob/Cosmos or file path.

Producer quick start
- Update `ProducerApp/appsettings.json`:
  - `BootstrapServers`: `<namespace>.servicebus.windows.net:9093`
  - `EventHubsConnectionString`: `Endpoint=sb://<namespace>.servicebus.windows.net/...`
  - `Topic`: `<eventhub-name>`
- Run:
  - `cd ProducerApp`
  - `dotnet run`

Consumer quick start
- Update `ConsumerApp/appsettings.json` with Kafka section and consumer group.
- Run locally:
  - `cd ConsumerApp`
  - `dotnet run`

Key concepts for production-ready consumer
- Connectivity: Event Hubs Kafka endpoint (SASL/SSL). Username `$ConnectionString`, password is Event Hubs connection string.
- Security: TLS 1.2, rotating secrets, consider Key Vault.
- Consumer groups: Map to Event Hubs consumer groups.
- Partitions: Scale with group replicas.
- Offset management: Auto-commit or explicit commit after successful processing.
- Idempotency & ordering: Per-partition ordering; make handlers idempotent.
- Error handling: Retries/backoff; classify transient vs permanent; poison-message quarantine.
- Backpressure: Limit max in-flight; optionally pause partitions.
- Observability: Structured logs, metrics; emit lag metrics.
- Health: Liveness/readiness endpoints.
- Graceful shutdown: Finish in-flight, commit offsets.
- Resilience: Circuit breakers around downstream deps.
- Data contracts: Validate schema and versioning.

Deployment notes
- Containerize and deploy to AKS/ACA/App Service.
- Use managed identity to retrieve secrets from Key Vault.
- Scale via replicas; ensure single consumer group per app.
- Monitor lag with Kafka metrics and Azure Monitor.

References
- Azure Event Hubs Kafka endpoint: https://learn.microsoft.com/azure/event-hubs/event-hubs-for-kafka-ecosystem-overview
- Confluent.Kafka client: https://github.com/confluentinc/confluent-kafka-dotnet
- Event Hubs auth for Kafka: https://learn.microsoft.com/azure/event-hubs/event-hubs-quickstart-kafka-dotnet
