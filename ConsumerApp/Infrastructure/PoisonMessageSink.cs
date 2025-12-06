using ConsumerApp.Application;
using ConsumerApp.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsumerApp.Infrastructure;

public class PoisonMessageSink : IPoisonMessageSink
{
    private readonly ILogger<PoisonMessageSink> _logger;
    private readonly IConfiguration _config;

    public PoisonMessageSink(ILogger<PoisonMessageSink> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task StoreAsync(PoisonMessage msg, CancellationToken ct)
    {
        var quarantineCfg = _config.GetSection("Quarantine");
        var useBlob = quarantineCfg.GetValue<bool>("UseBlob");
        if (useBlob)
        {
            var conn = quarantineCfg["BlobConnectionString"]!;
            var container = quarantineCfg["BlobContainer"]!;
            await StoreToBlobAsync(conn, container, msg, ct);
        }
        else
        {
            var path = quarantineCfg["LocalPath"] ?? "poison";
            Directory.CreateDirectory(path);
            var fileName = Path.Combine(path, $"{msg.Topic}-{msg.Partition}-{msg.Offset}.json");
            await File.WriteAllTextAsync(fileName, System.Text.Json.JsonSerializer.Serialize(msg), ct);
            _logger.LogWarning("Poison message stored at {File}", fileName);
        }
    }

    private async Task StoreToBlobAsync(string conn, string container, PoisonMessage msg, CancellationToken ct)
    {
        var client = new Azure.Storage.Blobs.BlobContainerClient(conn, container);
        await client.CreateIfNotExistsAsync(cancellationToken: ct);
        var blob = client.GetBlobClient($"{msg.Topic}/{msg.Partition}/{msg.Offset}.json");
        using var stream = new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(msg));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        _logger.LogWarning("Poison message stored in blob {Name}", blob.Name);
    }
}
