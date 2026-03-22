using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Gateway.Infrastructure.JsonConverters.Bybit;

public sealed class TradeInfoConverter 
    : JsonConverter<ArraySegment<TradeInfo>>
{
    private TradeInfo[] _trades = new TradeInfo[12];
    private int _count;
    
    public override ArraySegment<TradeInfo> Read(ref Utf8JsonReader json, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!json.Read() || json.TokenType != JsonTokenType.StartObject)
            throw new JsonException("StartObject '{' expected");
        
        /*{
             "topic": "publicTrade.BTCUSDT",
             "ts": 1774023924134,
             "type": "snapshot",
             "data": [
               {
                 "i": "2290000001077842686",
                 "T": 1774023924133,
                 "p": "69984.9",
                 "v": "0.000999",
                 "S": "Sell",
                 "seq": 102815014895,
                 "s": "BTCUSDT",
                 "BT": false,
                 "RPI": false
               }
             ]
           }*/
        var ok = false;

        // skip till 'data':[]
        while (json.TokenType != JsonTokenType.StartArray)
        {
            if (json.TokenType == JsonTokenType.PropertyName
                && json.ValueSpan.SequenceEqual("topic"u8))
            {
                Debug.Assert(json.Read() && json.ValueSpan.StartsWith("publicTrade."u8));
            }
            ok = json.Read();
            Debug.Assert(ok);
        }

        _count = 0;
        ok = json.Read();
        Debug.Assert(ok && json.TokenType == JsonTokenType.StartObject);
        while (json.TokenType != JsonTokenType.EndArray)
        {
            if (_count == _trades.Length)
                Array.Resize(ref _trades, _count * 2);
            
            ref var res = ref _trades[_count++];
            ref var info = ref res.Info;
            ref var trade = ref res.Trade;

            ok = json.Read();
            while (json.TokenType != JsonTokenType.EndObject)
            {
                Debug.Assert(ok && json.TokenType == JsonTokenType.PropertyName);
                var span = json.ValueSpan;
                var (p0, plen) = ((char)span[0], span.Length);
                ok = json.Read();
                Debug.Assert(ok);
                //TODO optimize to linear cases
                switch ((p0, plen))
                {
                    case ('i', _):
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        var gl = new GuidFromInt64();
                        ok = long.TryParse(json.ValueSpan, out gl.Lo);
                        Debug.Assert(ok);
                        trade.Id = gl.Guid;
                        break;
                    case ('T', _):
                        Debug.Assert(json.TokenType == JsonTokenType.Number);
                        info.Timestamp = json.GetInt64() * 1_000;
                        break;
                    case ('p', _):
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out trade.Price);
                        Debug.Assert(ok);
                        break;
                    case ('v', _):
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        ok = double.TryParse(json.ValueSpan, out var vol);
                        Debug.Assert(ok);
                        trade.Volume = vol;
                        break;
                    case ('S', _):
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        trade.TakerIsBuyer = json.ValueSpan[0] == 'B';
                        break;
                    case ('s', 1): // not 'seq'
                        Debug.Assert(json.TokenType == JsonTokenType.String);
                        info.Symbol = Str16.FromJsonUnsafe(json.ValueSpan, out var _);
                        break;
                }

                ok = json.Read();
                Debug.Assert(ok);
            }
            ok = json.Read();
            Debug.Assert(ok);
        }

        return new(_trades, 0, _count);
    }

    public override void Write(Utf8JsonWriter writer, ArraySegment<TradeInfo> value, JsonSerializerOptions options)
        => throw new NotImplementedException();

    [StructLayout(LayoutKind.Explicit)]
    private struct GuidFromInt64
    {
        [FieldOffset(0)] public Guid Guid;
        [FieldOffset(0)] public long Lo;
        [FieldOffset(8)] public long Hi;
    }
}
