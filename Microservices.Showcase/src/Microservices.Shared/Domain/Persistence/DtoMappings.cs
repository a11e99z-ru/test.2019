using Microservices.Shared.Domain.Persistence;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Shared.Domain.Persistence;

public static class DtoMappings
{
    public static TradeDto ToTradeDto(this in TradeInfo trade)
    {
        return new()
        {
            Timestamp = trade.Info.EventTime,
            Symbol = trade.Info.Symbol.ToString(),
            TradeId = trade.Trade.Id,
            Price = trade.Trade.Price,
            Volume = trade.Trade.Volume,
            TakerIsBuyer = trade.Trade.TakerIsBuyer,
        };
    }

    public static TradeInfo ToTradeInfo(this TradeDto trade)
    {
        return new()
        {
            Info = new(Str16.FromAscii(trade.Symbol), trade.Timestamp),
            Trade = new()
            {
                Id = trade.TradeId, 
                Price = trade.Price, 
                Volume = trade.Volume, 
                TakerIsBuyer = trade.TakerIsBuyer
            },
        };
    }
}
