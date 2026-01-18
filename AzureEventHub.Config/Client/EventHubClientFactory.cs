using System.Text.Json;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using AzureEventHub.Config.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureEventHub.Config.Client
{
    // Creates clients per configured event hub; dispatches messages to registered IEventHandler<T> implementations.
    public sealed class EventHubClientFactory
    {
        private readonly EventHubConfiguration _configuration;
        private readonly TokenCredential _credential;
        private readonly EventHubSubscription _subscription;
        private readonly IServiceProvider _sp;
        private readonly ILogger<EventHubClientFactory> _logger;

        public EventHubClientFactory(
            EventHubConfiguration options,
            TokenCredential credential,
            IServiceProvider sp,
            ILogger<EventHubClientFactory> logger)
        {
            _configuration = options;
            _credential = credential;
            _sp = sp;
            _logger = logger;
            _subscription = sp.GetService<EventHubSubscription>() ?? new EventHubSubscription();
        }

        public IReadOnlyList<IEventHubClient> CreateClients()
        {
            if (_configuration.UseInMemoryClient)
            {
                _logger.LogInformation("Using in-memory Event Hub clients.");
                return _subscription.EventHubs
                    .Select(_ => (IEventHubClient)new InMemoryEventHubClient())
                    .ToList();
            }

            if (string.IsNullOrWhiteSpace(_configuration.BlobContainer))
            {
                throw new InvalidOperationException("Blob container name must be configured in EventHubRiderOptions.BlobContainer.");
            }
            if (string.IsNullOrWhiteSpace(_configuration.Namespace))
            {
                throw new InvalidOperationException("Fully qualified namespace must be configured in EventHubRiderOptions.Namespace.");
            }
            if (string.IsNullOrWhiteSpace(_configuration.StorageAccount))
            {
                throw new InvalidOperationException("Storage account must be configured in EventHubRiderOptions.StorageAccount.");
            }

            var storageAccountUrl = _configuration.StorageAccount.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? _configuration.StorageAccount
                : $"https://{_configuration.StorageAccount}.blob.core.windows.net";

            var blobServiceClient = new BlobServiceClient(new Uri(storageAccountUrl), _credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(_configuration.BlobContainer);
            containerClient.CreateIfNotExists();

            var regs = _subscription.EventHubs ?? new List<EventHub>();
            var clients = new List<IEventHubClient>(regs.Count);
            foreach (var reg in regs)
            {
                var processorOptions = new EventProcessorClientOptions  
                {
                    MaximumWaitTime = TimeSpan.FromSeconds(30)
                };
                var processor = new EventProcessorClient(
                    containerClient,
                    reg.ConsumerGroup,
                    _configuration.Namespace!,
                    reg.EventHubName,
                    _credential,
                    processorOptions);

                processor.ProcessEventAsync += args => DispatchEventAsync(args, reg.EventType!);
                processor.ProcessErrorAsync += args =>
                {
                    _logger.LogError(args.Exception, "EventProcessor error on partition {PartitionId}", args.PartitionId);
                    return Task.CompletedTask;
                };

                clients.Add(new AzureEventHubClient(processor));
            }

            return clients;
        }

        private async Task DispatchEventAsync(ProcessEventArgs args, Type eventType)
        {
            try
            {
                if (!args.HasEvent)
                {
                    return; // nothing to process
                }

                if (eventType == null)
                {
                    _logger.LogError("No event type configured for Event Hub {EventHub} in consumer group {ConsumerGroup}", args.Partition.EventHubName, args.Partition.ConsumerGroup);
                    return;
                }                

                var body = args.Data.EventBody.ToString();
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var message = JsonSerializer.Deserialize(body, eventType, serializerOptions);
                if (message == null)
                {
                    _logger.LogError("Failed to deserialize message on partition {PartitionId}", args.Partition.PartitionId);
                    return;
                }

                // attach raw json if the event derives from EventBase
                if (message is EventBase baseEvent)
                {
                    baseEvent.RawJson = body;
                }

                var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
                object? handler;
                try
                {
                    handler = _sp.GetService(handlerType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving handler for type {EventType}", eventType.FullName);
                    return;
                }

                if (handler == null)
                {
                    // Try to instantiate a handler if a mapping was configured via ReceiveEndpoint
                    var reg = _subscription.EventHubs.FirstOrDefault(r => r.EventType == eventType);
                    if (reg?.HandlerType != null)
                    {
                        try
                        {
                            handler = Activator.CreateInstance(reg.HandlerType);
                        }
                        catch (Exception createEx)
                        {
                            _logger.LogError(createEx, "Failed to instantiate handler type {HandlerType}", reg.HandlerType.FullName);
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogError("No handler registered for event type {EventType}", eventType.FullName);
                        return;
                    }
                }

                var handleMethod = handlerType.GetMethod(nameof(IEventHandler<object>.HandleAsync));
                if (handleMethod == null)
                {
                    _logger.LogError("HandleAsync method not found for handler of type {HandlerType}", handler.GetType().FullName);
                    return;
                }

                await (Task)handleMethod.Invoke(handler, new[] { message, args.CancellationToken })!;
                await args.UpdateCheckpointAsync(args.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event from partition {PartitionId}", args.Partition.PartitionId);                
            }
        }
    }
}
