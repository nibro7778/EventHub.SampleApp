using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using ConsumerApp.Application;
using ConsumerApp.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ConsumerApp.Presentation
{
    public class EventHubProcessorService : IHostedService
    {
        private readonly ILogger<EventHubProcessorService> _logger;
        private readonly IConfiguration _config;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly EventProcessorClient _processor;
        private readonly BusinessProcessor _businessProcessor;
        private readonly DlqService _dlq;
        private readonly int _maxRetries;
        private readonly AsyncRetryPolicy _retryPolicy;

        public EventHubProcessorService(
            ILogger<EventHubProcessorService> logger,
            IConfiguration config,
            BlobContainerClient blobContainerClient,
            BusinessProcessor businessProcessor,
            DlqService dlq)
        {
            _logger = logger;
            _config = config;
            _blobContainerClient = blobContainerClient;
            _businessProcessor = businessProcessor;
            _dlq = dlq;

            var ehConnection = _config["EventHubs:ConnectionString"];
            var eventHubName = _config["EventHubs:EventHubName"];
            var consumerGroup = _config["EventHubs:ConsumerGroup"] ?? EventHubConsumerClient.DefaultConsumerGroupName;

            var processorOptions = new EventProcessorClientOptions
            {
                // Tune as required
                MaximumWaitTime = TimeSpan.FromSeconds(30)
            };

            _processor = new EventProcessorClient(_blobContainerClient, consumerGroup, ehConnection, eventHubName, processorOptions);

            _processor.ProcessEventAsync += ProcessEventHandler;
            _processor.ProcessErrorAsync += ProcessErrorHandler;

            _maxRetries = int.TryParse(config["Processing:MaxRetryAttempts"], out var m) ? m : 3;
            var initialDelayMs = int.TryParse(config["Processing:RetryInitialDelayMs"], out var d) ? d : 500;

            _retryPolicy = Policy
                .Handle<Exception>(ex => IsTransient(ex))
                .WaitAndRetryAsync(_maxRetries, attempt => TimeSpan.FromMilliseconds(initialDelayMs * Math.Pow(2, attempt - 1)),
                    onRetry: (ex, ts, retryCount, ctx) =>
                    {
                        _logger.LogWarning(ex, "Transient error on attempt {Attempt}. Waiting {Wait}.", retryCount, ts);
                    });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Event Processor...");
            await _processor.StartProcessingAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Event Processor...");
            await _processor.StopProcessingAsync(cancellationToken);
        }

        private async Task ProcessEventHandler(ProcessEventArgs args)
        {
            var eventData = args.Data;
            if (eventData == null) return;

            var rawBody = Encoding.UTF8.GetString(eventData.Body.ToArray());
            var seq = eventData.SequenceNumber;
            var partitionId = args.Partition.PartitionId;
            var messageId = !string.IsNullOrEmpty(eventData.MessageId)
                            ? eventData.MessageId
                            : (eventData.Properties != null && eventData.Properties.ContainsKey("messageId")
                                ? eventData.Properties["messageId"]?.ToString()
                                : null);

            _logger.LogInformation("Received event. Partition: {Partition} Seq: {Seq} MessageId: {MessageId}", partitionId, seq, messageId);

            Exception lastEx = null;
            bool processed = false;

            try
            {
                // Use retry policy for transient errors
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _businessProcessor.ProcessAsync(rawBody); // may throw
                    processed = true;
                });
            }
            catch (Exception ex)
            {
                lastEx = ex;
                _logger.LogError(ex, "Processing failed for Partition={Partition} Seq={Seq}", partitionId, seq);
            }

            if (processed)
            {
                // safe to checkpoint once processed
                try
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                    _logger.LogInformation("Checkpoint updated for partition {Partition} seq {Seq}", partitionId, seq);
                }
                catch (Exception chkEx)
                {
                    _logger.LogError(chkEx, "Failed to checkpoint Partition={Partition} Seq={Seq}", partitionId, seq);
                }
            }
            else
            {
                // non-transient/following retries — move to DLQ with metadata and checkpoint past it
                var metadata = new Dictionary<string, object>
                {
                    ["PartitionId"] = partitionId,
                    ["SequenceNumber"] = seq,
                    ["MessageId"] = messageId ?? string.Empty
                };

                await _dlq.SendToDlqAsync(rawBody, metadata, lastEx);

                try
                {
                    await args.UpdateCheckpointAsync(args.CancellationToken);
                    _logger.LogInformation("Checkpointed past failed message Partition={Partition} Seq={Seq}", partitionId, seq);
                }
                catch (Exception chkEx)
                {
                    _logger.LogError(chkEx, "Failed to checkpoint after DLQ Partition={Partition} Seq={Seq}", partitionId, seq);
                }
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs args)
        {
            // This captures errors in the EventProcessor pipeline like ownership/partition errors
            _logger.LogError(args.Exception, "EventProcessor Error. Operation: {Operation} PartitionId: {PartitionId}", args.Operation, args.PartitionId);
            // Add alerting logic here e.g. send to monitoring/alerting pipeline
            return Task.CompletedTask;
        }

        private bool IsTransient(Exception ex)
        {
            // Interpret what you consider transient: network, timeout, throttling etc.
            // Example simplistic:
            if (ex is TaskCanceledException) return true;
            if (ex is OperationCanceledException) return true;
            if (ex.Message.Contains("429") || ex.Message.Contains("throttl") || ex.Message.Contains("Timeout")) return true;
            // expand with specific exception types from downstream libs
            return false;
        }
    }
}
