using System.Text.Json;
using Microservices.Shared.Domain.MarketData;
using Microservices.Gateway.Infrastructure.JsonConverters.Bybit;
using Xunit;

namespace Microservices.Gateway.Tests.Infrastructure.JsonConverters;

public class BybitConvertersTests
{
    [Fact]
    public void Json_ToTradeInfo()
    {
        var json = 
"""
{
    "topic": "publicTrade.BTCUSDT",
    "ts": 1774023924134,
    "type": "snapshot",
    "data": [
    {
        "i": "1311768467463790320",
        "T": 1774023924133,
        "p": "123456.78",
        "v": "1.2345",
        "S": "Sell",
        "seq": 102815014895,
        "s": "BTCUSDT",
        "BT": false,
        "RPI": false
   },
   {
        "i": "1311768467463790321",
        "T": 1774023924134,
        "p": "1234.56",
        "v": "1.23",
        "S": "Buy",
        "seq": 102815014896,
        "s": "ETHUSDT",
        "BT": false,
        "RPI": false
   }]
}
"""u8; // "i":0x123456789abcdef0
        var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
        var conv = new TradeInfoConverter();
        var trades = conv.Read(ref reader, typeof(TradeInfo), default);

        Assert.Equal("BTCUSDT", trades[0].Info.Symbol.ToString());
        Assert.Equal(1_774_023_924_133_000, trades[0].Info.Timestamp);
        Assert.Equal(123456.78, trades[0].Trade.Price);
        Assert.Equal(1.2345, trades[0].Trade.Volume);
        Assert.False(trades[0].Trade.TakerIsBuyer);
        Assert.Equal(Guid.Parse("{9abcdef0-5678-1234-0000-000000000000}"), trades[0].Trade.Id);

        Assert.Equal("ETHUSDT", trades[1].Info.Symbol.ToString());
        Assert.Equal(1_774_023_924_134_000, trades[1].Info.Timestamp);
        Assert.Equal(1234.56, trades[1].Trade.Price);
        Assert.Equal(1.23, trades[1].Trade.Volume);
        Assert.True(trades[1].Trade.TakerIsBuyer);
        Assert.Equal(Guid.Parse("{9abcdef1-5678-1234-0000-000000000000}"), trades[1].Trade.Id);

        Assert.True(trades.Count == 2);
    }

    [Fact]
    public void Json_ToKlineInfo()
    {
        var json = 
"""
{
    "type": "snapshot",
    "topic": "kline.1.BTCUSDT",
    "data": [
    {
        "start": 1774022040000,
        "end": 1774022099999,
        "interval": "1",
        "open": "69933.8",
        "close": "69890.3",
        "high": "69938.9",
        "low": "69856.6",
        "volume": "14.020913",
        "turnover": "979906.164487",
        "confirm": false,
        "timestamp": 1774022100157
    },
    {
        "start": 1774022100000,
        "end": 1774022159999,
        "interval": "1",
        "open": "69890.3",
        "close": "69900.0",
        "high": "69910.0",
        "low": "69880.0",
        "volume": "10.5",
        "turnover": "733950.0",
        "confirm": true,
        "timestamp": 1774022160000
    }],
    "ts": 1774022160000
}
"""u8;
        var reader = new Utf8JsonReader(json, isFinalBlock: true, state: default);
        var conv = new KlineInfoConverter();
        var klines = conv.Read(ref reader, typeof(ArraySegment<KlineInfo>), default);

        // SortKlines should move confirm:true to the front
        Assert.Single(klines);
        ReadOnlySpan<KlineInfo> spk = klines;
        ref readonly var kl0 = ref spk[0];
        Assert.Equal(1_774_022_100_000_000, kl0.Info.Timestamp);
        Assert.Equal("BTCUSDT", kl0.Info.Symbol.ToString());
        Assert.Equal(KlinePeriod.Min1, kl0.Period);
        Assert.Equal(69890.3, kl0.Kline.Open);
        Assert.Equal(69910.0, kl0.Kline.High);
        Assert.Equal(69880.0, kl0.Kline.Low);
        Assert.Equal(69900.0, kl0.Kline.Close);
        Assert.Equal(10.5, kl0.Kline.Volume);
        Assert.Equal(733950.0, klines[0].Kline.Value);
    }

    [Fact]
    public void Json_ToLevelsInfo()
    {
        var jsonSnapshot =
"""
{
  "topic": "orderbook.50.BTCUSDT",
  "ts": 1774088848028,
  "type": "snapshot",
  "data": {
    "s": "BTCUSDT",
    "b": [
      ["70579.9", "1.085942"],
      ["70578.8", "0.607274"],
      ["70578.7", "0.186984"],
      ["70577.5", "0.005"],
      ["70577.3", "0.133423"],
      ["70577", "0.007084"],
      ["70576.9", "0.001"],
      ["70576.8", "0.002"],
      ["70576.2", "0.001"],
      ["70576.1", "0.03"],
      ["70575.7", "0.003"],
      ["70575.3", "0.032227"]
    ],
    "a": [
      ["70580", "0.01837"],
      ["70580.4", "0.002"],
      ["70581.4", "0.000333"],
      ["70583.1", "0.100107"],
      ["70584", "0.001"],
      ["70584.3", "0.099134"],
      ["70584.8", "0.000333"],
      ["70585", "0.02"],
      ["70585.3", "0.140743"],
      ["70585.8", "0.008515"],
      ["70585.9", "0.0002"],
      ["70586", "0.013515"]
    ],
    "u": 7528002,
    "seq": 102879627761
  },
  "cts": 1774088848027
}
"""u8; 
        
        var jsonDelta = // Ask:del+upd. Bids:del+ins
"""
{
    "topic": "orderbook.50.BTCUSDT",
    "ts": 1774088848049,
    "type": "delta",
    "data": 
    {
        "s": "BTCUSDT",
        "b": [
            ["70577", "0"],
            ["70576.1", "0.12"]
        ],
        "a": [
            ["70584", "0"],
            ["70584.5", "0.23"]
        ],
        "u": 7528003,
        "seq": 102879627831
    },
    "cts": 1774088848047
}
"""u8;

        var conv = new LevelsInfoConverter();
        var lvl10 = default(Levels10Info);
        {
            var reader = new Utf8JsonReader(jsonSnapshot, isFinalBlock: true, state: default);
            lvl10 = conv.Read(ref reader, typeof(ArraySegment<Levels10Info>), default);
        }
        Assert.Equal("BTCUSDT", lvl10.Info.Symbol.ToString());
        Assert.Equal(1774088848028_000, lvl10.Info.Timestamp);
        Assert.Equal(new PriceLevel(70577, 0.007084), lvl10.Levels.Bids[5]);
        Assert.Equal(new PriceLevel(70584, 0.001000), lvl10.Levels.Asks[4]);
        
        {
            var reader = new Utf8JsonReader(jsonDelta, isFinalBlock: true, state: default);
            lvl10 = conv.Read(ref reader, typeof(ArraySegment<Levels10Info>), default);
        }
        Assert.Equal(1774088848049_000, lvl10.Info.Timestamp);
        Assert.Equal(new PriceLevel(70576.9, 0.001), lvl10.Levels.Bids[5]);
        Assert.Equal(new PriceLevel(70576.1, 0.120), lvl10.Levels.Bids[8]);
        Assert.Equal(new PriceLevel(70584.3, 0.099134), lvl10.Levels.Asks[4]);
        Assert.Equal(new PriceLevel(70584.5, 0.230), lvl10.Levels.Asks[5]);
    }
}
