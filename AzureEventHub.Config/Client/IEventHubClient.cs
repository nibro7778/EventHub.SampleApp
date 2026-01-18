namespace AzureEventHub.Config.Client
{
    public interface IEventHubClient
    {
        Task StartProcessingAsync(CancellationToken cancellationToken = default);
        Task StopProcessingAsync(CancellationToken cancellationToken = default);
    }
}
