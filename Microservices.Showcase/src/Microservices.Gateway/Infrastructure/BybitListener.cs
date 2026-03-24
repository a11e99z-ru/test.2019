using System.Collections.Frozen;
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
using Prometheus;
using Prometheus.DotNetRuntime;
using Prometheus.HttpClientMetrics;

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
"""u8.ToArray(); // NO for klines, I'll do it myself
    
#region Prometheus    
    private static readonly FrozenDictionary<Str16, string> _labels = new Dictionary<Str16, string>() 
    { 
        [Str16.FromAscii("BTCUSDT")] = "BTC", 
        [Str16.FromAscii("ETHUSDT")] = "ETH",
        [Str16.FromAscii("btcusdt")] = "BTC", 
        [Str16.FromAscii("ethusdt")] = "ETH" 
    }.ToFrozenDictionary();
    
    private readonly Histogram _tradeVolumeHistogram = Metrics.CreateHistogram(
        "trade_volume_usd", "Trade volume in USD", 
        new HistogramConfiguration()
        {
            Buckets = Histogram.ExponentialBuckets(start: 0.01, factor: 10, count: 9),
            LabelNames = ["asset"]
        });
    /*scrape_configs:
       - job_name: 'trading-bot'
         scrape_interval: 5s
         # Важно для Native Histograms
         fallback_scrape_protocol: PrometheusProto
         static_configs:
           - targets: ['localhost:5000']*/

    private readonly Counter _tradeCounter = Metrics.CreateCounter(
        "trade_count", "Trade counter",
        new CounterConfiguration() { LabelNames = ["asset"] });
    private readonly Gauge _lastTradePrice = Metrics.CreateGauge(
        "last_trade_price", "Last trade price",
        new GaugeConfiguration() { LabelNames = ["asset"] });
    private readonly Gauge _deltaVolumes = Metrics.CreateGauge(
        "delta_10_volumes", "Sum(Ask[].Volume - Bid[].Volume)",
        new GaugeConfiguration() { LabelNames = ["asset"] });
#endregion
    
    private readonly ILogger _logger = Log.ForContext<BybitListener>();
    private readonly AppConfig _config;
    private readonly IAsyncPublisher<TradeInfo> _pubTrades;
    private readonly IAsyncPublisher<KlineInfo> _pubKlines;
    private readonly IAsyncPublisher<Levels10Info> _pubLevels;

    private readonly TradeInfoConverter _cnvTrades = new();
    private readonly LevelsInfoConverter _cnvLevels = new();
    private readonly KlinesGenerator _genKlines;

    public BybitListener(IOptions<AppConfig> options,
        IAsyncDistributor<TradeInfo> pubTrades, 
        IAsyncDistributor<KlineInfo> pubKlines,
        IAsyncDistributor<Levels10Info> pubLevels)
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
            .WaitAndRetryAsync(int.MaxValue, 
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time, context) => {
                    _logger.Error(ex, "WS-conn failed. Retrying in {secs}s...", time.TotalSeconds);
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
                foreach (var it in trades)
                {
                    await _pubTrades.PublishAsync(it, ct);
                    // metrics
                    var label = _labels[it.Info.Symbol];
                    _tradeCounter.WithLabels(label).Inc();
                    _lastTradePrice.WithLabels(label).Set(it.Trade.Price);
                    _tradeVolumeHistogram.WithLabels(label).Observe(it.Trade.Price * it.Trade.Volume);
                }

                // pub candles
                var bars = _genKlines.Update(trades);
                foreach (var it in bars)
                    await _pubKlines.PublishAsync(it, ct);
            }
            else if (json.IndexOf("\"orderbook.50."u8) > 0)
            {
                var lvl10 = _cnvLevels.Read(ref reader, null, default);
                await _pubLevels.PublishAsync(lvl10, ct);
                // metrics
                var label = _labels[lvl10.Info.Symbol];
                _deltaVolumes.WithLabels(label).Set(lvl10.Levels.AsksVolume - lvl10.Levels.BidsVolume);
            }
            else
                _logger.Debug("Unhandled WS-message: '{msg}'", Encoding.UTF8.GetString(json));
        }
    }
}
