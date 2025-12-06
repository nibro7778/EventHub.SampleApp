namespace ConsumerApp.Application;

public interface IMetricsExporter
{
    void IncrementConsumed();
    void IncrementProcessed();
    void RecordKafkaStats(string json);
}
