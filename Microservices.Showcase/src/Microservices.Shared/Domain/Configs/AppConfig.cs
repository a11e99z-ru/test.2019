namespace Microservices.Shared.Domain.Configs;

public class AppConfig
{
    public int QueueSize { get; set; } = 8 * 1024;

    public sealed record KafkaConfig
    {
        public string Servers { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "";
        public string GroupKey { get; set; } = "";
        
        public string User { get; set; } = "";
        public string Pwd { get; set; } = "";
        public string SaslMode { get; set; } = "";
        public string SaslProto { get; set; } = "";
    }
}
