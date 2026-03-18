using Confluent.Kafka;

namespace Microservices.Shared.Infrastructure.Kafka;

public sealed class SkipStringKeySerde
    : ISerializer<string>
    , IDeserializer<string>
{
    public byte[] Serialize(string data, SerializationContext context)
        => [];
    
    public string Deserialize(byte[] data, SerializationContext context)
        => string.Empty;

    public string Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? null : string.Empty;
}
