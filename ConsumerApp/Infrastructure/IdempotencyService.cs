using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsumerApp.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ConsumerApp.Infrastructure
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly string _prefix;

        public IdempotencyService(IConnectionMultiplexer redis, IConfiguration config)
        {
            _redis = redis;
            _prefix = config["Redis:ProcessedKeyPrefix"] ?? "processed:";
        }

        // Returns true if already processed.
        public async Task<bool> IsAlreadyProcessedAsync(string messageId)
        {
            if (string.IsNullOrEmpty(messageId)) return false;
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync($"{_prefix}{messageId}");
        }

        // Marks processed; returns true if successfully set (i.e., not existed before)
        public async Task<bool> MarkProcessedAsync(string messageId, TimeSpan? ttl = null)
        {
            if (string.IsNullOrEmpty(messageId)) return false;
            var db = _redis.GetDatabase();
            var key = $"{_prefix}{messageId}";
            var expiry = ttl ?? TimeSpan.FromDays(7);
            // Set if not exists
            return await db.StringSetAsync(key, "1", expiry, When.NotExists);
        }
    }
}
