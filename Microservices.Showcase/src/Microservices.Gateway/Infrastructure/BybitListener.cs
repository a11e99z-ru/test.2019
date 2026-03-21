using System.Diagnostics;
using System.Net.WebSockets;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.Configs;
using Microservices.Shared.Domain.MarketData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Serilog;

namespace Microservices.Gateway.Infrastructure;

public sealed class BybitListener
    : BackgroundService
{
    private readonly ILogger _logger = Log.ForContext<BybitListener>();
    private readonly AppConfig _config;
    private readonly IAsyncPublisher<TradeInfo[]> _pubTrades;
    private readonly IAsyncPublisher<Levels10Info[]> _pubLevels;
    private readonly IAsyncPublisher<KlineInfo[]> _pubKlines;

    public BybitListener(IOptions<AppConfig> options,
        IAsyncPublisher<TradeInfo[]> pubTrades,
        IAsyncPublisher<Levels10Info[]> pubLevels,
        IAsyncPublisher<KlineInfo[]> pubKlines)
    {
        _config = options.Value;
        _pubTrades = pubTrades;
        _pubLevels = pubLevels;
        _pubKlines = pubKlines;
    }
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.Information("BybitListener is starting...");
        
        var retryPolicy = Policy
            .Handle<WebSocketException>().Or<IOException>()
            .WaitAndRetryAsync(7, 
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)),
                (ex, time, context) => {
                    _logger.Error(ex, "Conn failed. Retrying in {secs}s...", time.TotalSeconds);
                });

        try
        {
            await retryPolicy.ExecuteAsync(async (ct) => await ReadingLoop(ct), ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in BybitListener.");
        }
        _logger.Warning("BybitListener is stopped.");
    }

    private async Task ReadingLoop(CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new(_config.MarketDataUrl), ct);

        var buf = new byte[20 * 1024];
        while (!ct.IsCancellationRequested)
        {
            var res = await ws.ReceiveAsync(new(buf), ct);
            if (res.MessageType == WebSocketMessageType.Close)
            {
                _logger.Warning("WS closed cuz {stat}: '{msg}'", 
                    res.CloseStatus, res.CloseStatusDescription);
                return;
            }
            Debug.Assert(res is { EndOfMessage: true, MessageType: WebSocketMessageType.Text });

            var span = buf.AsSpan(0, res.Count);
        }
    }
}
