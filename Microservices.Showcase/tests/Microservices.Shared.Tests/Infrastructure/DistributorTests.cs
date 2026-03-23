using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microservices.Shared.Infrastructure;
using Microsoft.Extensions.Options;
using Serilog;
using Xunit;

namespace Microservices.Shared.Tests.Infrastructure;

public class DistributorTests
{
    private static readonly IOptions<AppConfig> _options 
        = Options.Create<AppConfig>(new AppConfig() { QueueSize = 10 });
    
    [Fact]
    public async Task Publish_ShouldDistributeToPublisher()
    {
        // Arrange
        var logger = Log.Logger; // Use silent or default logger
        var pub = new TestPublisher<int>();
        var distributor = new Distributor<int>(logger, _options, pub);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await distributor.StartAsync(cts.Token);
        await distributor.PublishAsync(123, cts.Token);

        // Assert
        var result = await pub.WaitAsync(1, cts.Token);
        Assert.Single(result);
        Assert.Equal(123, result[0]);

        await distributor.StopAsync(cts.Token);
    }

    [Fact]
    public async Task Publish_ShouldBatchItems()
    {
        // Arrange
        var logger = Log.Logger;
        var pub = new TestPublisher<int>();
        var distributor = new Distributor<int>(logger, _options, pub);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await distributor.StartAsync(cts.Token);
        // Publish multiple items quickly
        await distributor.PublishAsync(10, cts.Token);
        await distributor.PublishAsync(20, cts.Token);
        await distributor.PublishAsync(30, cts.Token);

        // Assert
        var result = await pub.WaitAsync(3, cts.Token);
        // It should at least have the first item, and possibly others if they were batched
        Assert.Equal(3, result.Length);
        Assert.Equal(10, result[0]);
        Assert.Equal(20, result[1]);
        Assert.Equal(30, result[2]);
        
        await distributor.StopAsync(cts.Token);
    }
    
    [Fact]
    public async Task Publish_ShouldReturnManyTimes()
    {
        // Arrange
        var logger = Log.Logger;
        var pub = new TestPublisher<int>();
        var distributor = new Distributor<int>(logger, _options, pub);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await distributor.StartAsync(cts.Token);
        // Publish multiple items quickly
        await distributor.PublishAsync(10, cts.Token);
        await distributor.PublishAsync(20, cts.Token);
        await distributor.PublishAsync(30, cts.Token);

        // Assert-1
        var result = await pub.WaitAsync(3, cts.Token);
        // It should at least have the first item, and possibly others if they were batched
        Assert.Equal(3, result.Length);
        Assert.Equal(10, result[0]);
        Assert.Equal(20, result[1]);
        Assert.Equal(30, result[2]);
        
        // Act-2
        await distributor.PublishAsync(100, cts.Token);
        await distributor.PublishAsync(200, cts.Token);
        
        // Assert-2
        result = await pub.WaitAsync(2, cts.Token);
        // It should at least have the first item, and possibly others if they were batched
        Assert.Equal(2, result.Length);
        Assert.Equal(100, result[0]);
        Assert.Equal(200, result[1]);
        
        await distributor.StopAsync(cts.Token);
    }
    
    private class TestPublisher<T> 
        : IAsyncPublisher<T[]>
    {
        private readonly List<T[]> _items = new(10);

        public ValueTask PublishAsync(T[] value, CancellationToken ct)
        {
            lock (_items)
                _items.Add(value);
            return ValueTask.CompletedTask;
        }

        public async Task<T[]> WaitAsync(int count, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                lock (_items)
                {
                    var items = _items.SelectMany(x => x);
                    if (items.Count() >= count)
                    {
                        var res = items.Take(count).ToArray();
                        items = items.Skip(count);
                        _items.Clear();
                        if (items.Any())
                            _items.Add(items.ToArray());

                        return res;
                    }
                }

                await Task.Delay(50, ct);
            }

            throw new OperationCanceledException();
        }
    }
}
