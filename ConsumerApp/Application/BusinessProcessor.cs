using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsumerApp.Application.Abstractions;
using ConsumerApp.Domain;
using ConsumerApp.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ConsumerApp.Application
{
    public class BusinessProcessor : IBusinessProcess
    {
        private readonly ILogger<BusinessProcessor> _logger;
        private readonly IdempotencyService _idempotency;

        public BusinessProcessor(ILogger<BusinessProcessor> logger, IdempotencyService idempotency)
        {
            _logger = logger;
            _idempotency = idempotency;
        }

        public async Task ProcessAsync(string rawBody)
        {
            // Example deserialize; handle schema/versioning here
            var payload = JsonSerializer.Deserialize<MyPayload>(rawBody);
            if (payload == null) throw new InvalidOperationException("Failed to deserialize payload");

            // Idempotency guard
            var messageId = payload.Id ?? Guid.NewGuid().ToString();
            var already = await _idempotency.IsAlreadyProcessedAsync(messageId);
            if (already)
            {
                _logger.LogInformation("Message {MessageId} already processed. Skipping.", messageId);
                return;
            }

            // Simulate business operation
            await DoBusinessWorkAsync(payload);

            // Mark processed
            await _idempotency.MarkProcessedAsync(messageId);
        }

        private Task DoBusinessWorkAsync(MyPayload payload)
        {
            _logger.LogInformation("Processing payload {Id} Type={Type}", payload.Id, payload.Type);
            // TODO: call downstream services / DB / etc.
            return Task.CompletedTask;
        }
    }
}
