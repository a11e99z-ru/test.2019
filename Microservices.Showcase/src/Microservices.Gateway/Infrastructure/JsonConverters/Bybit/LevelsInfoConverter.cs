using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Gateway.Infrastructure.JsonConverters.Bybit;

public sealed class LevelsInfoConverter
    : JsonConverter<Levels10Info>
{
    private readonly Dictionary<Str16, Levels> _lobs = new(); 
    private readonly Levels _lvls = new();
    
    private Str16 _sym = Str16.Empty;
    private long _ts;
    private bool _isDelta;

    public override Levels10Info Read(ref Utf8JsonReader json, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!json.Read() || json.TokenType != JsonTokenType.StartObject)
            throw new JsonException("StartObject '{' expected");
        
        /*{
             "topic": "orderbook.50.BTCUSDT",
             "ts": 1774088848049,
             "type": "delta",
             "data": {
               "s": "BTCUSDT",
               "b": [
                 ["70579.9", "1.102041"],
                 ["70579.7", "0.014168"]
               ],
               "a": [
                 ["70580", "0.02757"],
                 ["70585.9", "0"]
               ],
               "u": 7528003,
               "seq": 102879627831
             },
             "cts": 1774088848047
           }*/

        var ok = false;
        (_sym, _ts, _isDelta) = (Str16.Empty, 0L, false);
        while (json.Read() && json.TokenType != JsonTokenType.EndObject)
        {
            Debug.Assert(json.TokenType == JsonTokenType.PropertyName);
            var (p0, plen) = ((char)json.ValueSpan[0], json.ValueSpan.Length);
            ok = json.Read();
            Debug.Assert(ok);
            switch ((p0, plen))
            {
                case ('t', 5): // topic
                    Debug.Assert(json.TokenType == JsonTokenType.String);
                    var dpos = json.ValueSpan.LastIndexOf((byte)'.');
                    Debug.Assert(dpos > 0);
                    _sym = Str16.FromJsonUnsafe(json.ValueSpan[++dpos..], out var _);
                    break;
                case ('t',2): // ts
                    Debug.Assert(json.TokenType == JsonTokenType.Number);
                    _ts = json.GetInt64();
                    break;
                case ('t',4): // type
                    Debug.Assert(json.TokenType == JsonTokenType.String);
                    _isDelta = json.ValueSpan[0] == (byte)'d';
                    break;
                case ('d',4): // data
                    //Debug.Assert(_sym != Str16.Empty && _lvls != null);
                    ReadLevels(ref json, _lvls);
                    break;
            }
        }
        Debug.Assert(_sym != Str16.Empty && _ts > 0);
        if (!_lobs.TryGetValue(_sym, out var lvls))
            _lobs.Add(_sym, lvls = new());

        if (_isDelta)
            lvls.UpdateFrom(_lvls);
        else 
            lvls.CopyFrom(_lvls);

        return lvls.ToLevels10(_sym, _ts);
    }

    private static void ReadLevels(ref Utf8JsonReader json, Levels items)
    {
        items.Clear();
        Debug.Assert(json.TokenType == JsonTokenType.StartObject);
        while (json.Read() && json.TokenType != JsonTokenType.EndObject)
        {
            Debug.Assert(json.TokenType == JsonTokenType.PropertyName);
            if (json.ValueSpan[0] == (byte)'a')
                ReadLevels(ref json, items.Asks);
            else if (json.ValueSpan[0] == (byte)'b')
                ReadLevels(ref json, items.Bids);
            else
                json.Read();
        }
    }

    private static void ReadLevels(ref Utf8JsonReader json, List<PriceLevel> items)
    {
        json.Read();
        Debug.Assert(json.TokenType == JsonTokenType.StartArray);
        while (json.Read() && json.TokenType != JsonTokenType.EndArray)
        {
            Debug.Assert(json.TokenType == JsonTokenType.StartArray);
            
            json.Read();
            Debug.Assert(json.TokenType == JsonTokenType.String);
            var price = double.Parse(json.ValueSpan);

            json.Read();
            Debug.Assert(json.TokenType == JsonTokenType.String);
            var volume = double.Parse(json.ValueSpan);
            
            items.Add(new PriceLevel(price, volume));

            json.Read();
            Debug.Assert(json.TokenType == JsonTokenType.EndArray);
        }
    }

    public override void Write(Utf8JsonWriter writer, Levels10Info value, JsonSerializerOptions options)
        => throw new NotImplementedException();
    
    #region Update levels
    private record Levels
    {
        public readonly List<PriceLevel> Asks = new();
        public readonly List<PriceLevel> Bids = new();

        public Levels Clear()
        {
            Asks.Clear();
            Bids.Clear();
            return this;
        }

        public void CopyFrom(Levels lvls)
        {
            Clear();
            Asks.AddRange(lvls.Asks);
            Bids.AddRange(lvls.Bids);
        }

        public void UpdateFrom(Levels delta)
        {
            Update(Asks, delta.Asks, _cmpAsks);
            Update(Bids, delta.Bids, _cmpBids);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Update(List<PriceLevel> dst, List<PriceLevel> upds, IComparer<PriceLevel> cmp)
            {
                var dasks = CollectionsMarshal.AsSpan(upds); 
                foreach (ref readonly var a in dasks)
                {
                    var upd = a.Volume > 0; 
                    var pos = dst.BinarySearch(a, cmp);
                    if (pos >= 0)
                    {
                        if (upd)
                            dst[pos] = a;
                        else
                            dst.RemoveAt(pos);
                    }
                    else if (upd)
                        dst.Insert(~pos, a);
                }
            }
        }

        public Levels10Info ToLevels10(Str16 sym, long timestamp)
        {
            var res = new Levels10Info();
            res.Info = new(sym, timestamp * 1_000);
            ref var l10 = ref res.Levels;
            for (var k = 0; k < 10; ++k)
            {
                l10.Asks[k] = Asks[k];
                l10.Bids[k] = Bids[k];
            }
            return res;
        }
        #endregion

        #region Comparers
        private static readonly AsksComparer _cmpAsks = new();
        private static readonly BidsComparer _cmpBids = new();

        private sealed class AsksComparer
            : IComparer<PriceLevel>
        {
            private const double Eps = 1e-5;
            public int Compare(PriceLevel x, PriceLevel y)
                => Math.Abs(x.Price - y.Price) <= Eps
                    ? 0
                    : x.Price.CompareTo(y.Price);
        }

        private sealed class BidsComparer
            : IComparer<PriceLevel>
        {
            private const double Eps = 1e-5;
            public int Compare(PriceLevel x, PriceLevel y)
                => Math.Abs(x.Price - y.Price) <= Eps
                    ? 0
                    : y.Price.CompareTo(x.Price);
        }
        #endregion Comparers
    }
}
