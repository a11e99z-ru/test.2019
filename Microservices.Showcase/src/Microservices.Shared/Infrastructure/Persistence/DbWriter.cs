using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Microservices.Shared.Infrastructure.Persistence;

public sealed class DbWriter<T, P>
    : IHostedService
    , IAsyncPublisher<T[]>
    where T : unmanaged
    where P : class, ICopyable<T>, new()
{
    internal const string TradeUpdatesChannelName = "trades_updates"; 
    private static readonly string TypeName = typeof(T).Name;
    private static readonly BulkCopyOptions BulkOptions
        = new() { BulkCopyType = BulkCopyType.ProviderSpecific };
    
    private readonly  ILogger _logger = Log.ForContext<DbWriter<T, P>>();
    private readonly AppConfig.DbConfig _config;
    private volatile ITable<P> _table;
    
    public DbWriter(IOptions<AppConfig.DbConfig> options)
    {
        _config = options.Value;
    }
    
    public async ValueTask PublishAsync(T[] data, CancellationToken ct)
    {
        var table = _table;
        if (table is null)
        {
            _logger.Warning("DbWrite<{type}> is not ready yet. Ignoring items.", TypeName);
            return;
        }
            
        try
        {
            var items = data.Select(x => 
                {
                    var p = new P(); //OPTZ from pool
                    p.CopyFrom(in x); 
                    return p;
                });
            
            //OPT when u sure that no duplicate PK
            // in other case method is stuck
            await table.DataContext.BulkCopyAsync(BulkOptions, items, ct);
            
            /*batch merge:
                await (_table)
                   .Merge()
                   .Using(items)
                   .OnTargetKey()
                   .InsertWhenNotMatched()
                   //.DeleteWhenMatchedAnd((_, s) => s.StatusIsFinished)
                   .UpdateWhenMatched()
                   .MergeAsync(ct);
            */
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DbWriter<{type}>.PublishAsync", TypeName);
            throw;
        }
    }
    
    public async Task StartAsync(CancellationToken ct)
    {
        //TODO create DB if its not 'postgres'
        try
        {
            var db = CreateConnection(_config.ConnectionString, ct);
#if DEBUG
            //NB sometimes L2DB caches old scheme
            //MappingSchema.ClearCache(); 
            var dbflags = TableOptions.CheckExistence; // drop & create
#else
            var dbflags = TableOptions.CreateIfNotExists;
#endif
            var table = await CreateTrades(db, _config.TableName, dbflags, ct);
            Interlocked.Exchange(ref _table, table);
            _logger.Warning("DbWriter<{type}> is started.", TypeName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in DbWriter<{type}>", TypeName);
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var table = Interlocked.Exchange(ref _table, null);
        await table.DataContext.DisposeAsync();
        _logger.Warning("DbWriter<{type}> is stopped.", TypeName);
    }
    
    internal static DataConnection CreateConnection(string connString, CancellationToken ct)
    {
        //TODO version. I used locally 18-alpine
        var provider = PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v18);

        var res = new DataConnection(new DataOptions().UseConnectionString(provider, connString));
#if DEBUG5 // to see SQL-commands
        res.TraceSwitchConnection.Level = TraceLevel.Verbose;
        res.OnTraceConnection = it => Print(it.CommandText, it.SqlText, it.TraceLevel);
#endif
        return res;
    }

    private static async Task<ITable<P>> CreateTrades(IDataContext db, string tableName, TableOptions flags, CancellationToken ct)
    {
        var table = await db.CreateTableAsync<P>(tableName, tableOptions: flags, token: ct);
        
        // create indices w/o Fluent API
        foreach (var cmd in new[] { 
                     $"CREATE INDEX IF NOT EXISTS ix_{tableName}_timestamp ON {tableName}(timestamp DESC)",
                     $"CREATE INDEX IF NOT EXISTS ix_{tableName}_symbol ON {tableName} USING HASH(symbol)"
                 })
        {
            await db.ExecuteAsync(cmd, cancellationToken: ct);
        }

        // create trigger & notification
        var cmd2 = $@"""CREATE OR REPLACE FUNCTION notify_order_event()
        RETURNS trigger AS $$
        BEGIN
        PERFORM pg_notify('{TradeUpdatesChannelName}', NEW.rowid::text);
        RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;

        CREATE TRIGGER IF NOT EXISTS tx_trades_after_insert
            AFTER INSERT ON trades
        FOR EACH ROW EXECUTE FUNCTION notify_order_event();""";
        await table.DataContext.ExecuteAsync(cmd2, ct);
        
        return table;
    }
}
