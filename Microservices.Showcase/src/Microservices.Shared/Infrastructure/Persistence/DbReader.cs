using System.Threading.Channels;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Microservices.Shared.Infrastructure.Persistence;

public sealed class DbReader<T, P>
    : BackgroundService
    where T : unmanaged
    where P : class, ICopyable<T>, new()
{
    private static readonly string TypeName = typeof(T).Name;
    private readonly  ILogger _logger = Log.ForContext<DbReader<T, P>>();
    private readonly AppConfig.DbConfig _config;
    private readonly Channel<string> _inserted;
    private readonly IAsyncPublisher<T> _publisher;
    
    public DbReader(IOptions<AppConfig.DbConfig> options,
        IAsyncPublisher<T> publisher)
    {
        _config = options.Value;
        _publisher = publisher;
        _inserted = Channel.CreateBounded<string>(new BoundedChannelOptions(_config.QueueSize)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.Information("DbReader<{type}> is starting...", TypeName);
        try
        {
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DbReader<{type}>", TypeName);
            throw;
        }
        _logger.Warning("DbReader<{type}> is starting...", TypeName);
    }
}
