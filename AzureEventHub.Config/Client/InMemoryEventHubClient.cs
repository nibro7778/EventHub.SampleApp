namespace AzureEventHub.Config.Client
{
    public class InMemoryEventHubClient : IEventHubClient
    {
        public Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopProcessingAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
