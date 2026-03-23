namespace Microservices.Shared.Domain.Configs;

public class AppConfig
{
    public string ConsulServer { get; set; } = "localhost:8500";
    
    public string MarketDataUrl { get; set; } = "";
    public int KlinePeriod { get; set; } = 10;
    
    public int QueueSize { get; set; } = 8 * 1024;

    public KafkaConfig Kafka { get; set; } = new KafkaConfig();
    
    public DbConfig Db { get; set; } = new DbConfig();
    
    public sealed record KafkaConfig
    {
        public string Servers { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "";
        public string GroupKey { get; set; } = "microservices.showcase";
        //TODO security
    }

    public sealed record DbConfig
    {
        public string ConnectionString { get; set; } = "";
        public string TableName { get; set; } = "";

        // for DB-notifications
        public int QueueSize { get; set; } = 8 * 1024;
    }
}
