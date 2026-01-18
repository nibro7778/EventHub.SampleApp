using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace AzureEventHub.Config.Client
{
    public class AzureEventHubClient : IEventHubClient
    {
        private readonly EventProcessorClient _client;
        
        public AzureEventHubClient(EventProcessorClient client)
        {
            _client = client;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            await _client.StartProcessingAsync(cancellationToken);
        }

        public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            await _client.StopProcessingAsync(cancellationToken);
        }
    }
}
