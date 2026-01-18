using AzureEventHub.Config;
using ConsumerApp.Events;

namespace ConsumerApp.Handlers
{
    public sealed class CustomerEventHandler : IEventHandler<CustomerEvent>
    {
        public Task HandleAsync(CustomerEvent message, CancellationToken cancellation)
        {
            Console.WriteLine($"CustomerHandler: Received event: Id={message.Id}, Type={message.Type}, Data={message.Data}");
            return Task.CompletedTask;
        }
    }
}
