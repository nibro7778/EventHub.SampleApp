using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ConsumerApp.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace ConsumerApp.Infrastructure
{
    public class DlqService : IDlqService
    {
        private readonly ServiceBusClient _client;
        private readonly string _dlqName;

        public DlqService(ServiceBusClient client, IConfiguration config)
        {
            _client = client;
            _dlqName = config["ServiceBus:DlqQueueName"];
        }

        public async Task SendToDlqAsync(string rawBody, IDictionary<string, object> metadata, Exception ex = null)
        {
            var sender = _client.CreateSender(_dlqName);

            var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(rawBody ?? string.Empty));

            if (metadata != null)
            {
                foreach (var kv in metadata)
                    msg.ApplicationProperties[kv.Key] = kv.Value;
            }

            if (ex != null)
            {
                msg.ApplicationProperties["ProcessingException"] = ex.Message;
                msg.ApplicationProperties["ProcessingStackTrace"] = ex.StackTrace ?? string.Empty;
            }

            await sender.SendMessageAsync(msg);
            await sender.DisposeAsync();
        }
    }
}
