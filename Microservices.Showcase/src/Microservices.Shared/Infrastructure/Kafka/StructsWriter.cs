using System.Runtime.InteropServices;
using System.Text;
using Confluent.Kafka;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Microservices.Shared.Infrastructure.Kafka;

public sealed class StructsWriter<T>
    : BackgroundService
    , IAsyncPublisher<T[]>
    , ISerializer<T[]>
    , ISerializer<string>
    where T : unmanaged
{
    private static readonly string TypeName = typeof(T).Name;
    private readonly ILogger _logger = Log.ForContext<StructsWriter<T>>();
    private readonly AppConfig.KafkaConfig _config;
    private readonly byte[] _keyMessage;
    private volatile IProducer<string, T[]>? _producer;

    public StructsWriter(IOptions<AppConfig.KafkaConfig> options)
    {
        _config = options.Value;
        _keyMessage = Encoding.UTF8.GetBytes(_config.GroupKey);
    }

    public async ValueTask PublishAsync(T[] value, CancellationToken ct)
    {
        var msg = new Message<string, T[]>() { Key = _config.GroupKey, Value = value };
        if (_producer is { } prod)
            await prod.ProduceAsync(_config.Topic, msg, ct);
        else
            _logger.Warning("StructsWriter<{type}>.PublishAsync: Kafka-producer is not ready yet. Ignoring item.", TypeName);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.Information("StructsWriter<{type}> is starting...", TypeName);
        try
        {
            
            
            while (!ct.IsCancellationRequested)
            {
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in StructsWriter<{type}>", TypeName);
        }
        _logger.Warning("StructsWriter<{type}> is stopped.", TypeName);
    }

    public byte[] Serialize(T[] data, SerializationContext context)
        => MemoryMarshal.AsBytes(data).ToArray();

    public byte[] Serialize(string data, SerializationContext context)
        => _keyMessage;
}
