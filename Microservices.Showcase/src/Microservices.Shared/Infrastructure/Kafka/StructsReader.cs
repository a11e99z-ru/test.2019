using System.Runtime.InteropServices;
using System.Text;
using Confluent.Kafka;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Microservices.Shared.Infrastructure.Kafka;

public sealed class StructsReader<T>
    : BackgroundService
    , IDeserializer<T[]>
    , IDeserializer<string>
    where T : unmanaged
{
    private static readonly string TypeName = typeof(T).Name;
    private readonly ILogger _logger = Log.ForContext<StructsReader<T>>();
    private readonly AppConfig.KafkaConfig _config;
    private readonly IAsyncPublisher<T[]> _publisher;
    private volatile IConsumer<string, T[]>? _consumer;

    public StructsReader(IOptions<AppConfig.KafkaConfig> options,
        IAsyncPublisher<T[]> publisher)
    {
        _config = options.Value;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.Information("StructsReader<{type}> is starting...", TypeName);
        try
        {
            while (!ct.IsCancellationRequested)
            {
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in StructsReader<{type}>", TypeName);
        }
        _logger.Warning("StructsReader<{type}> is stopped.", TypeName);
    }

    T[] IDeserializer<T[]>.Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? null
            : MemoryMarshal.Cast<byte, T>(data).ToArray();

    string IDeserializer<string>.Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? null : string.Empty; // ignore the Key
}
