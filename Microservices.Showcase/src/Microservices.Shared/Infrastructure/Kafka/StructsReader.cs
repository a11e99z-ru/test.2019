using System.Runtime.InteropServices;
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
            var config = new ConsumerConfig
            {
                BootstrapServers = _config.Servers,
                GroupId = _config.GroupKey,
                EnableAutoCommit = true
                //TODO security
            };
            
            using var consumer = new ConsumerBuilder<string, T[]>(config)
                .SetKeyDeserializer(this)
                .SetValueDeserializer(this)
                .SetErrorHandler((_, err)
                    => _logger.Error("Error in StructsReader<{type}>.Kafka: {err}", TypeName, err))
#if DEBUG
                .SetLogHandler((_, lm) =>
                {
                    switch (lm.Level)
                    {
                        case <= SyslogLevel.Debug:
                            _logger.Debug("StructsReader<{type}>.Kafka: {msg}", TypeName, lm.Message);
                            break;
                        default:
                            _logger.Information("StructsReader<{type}>.Kafka: {msg}", TypeName, lm.Message);
                            break;
                    }
                })
#endif
                .Build();
            consumer.Subscribe(_config.Topic);

            while (!ct.IsCancellationRequested)
            {
                var recv = consumer.Consume(ct);
                if (recv?.Message?.Value is T[] data)
                    await _publisher.PublishAsync(data, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in StructsReader<{type}>", TypeName);
        }
        _logger.Warning("StructsReader<{type}> is stopped.", TypeName);
    }

#pragma warning disable CS8768
    T[]? IDeserializer<T[]>.Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? null : Deserialize(data);

    string? IDeserializer<string>.Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        => isNull ? null : string.Empty; // ignore the Key

    public static T[] Deserialize(ReadOnlySpan<byte> data)
        => MemoryMarshal.Cast<byte, T>(data).ToArray();
}
