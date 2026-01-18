using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AzureEventHub.Config.Client;

namespace AzureEventHub.Config.Hosting
{
    public class EventHubHostedService(EventHubClientFactory factory, ILogger<EventHubHostedService> logger) : IHostedService
    {
        private readonly EventHubClientFactory _factory = factory;
        private readonly ILogger<EventHubHostedService> _logger = logger;
        private IReadOnlyList<IEventHubClient> _clients = Array.Empty<IEventHubClient>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _clients = _factory.CreateClients();
            foreach (var client in _clients)
            {                
                _logger.LogInformation("Starting Event Hub client");
                await client.StartProcessingAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var client in _clients)
            {
                await client.StopProcessingAsync(cancellationToken);
            }           
        }
    }
}
