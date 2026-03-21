using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LinqToDB;
using LinqToDB.Data;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microservices.Shared.Domain.MarketData;
using Microservices.Shared.Domain.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
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
    private readonly IAsyncPublisher<T[]> _publisher;
    private readonly Channel<long> _inserted;
    
    public DbReader(IOptions<AppConfig.DbConfig> options,
        IAsyncPublisher<T[]> publisher)
    {
        _config = options.Value;
        _publisher = publisher;
        _inserted = Channel.CreateBounded<long>(new BoundedChannelOptions(_config.QueueSize)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // we can read Db since .Where(.numid > lastRowId) each 1sec
        // but we will use DB.notifications. why not?
        
        _logger.Information("DbReader<{type}> is starting...", TypeName);
        var cts2 = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ct = cts2.Token;
        try
        {
            await using var db = DbWriter<T, P>.CreateConnection(_config.ConnectionString, ct);
            var trades = db.CreateTable<TradeEntity>(_config.TableName, tableOptions: TableOptions.None);

            Task.Run(async () => await ListenNotifications(new DataConnection(db.Options), ct));

            var items = new List<TradeInfo>(10);
            var lastRowId = -1L;
            while (!ct.IsCancellationRequested)
            {
                var id = await _inserted.Reader.ReadAsync(ct);
                var ids = ReadAllInChannel().Append(id).Append(lastRowId);
                var (minid, maxid) = (ids.Min(), ids.Max());
                if (maxid <= lastRowId)
                    continue;
#if DEBUG
                _logger.Debug("DbReader<{type}> read (min:{min}, max:{max}) items",
                    TypeName, minid, maxid);
#endif
                var rows = trades.Where(x => x.RowId > maxid);
                if (!rows.Any())
                    continue;
                lastRowId = rows.Max(x => x.RowId);

                items.Clear();
                foreach (var p in rows)
                {
                    var t = new TradeInfo();
                    p.CopyTo(ref t);
                    items.Add(t);
                }

                var tt = Unsafe.As<T[]>(items.ToArray());
                await _publisher.PublishAsync(tt, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DbReader<{type}>", TypeName);
            await cts2.CancelAsync();
        }
        _logger.Warning("DbReader<{type}> is starting...", TypeName);
        
        IEnumerable<long> ReadAllInChannel()
        {
            while (_inserted.Reader.TryRead(out var item))
                yield return item;
        }
    }

    private async ValueTask ListenNotifications(DataConnection db, CancellationToken ct)
    {
        await using var npgsqlConn = (NpgsqlConnection)await db.OpenDbConnectionAsync(ct);

        npgsqlConn.Notification += async (o, e) =>
        {
#if DEBUG
            _logger.Debug("DbReader<{type}> was notified with '{str}'",
                TypeName, e.Payload);
#endif
            if (long.TryParse(e.Payload, out var rowid))
                await _inserted.Writer.WriteAsync(rowid, ct);
        };
        
        var chanName = DbWriter<T, P>.TradeUpdatesChannelName;
        await db.ExecuteAsync($"LISTEN {chanName}");
        _logger.Information("DbReader<{type}> is listening '{chan}'", TypeName, chanName);

        try
        {
            while (!ct.IsCancellationRequested)
                await npgsqlConn.WaitAsync(ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DbReader<{type}>.", TypeName);
        }
    }
}
