# EventHub.SampleApp

Code-first Azure Event Hubs consumer/producer sample targeting .NET 8.

## Projects
- ConsumerApp: sample consumer
- ProducerApp: sample producer
- AzureEventHub.Config: shared library (configuration, subscriptions, clients, hosting)

## Code-first configuration
Endpoints are configured in code; no `EventHubs` array in appsettings.

```csharp
services.AddEventHub(k =>
{
    k.Namespace(config["EventHubRider:Namespace"]);
    k.Storage(config["EventHubRider:StorageAccount"]);
    k.BlobContainer(config["EventHubRider:BlobContainer"]);

    k.ReceiveEndpoint("customer.created", "sampleapp", c =>
    {
        c.ConfigureConsumer<CustomerEventHandler>(context);
    });

    k.ReceiveEndpoint("invoice.created", "sampleapp", c =>
    {
        c.ConfigureConsumer<InvoiceEventHandler>(context);
    });
});
```

Library types:
- Configuration: `EventHubConfiguration`
- Subscriptions: `EventHubSubscription`, `EventHub`
- Clients: `EventHubClientFactory`, `AzureEventHubClient`, `InMemoryEventHubClient`, `IEventHubClient`
- Hosting: `EventHubHostedService`

Handler resolution:
- Prefer DI registration of handlers.
- If not in DI, handlers mapped via `ConfigureConsumer<T>` will be constructed.

## Secrets and GitHub push protection
Do not commit connection strings or SAS keys.
- Use environment variables or Azure Key Vault.
- Add appsettings with secrets to `.gitignore` and remove from Git index.
- If blocked by push protection, scrub secrets from history with `git filter-repo` or BFG, then rotate the key.

## Requirements
- .NET 8
- Azure SDK packages: Event Hubs Processor, Blob Storage, Identity

## Azure resources and permissions
Provision and grant access for:
- Azure Event Hubs
  - Namespace and Event Hub(s)
  - Consumer groups used by the app (e.g., "sampleapp")
  - Authentication: Managed Identity or Azure AD app with role assignments
    - Roles: Azure Event Hubs Data Receiver for the consumer
               Azure Event Hubs Data Sender for the producer
- Azure Storage (Blob)
  - Storage account and blob container for checkpoints
  - Authentication: Managed Identity or Azure AD app
    - Roles: Storage Blob Data Contributor (for checkpoints)
- Optional: Azure Key Vault for secrets
  - Authentication: Managed Identity or Azure AD app
    - Access policy/roles: get/list secrets

Minimum role assignments (recommended with Managed Identity):
- ConsumerApp identity
  - Azure Event Hubs Data Receiver on the Event Hubs namespace or specific Event Hub
  - Storage Blob Data Contributor on the storage account
- ProducerApp identity (if used)
  - Azure Event Hubs Data Sender on the Event Hubs namespace or specific Event Hub
- Configuration/secrets
  - Key Vault Secrets User on the Key Vault (if using KV)
