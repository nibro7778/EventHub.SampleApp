namespace ConsumerApp.Application;

public interface IMessageProcessor
{
    Task ProcessAsync(string? key, string payload, CancellationToken ct);
}
