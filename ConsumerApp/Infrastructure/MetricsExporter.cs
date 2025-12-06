using ConsumerApp.Application;

namespace ConsumerApp.Infrastructure;

public class MetricsExporter : IMetricsExporter
{
    private long _consumed;
    private long _processed;
    public void IncrementConsumed() => Interlocked.Increment(ref _consumed);
    public void IncrementProcessed() => Interlocked.Increment(ref _processed);
    public void RecordKafkaStats(string json) { /* expose or log stats */ }
}
