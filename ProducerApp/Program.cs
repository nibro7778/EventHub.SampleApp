using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();
var config = builder.Build();

var kafkaCfg = config.GetSection("Kafka");
var bootstrapServers = kafkaCfg["BootstrapServers"]!;
var topic = kafkaCfg["Topic"]!;
var ehConnectionString = kafkaCfg["EventHubsConnectionString"]!;

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

for (int i = 0; i < 10; i++)
{
    var key = Guid.NewGuid().ToString();
    var value = $"Hello {i} @ {DateTimeOffset.UtcNow}";

    try
    {
        var dr = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = value
        });
        Console.WriteLine($"Delivered to {dr.TopicPartitionOffset}");
    }
    catch (ProduceException<string, string> ex)
    {
        Console.WriteLine($"Delivery failed: {ex.Error.Reason}");
    }
}

producer.Flush(TimeSpan.FromSeconds(5));
