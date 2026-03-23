using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microservices.Gateway.Infrastructure.JsonConverters.Bybit;
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
    private static readonly byte[] Subscribe = 
"""
{"req_id":"1","op":"subscribe","args":[
"orderbook.50.BTCUSDT",
"publicTrade.BTCUSDT",
"orderbook.50.ETHUSDT",
"publicTrade.ETHUSDT"
]}
"""u8.ToArray(); // NO "kline.1.***USDT"
    
    private readonly ILogger _logger = Log.ForContext<BybitListener>();
    private readonly AppConfig _config;
    private readonly IAsyncPublisher<TradeInfo[]> _pubTrades;
    private readonly IAsyncPublisher<KlineInfo[]> _pubKlines;
    private readonly IAsyncPublisher<Levels10Info> _pubLevels;

    private readonly TradeInfoConverter _cnvTrades = new();
    private readonly LevelsInfoConverter _cnvLevels = new();
    private readonly KlinesGenerator _genKlines;

    public BybitListener(IOptions<AppConfig> options,
        IAsyncPublisher<TradeInfo[]> pubTrades,
        IAsyncPublisher<KlineInfo[]> pubKlines,
        IAsyncPublisher<Levels10Info> pubLevels)
    {
        _config = options.Value;
        _pubTrades = pubTrades;
        _pubLevels = pubLevels;
        _pubKlines = pubKlines;
        _genKlines = new((KlinePeriod)_config.KlinePeriod);
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
            await retryPolicy.ExecuteAsync(async t => await ReadingLoop(t), ct);
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
        await ws.SendAsync(new ArraySegment<byte>(Subscribe), WebSocketMessageType.Text, true, ct);

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
            var json = buf.AsSpan(0, res.Count);
            Debug.Assert(json[0] == (byte)'{'); // really JSON
            
            var reader = new Utf8JsonReader(json, isFinalBlock: true, default);
            if (json.IndexOf("\"publicTrade."u8) > 0) // trade
            {
                var trades = _cnvTrades.Read(ref reader, null, default);
                
                // pub trades
                if (trades.Count == 0)
                    continue;
                await _pubTrades.PublishAsync(trades.Array![0..trades.Count], ct);
                
                // pub candles
                var bars = _genKlines.Update(trades);
                if (bars.Count > 0)
                    await _pubKlines.PublishAsync(bars.Array![0..trades.Count], ct);
            }
            else if (json.IndexOf("\"orderbook.50."u8) > 0)
            {
                var lvl10 = _cnvLevels.Read(ref reader, null, default);
                await _pubLevels.PublishAsync(lvl10, ct);
            }
            else
                _logger.Debug("Unhandled WS-message: '{msg}'", Encoding.UTF8.GetString(json));
        }
    }
}
