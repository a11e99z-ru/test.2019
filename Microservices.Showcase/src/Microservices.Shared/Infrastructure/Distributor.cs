using System.Threading.Channels;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Microservices.Shared.Infrastructure;

public sealed class Distributor<T>
    : BackgroundService
    , IAsyncDistributor<T[]>
    where T : unmanaged
{
    private static readonly string TypeName = typeof(T).Name;
    private readonly ILogger _logger = Log.ForContext<Distributor<T>>();
    private readonly Channel<T[]> _channel;
    private readonly List<IAsyncPublisher<T[]>> _pubs;
    
    public Distributor(ILogger logger,
        IOptions<AppConfig> options,
        params IEnumerable<IAsyncPublisher<T[]>> publishers)
    {
        _pubs = publishers.ToList();
        _channel = Channel.CreateBounded<T[]>(new BoundedChannelOptions(options.Value.QueueSize)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
    }
    
    public async ValueTask PublishAsync(T[] value, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(value, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.Information("Distributor<{type}> is starting...", TypeName);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var arr = await _channel.Reader.ReadAsync(ct);
                
                var ts = _pubs.Select(x => x.PublishAsync(arr, ct).AsTask());
                Task.WaitAll(ts, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in Distributor<{type}>", TypeName);
        }
        _logger.Warning("Distributor<{type}> is stopped.", TypeName);
    }
}
