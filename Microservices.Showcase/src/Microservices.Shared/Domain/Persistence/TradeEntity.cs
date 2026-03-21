using LinqToDB.Mapping;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Shared.Domain.Persistence;

[Table("trades")]
public sealed record TradeEntity
    : ICopyable<TradeInfo>
{
    [Column("trade_id"), PrimaryKey]
    public Guid TradeId { get; set; }

    // for notifications
    [Column("rowid"), Identity] 
    public long RowId { get; set; }
    
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("symbol")]
    public string Symbol { get; set; } = "";
    
    [Column("price")]
    public double Price { get; set; }
    
    [Column("volume")]
    public double Volume { get; set; }

    [Column("is_buy")]
    public bool TakerIsBuyer { get; set; }

    public void CopyFrom(in TradeInfo value)
    {
        ref readonly var info = ref value.Info;
        Timestamp = info.EventTime;
        Symbol = info.Symbol.ToString();
        
        ref readonly var trade = ref value.Trade;
        TradeId = trade.Id;
        Price = trade.Price;
        Volume = trade.Volume;
        TakerIsBuyer = trade.TakerIsBuyer;
    }

    public void CopyTo(ref TradeInfo value)
    {
        value.Info = new(Str16.FromAscii(Symbol), Timestamp);
        value.Trade = new()
        {
            Id = TradeId,
            Price = Price,
            Volume = Volume,
            TakerIsBuyer = TakerIsBuyer
        };
    }
}
