namespace AzureEventHub.Config.Configuration
{
    // Options class for configuring Event Hub Rider settings
    public class EventHubConfiguration
    {
        public bool UseInMemoryClient { get; set; }
        public string? Namespace { get; set; }        
        public string? StorageAccount { get; set; }
        public string? BlobContainer { get; set; }
    }

    public class EventHub
    {
        public string EventHubName { get; set; } = string.Empty;
        public string ConsumerGroup { get; set; } = "$Default";
        public Type? EventType { get; set; }
        public Type? HandlerType { get; set; }
    }
}
