using Confluent.Kafka;
using ConsumerApp.Application;
using ConsumerApp.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsumerApp.Presentation;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _config;
    private readonly IMessageProcessor _processor;
    private readonly IPoisonMessageSink _poison;
    private readonly IMetricsExporter _metrics;

    public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConfiguration config, IMessageProcessor processor, IPoisonMessageSink poison, IMetricsExporter metrics)
    {
        _logger = logger;
        _config = config;
        _processor = processor;
        _poison = poison;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaCfg = _config.GetSection("Kafka");
        var enableAutoCommit = kafkaCfg.GetValue<bool>("EnableAutoCommit");
        var topic = kafkaCfg["Topic"]!;
        var groupId = kafkaCfg["ConsumerGroup"]!;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaCfg["BootstrapServers"],
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = "$ConnectionString",
            SaslPassword = kafkaCfg["EventHubsConnectionString"],
            GroupId = groupId,
            EnableAutoCommit = enableAutoCommit,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SessionTimeoutMs = kafkaCfg.GetValue<int?>("SessionTimeoutMs"),
            SocketTimeoutMs = kafkaCfg.GetValue<int?>("SocketTimeoutMs"),
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((c, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
            .SetStatisticsHandler((c, json) => _metrics.RecordKafkaStats(json))
            .Build();

        consumer.Subscribe(topic);
        _logger.LogInformation("Subscribed to topic {Topic} in group {GroupId}", topic, groupId);

        var maxInFlight = kafkaCfg.GetValue<int>("MaxInFlight");
        var semaphore = new SemaphoreSlim(maxInFlight);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    _metrics.IncrementConsumed();

                    await semaphore.WaitAsync(stoppingToken);
                    _ = HandleMessageAsync(consumer, cr, enableAutoCommit, semaphore, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected consume loop error");
                }
            }
        }
        finally
        {
            _logger.LogInformation("Shutting down consumer");
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(IConsumer<string, string> consumer, ConsumeResult<string, string> cr, bool enableAutoCommit, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            await _processor.ProcessAsync(cr.Message.Key, cr.Message.Value, ct);
            _metrics.IncrementProcessed();

            if (!enableAutoCommit)
            {
                consumer.Commit(cr);
            }
        }
        catch (TransientProcessingException ex)
        {
            _logger.LogWarning(ex, "Transient processing error. Will retry via re-consume/visibility timeout semantics");
            // Let it be reprocessed by not committing; consider pausing partition or backoff strategies.
        }
        catch (PermanentProcessingException ex)
        {
            _logger.LogError(ex, "Permanent processing error. Quarantining message");
            await _poison.StoreAsync(new PoisonMessage
            {
                Topic = cr.Topic,
                Partition = cr.Partition.Value,
                Offset = cr.Offset.Value,
                Key = cr.Message.Key,
                Payload = cr.Message.Value,
                Timestamp = cr.Message.Timestamp.UtcDateTime,
                Reason = ex.Message
            }, ct);
            // Commit to avoid infinite retry on poison
            if (!enableAutoCommit)
            {
                consumer.Commit(cr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled processing exception");
        }
        finally
        {
            semaphore.Release();
        }
    }
}
