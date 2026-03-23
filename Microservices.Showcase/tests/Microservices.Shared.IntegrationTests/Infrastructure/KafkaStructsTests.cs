using Testcontainers.Kafka;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microservices.Shared.Infrastructure.Kafka;
using Microsoft.Extensions.Options;
using Xunit;
using Serilog;
using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Microservices.Shared.IntegrationTests.Infrastructure;

public record struct TestStruct(long Id, double Value);

public class KafkaStructsTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.6.1")
        .Build();

    public async Task InitializeAsync()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        await _kafkaContainer.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _kafkaContainer.DisposeAsync();
        await Log.CloseAndFlushAsync();
    }

    private class TestPublisher<T> : IAsyncPublisher<T[]>
    {
        public readonly TaskCompletionSource<T[]> Tcs = new();

        public ValueTask PublishAsync(T[] value, CancellationToken ct)
        {
            Tcs.TrySetResult(value);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Structs_RoundTrip_ThroughKafka_ShouldSucceed()
    {
        // Arrange
        var topic = "test-topic-" + Guid.NewGuid();
        var groupKey = "test-group-" + Guid.NewGuid();
        var bootstrapServers = _kafkaContainer.GetBootstrapAddress();
        
        var kafkaConfig = new AppConfig.KafkaConfig
        {
            Servers = bootstrapServers,
            Topic = topic,
            GroupKey = groupKey
        };
        var options = Options.Create(kafkaConfig);

        // CREATE TOPIC EXPLICITLY
        using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build())
        {
            try {
                await adminClient.CreateTopicsAsync(new TopicSpecification[] { 
                    new TopicSpecification { Name = topic, ReplicationFactor = 1, NumPartitions = 1 } 
                });
            } catch (CreateTopicsException e) {
                Log.Warning("Topic create failed: {reason}", e.Results[0].Error.Reason);
            }
        }

        var writer = new StructsWriter<TestStruct>(options);
        var pub = new TestPublisher<TestStruct>();
        var reader = new StructsReader<TestStruct>(options, pub);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        await writer.StartAsync(cts.Token);
        await reader.StartAsync(cts.Token);

        // Wait for reader to be assigned to partitions
        await Task.Delay(10000, cts.Token);

        var originalData = new[]
        {
            new TestStruct(1, 1.1),
            new TestStruct(2, 2.2),
            new TestStruct(3, 3.3)
        };

        // Act
        // Repeatedly publish every few seconds until received or timeout
        var sw = Stopwatch.StartNew();
        while (!pub.Tcs.Task.IsCompleted && sw.Elapsed < TimeSpan.FromSeconds(40))
        {
            try {
                await writer.PublishAsync(originalData, cts.Token);
                Log.Information("Published data attempt...");
            } catch (Exception ex) {
                Log.Warning(ex, "Failed to publish attempt");
            }
            await Task.Delay(2000, cts.Token);
        }

        // Assert
        var receivedData = await pub.Tcs.Task.WaitAsync(cts.Token);
        Assert.Equal(originalData.Length, receivedData.Length);
        Assert.Equal(originalData, receivedData);

        await reader.StopAsync(cts.Token);
        await writer.StopAsync(cts.Token);
    }
}
