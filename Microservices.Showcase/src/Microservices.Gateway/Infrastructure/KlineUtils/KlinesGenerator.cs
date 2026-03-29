using System.Diagnostics;
using System.Runtime.InteropServices;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Gateway.Infrastructure.KlineUtils;

// generates Klines based on Trades
// own generator of klines cuz Bybit supports minimum 1min
// and the one can generate for any Period
public sealed class KlinesGenerator
{
    private readonly KlinePeriod Period;
    private readonly long PeriodMcs;
        
    private readonly Dictionary<Str16, KlineInfo> _symKlines = new();
    private KlineInfo[] _klines = new KlineInfo[12];

    public KlinesGenerator(KlinePeriod period)
    {
        Period = period;
        PeriodMcs = period.ToSeconds() * 1_000_000L;
    }

    public ArraySegment<KlineInfo> Update(ArraySegment<TradeInfo> trades)
    {
        var pos = 0;
        foreach (ref var it in (Span<TradeInfo>)trades)
        {
            ref var kline = ref CollectionsMarshal.GetValueRefOrAddDefault(_symKlines, it.Info.Symbol, out var was);
            if (!was)
                (kline.Info.Symbol, kline.Period) = (it.Info.Symbol, Period);
            
            var nbar = Update(ref kline, it);
            if (nbar is { Kline.Volume: > 0 } bar)
            {
                if (pos == _klines.Length)
                    Array.Resize(ref _klines, _klines.Length * 2);
                _klines[pos++] = bar;
            }
        }

        return new(_klines, 0, pos);
    }

    private KlineInfo? Update(ref KlineInfo bar, in TradeInfo trade)
    {
        Debug.Assert(bar.Info.Symbol == trade.Info.Symbol);
        
        var (ts, price, vol) = (trade.Info.Timestamp, trade.Trade.Price, trade.Trade.Volume);
        ref var kline = ref bar.Kline;
        var oldBar = bar;

        // if diff period then kline is ready
        var klineStartTime = ts - ts % PeriodMcs;
        var createdNew = bar.Info.Timestamp != klineStartTime;
        //TODO missed candles (OHLC=.Price etc)
        if (createdNew)
        {
            kline = default;
            bar.Info.Timestamp = klineStartTime;
            kline.Open = price;
        }

        kline.High = Math.Max(kline.High, price);
        if (kline.Low == 0 || kline.Low > price)
            kline.Low = price;
        kline.Close = price;
        
        kline.Volume += vol;
        kline.Value += price * vol;

        return createdNew ? oldBar : null;
    }
}
