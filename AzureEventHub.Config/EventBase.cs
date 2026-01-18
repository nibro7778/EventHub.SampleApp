namespace AzureEventHub.Config
{
    // Base type for events to carry the original raw JSON payload.
    public abstract class EventBase
    {
        public string? RawJson { get; internal set; }
    }
}
