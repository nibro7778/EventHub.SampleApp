namespace AzureEventHub.Config.Configuration
{
    public class EventHubSubscription
    {
        public List<EventHub> EventHubs { get; } = new();
    }
}
