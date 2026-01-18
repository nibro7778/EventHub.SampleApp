using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();
var config = builder.Build();

var kafkaCfg = config.GetSection("Kafka");
var bootstrapServers = "kafaka-test.servicebus.windows.net:9093";
var topic = "customer.created";
var ehConnectionString = "Endpoint=sb://kafaka-test.servicebus.windows.net/;SharedAccessKeyName=eventhub-producer;SharedAccessKey=I+4m3VTtbhowVlZTY0hyNAF67ujrjKxYJ+AEhA264Aw=";

var producerConfig = new ProducerConfig
{
    BootstrapServers = bootstrapServers,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslMechanism = SaslMechanism.Plain,
    SaslUsername = "$ConnectionString",
    SaslPassword = ehConnectionString,
    // Optional tuning
    Acks = Acks.All,
    MessageTimeoutMs = kafkaCfg.GetValue<int?>("MessageTimeoutMs"),
};

using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

Console.WriteLine($"Producing to {topic} on {bootstrapServers}");

for (int i = 0; i < 1; i++)
{
    var id = Guid.NewGuid().ToString();
    var evt = new CustomerEvent(
        Id: id,
        Type: "customer.created",
        Data: $"Hello {i} @ {DateTimeOffset.UtcNow}");

    var payload = JsonSerializer.Serialize(evt);

    try
    {
        var dr = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = id,        // partition key
            Value = payload  // JSON matching consumer schema
        });
        Console.WriteLine($"Delivered {id} to {dr.TopicPartitionOffset}");
    }
    catch (ProduceException<string, string> ex)
    {
        Console.WriteLine($"Delivery failed: {ex.Error.Reason}");
    }
}

topic = "invoice.created";
Console.WriteLine($"Producing to {topic} on {bootstrapServers}");

for (int i = 0; i < 1; i++)
{
    var id = Guid.NewGuid().ToString();
    var evt = new InvoiceEvent(
        Id: id,
        Type: "invoice.created",
        Data: $"Hello {i} @ {DateTimeOffset.UtcNow}");

    var payload = JsonSerializer.Serialize(evt);

    try
    {
        var dr = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = id,        // partition key
            Value = payload  // JSON matching consumer schema
        });
        Console.WriteLine($"Delivered {id} to {dr.TopicPartitionOffset}");
    }
    catch (ProduceException<string, string> ex)
    {
        Console.WriteLine($"Delivery failed: {ex.Error.Reason}");
    }
}

producer.Flush(TimeSpan.FromSeconds(5));

// Mirror of ConsumerApp.Domain.CustomerEvent
public record CustomerEvent(string Id, string Type, string Data);
public record InvoiceEvent(string Id, string Type, string Data);
