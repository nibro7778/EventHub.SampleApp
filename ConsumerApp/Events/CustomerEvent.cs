using AzureEventHub.Config;

namespace ConsumerApp.Events
{
    public class CustomerEvent : EventBase
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Data { get; set; }
    }
}
