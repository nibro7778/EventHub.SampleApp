namespace ConsumerApp.Application.Abstractions
{
    public interface IIdempotencyService
    {
        Task<bool> IsAlreadyProcessedAsync(string messageId);
        Task<bool> MarkProcessedAsync(string messageId, TimeSpan? ttl = null);
    }
}
