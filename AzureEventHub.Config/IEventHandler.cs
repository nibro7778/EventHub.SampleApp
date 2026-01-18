namespace AzureEventHub.Config
{
    public interface IEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent message, CancellationToken cancellation);
    }
}
