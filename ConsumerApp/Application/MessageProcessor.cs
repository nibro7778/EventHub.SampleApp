using ConsumerApp.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsumerApp.Application;

public class MessageProcessor : IMessageProcessor
{
    private readonly ILogger<MessageProcessor> _logger;
    private readonly IConfiguration _config;

    public MessageProcessor(ILogger<MessageProcessor> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task ProcessAsync(string? key, string payload, CancellationToken ct)
    {
        var retryCfg = _config.GetSection("Retry");
        var attempts = retryCfg.GetValue<int>("MaxAttempts");
        var delayMs = retryCfg.GetValue<int>("InitialDelayMs");
        var maxDelayMs = retryCfg.GetValue<int>("MaxDelayMs");

        int tryNo = 0;
        for (;;)
        {
            try
            {
                // TODO: validate schema, idempotency check, business logic
                _logger.LogInformation("Processing key={Key} payload length={Len}", key, payload.Length);
                await Task.Delay(10, ct); // simulate work
                return;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                tryNo++;
                if (tryNo >= attempts)
                {
                    throw new PermanentProcessingException("Max attempts reached", ex);
                }
                var nextDelay = Math.Min(delayMs * (int)Math.Pow(2, tryNo - 1), maxDelayMs);
                _logger.LogWarning(ex, "Transient failure, attempt {Attempt}, delaying {Delay}ms", tryNo, nextDelay);
                await Task.Delay(nextDelay, ct);
            }
            catch (Exception ex)
            {
                throw new PermanentProcessingException("Permanent failure", ex);
            }
        }
    }

    private bool IsTransient(Exception ex)
    {
        return ex is TimeoutException || ex is OperationCanceledException || ex is HttpRequestException;
    }
}
