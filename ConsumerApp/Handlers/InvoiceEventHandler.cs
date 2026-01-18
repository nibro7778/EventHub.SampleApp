using AzureEventHub.Config;
using ConsumerApp.Events;

namespace ConsumerApp.Handlers
{
    public sealed class InvoiceEventHandler : IEventHandler<InvoiceEvent>
    {
        public Task HandleAsync(InvoiceEvent message, CancellationToken cancellation)
        {
            Console.WriteLine($"InvoiceHandler: Received event: Id={message.Id}, Type={message.Type}, Data={message.Data}");
            return Task.CompletedTask;
        }
    }
}
