using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Gateway.Infrastructure.JsonConverters.Bybit;

public sealed class KlineInfoConverter
    : JsonConverter<ArraySegment<KlineInfo>>
{
    private const int DefLength = 12;
    private KlineInfo[] _klines = new KlineInfo[DefLength];
    private bool[] _fins = new bool[DefLength];
    private int _count = 0;
    
    public override ArraySegment<KlineInfo> Read(ref Utf8JsonReader json, Type typeToConvert, JsonSerializerOptions options)
    {
        var ok = json.Read();
        Debug.Assert(ok);
        if (json.TokenType != JsonTokenType.StartObject)
            throw new JsonException("StartObject '{' expected");

        /*{
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
                 "confirm": true,
                 "timestamp": 1774022100157
               }
             ],
             "ts": 1774022100157
           }*/
        
        // skip till 'data':[]
        Str16 symbol = Str16.Empty; 
        while (json.TokenType != JsonTokenType.StartArray)
        {
            if (json.TokenType == JsonTokenType.PropertyName
                && json.ValueSpan.SequenceEqual("topic"u8))
            {
                Debug.Assert(json.Read() && json.ValueSpan.StartsWith("kline."u8));
                var dpos = json.ValueSpan.LastIndexOf((byte)'.');
                symbol = Str16.FromJsonUnsafe(json.ValueSpan[++dpos..], out var _);
            }
            ok = json.Read();
            Debug.Assert(ok);
        }
        Debug.Assert(symbol != Str16.Empty);

        _count = 0;
        ok = json.Read();
        Debug.Assert(ok && json.TokenType == JsonTokenType.StartObject);
        while (json.TokenType != JsonTokenType.EndArray)
        {
            if (_count == _klines.Length)
            {
                Array.Resize(ref _klines, _count * 2);
                Array.Resize(ref _fins, _klines.Length);
            }
            
            ref var res = ref _klines[_count];
            ref var info = ref res.Info;
            ref var kline = ref res.Kline;
            ref var isFinished = ref _fins[_count++];

            ok = json.Read();
            while (json.TokenType != JsonTokenType.EndObject)
            {
                Debug.Assert(ok && json.TokenType == JsonTokenType.PropertyName);
                var span = json.ValueSpan;
                var (ch0, plen) = ((char)span[0], span.Length);
                ok = json.Read();
                Debug.Assert(ok);
                switch ((p0: ch0, plen))
                {
                    case ('s', _): // start
                        Debug.Assert(json.TokenType == JsonTokenType.Number);
                        info.Timestamp = json.GetInt64() * 1_000;
                        break;
                    case ('i', _): // interval
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = int.TryParse(json.ValueSpan, out var min);
                        Debug.Assert(ok);
                        res.Period = (KlinePeriod)(min * 60);
                        break;
                    case ('o', _): // open
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.Open);
                        Debug.Assert(ok);
                        break;
                    case ('c', 5): // close
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.Close);
                        Debug.Assert(ok);
                        break;
                    case ('h', _): // high
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.High);
                        Debug.Assert(ok);
                        break;
                    case ('l', _): // low
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.Low);
                        Debug.Assert(ok);
                        break;
                    case ('v', _): // volume
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.Volume);
                        Debug.Assert(ok);
                        break;
                    case ('t', 8): // turnover
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out kline.Value);
                        Debug.Assert(ok);
                        break;
                    case ('c', 7): // confirm
                        Debug.Assert(json.TokenType is JsonTokenType.True or JsonTokenType.False);
                        isFinished = json.GetBoolean();
                        break;
                }

                ok = json.Read();
                Debug.Assert(ok);
            }
            info.Symbol = symbol;
            ok = json.Read();
            Debug.Assert(ok);
        }

        _count = SortKlines();
        return new(_klines, 0, _count);
    }

    public override void Write(Utf8JsonWriter writer, ArraySegment<KlineInfo> value, JsonSerializerOptions options)
        => throw new NotImplementedException();

    private int SortKlines() // fin==true in front
    {
        var spk = _klines.AsSpan()[.._count];
        var spf = _fins.AsSpan()[.._count];
        
        var finCount = 0;
        for (var i = 0; i < _count; i++)
        {
            if (!spf[i]) continue;
            if (i != finCount)
            {
                (spk[finCount], spk[i]) = (spk[i], spk[finCount]);
                (spf[finCount], spf[i]) = (spf[i], spf[finCount]);
            }
            finCount++;
        }
        return finCount;
    }
}
