using ConsumerApp.Domain;

namespace ConsumerApp.Application;

public interface IPoisonMessageSink
{
    Task StoreAsync(PoisonMessage msg, CancellationToken ct);
}
