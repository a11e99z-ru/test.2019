using System.Reflection;
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
    : IHostedService
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
            _logger.Warning("StructsWriter<{type}>.PublishAsync: Kafka-producer is not ready yet. Ignoring item.",
                TypeName);
    }

    public Task StartAsync(CancellationToken ct)
    {
        try
        {
            var cfgKafka = new ProducerConfig
            {
                BootstrapServers = _config.Servers,
                ClientId = Assembly.GetEntryAssembly()!.GetName().Name,
                MessageTimeoutMs = 1_000,
                MessageSendMaxRetries = 0,
                AllowAutoCreateTopics = true,
                LingerMs = 50,
                BatchSize = 10,
                //TODO security
            };
            
            var prod = new ProducerBuilder<string, T[]>(cfgKafka)
                .SetKeySerializer(this)
                .SetValueSerializer(this)
                .SetErrorHandler((_, err)
                    => _logger.Error("Error in StructsWriter<{type}>.Kafka: {err}", TypeName, err))
#if DEBUG
                .SetLogHandler((_, lm) =>
                {
                    switch (lm.Level)
                    {
                        case <= SyslogLevel.Debug:
                            _logger.Debug("StructsWriter<{type}>.Kafka: {msg}", TypeName, lm.Message);
                            break;
                        default:
                            _logger.Information("StructsWriter<{type}>.Kafka: {msg}", TypeName, lm.Message);
                            break;
                    }
                })
#endif
                .Build();
            Interlocked.Exchange(ref _producer, prod);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in StructsWriter<{type}>", TypeName);
            throw;
        }

        _logger.Information("StructsWriter<{type}> is started.", TypeName);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        Interlocked.Exchange(ref _producer, null)?.Dispose();
        _logger.Warning("StructsWriter<{type}> is stopped.", TypeName);
        return Task.CompletedTask;
    }

    public byte[] Serialize(T[] data, SerializationContext context)
        => Serialize(data);

    public byte[] Serialize(string data, SerializationContext context)
        => _keyMessage;

    public static byte[] Serialize(T[] data)
        => MemoryMarshal.AsBytes(data).ToArray();
}
