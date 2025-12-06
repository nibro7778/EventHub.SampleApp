namespace ConsumerApp.Domain;

public record PoisonMessage
{
    public string Topic { get; init; } = string.Empty;
    public int Partition { get; init; }
    public long Offset { get; init; }
    public string? Key { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Reason { get; init; } = string.Empty;
}
