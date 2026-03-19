using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;
using Microservices.Shared.Domain.Persistence;
using Xunit;

namespace Microservices.Shared.Tests.Domain.Persistense;

public class PersistenseTests
{
    [Fact]
    public void ToTradeDto_ShouldMapCorrectly()
    {
        // Arrange
        var symbol = Str16.FromAscii("BTCUSDT");
        var timestamp = new DateTime(2023, 10, 27, 10, 0, 0, DateTimeKind.Utc);
        var tradeId = Guid.NewGuid();
        var price = 34000.5;
        var volume = 1.2;
        var takerIsBuyer = true;

        var tradeInfo = new TradeInfo
        {
            Info = new DataInfo(symbol, timestamp),
            Trade = new Trade
            {
                Id = tradeId,
                Price = price,
                Volume = volume,
                TakerIsBuyer = takerIsBuyer
            }
        };

        // Act
        var dto = new TradeEntity();
        dto.CopyFrom(in tradeInfo);

        // Assert
        Assert.Equal(timestamp, dto.Timestamp);
        Assert.Equal("BTCUSDT", dto.Symbol);
        Assert.Equal(tradeId, dto.TradeId);
        Assert.Equal(price, dto.Price);
        Assert.Equal(volume, dto.Volume);
        Assert.Equal(takerIsBuyer, dto.TakerIsBuyer);
    }

    [Fact]
    public void ToTradeInfo_ShouldMapCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2023, 10, 27, 10, 0, 0, DateTimeKind.Utc);
        var dto = new TradeEntity
        {
            Timestamp = timestamp,
            Symbol = "ETHUSDT",
            TradeId = Guid.NewGuid(),
            Price = 1800.75,
            Volume = 5.5,
            TakerIsBuyer = false
        };

        // Act
        var tradeInfo = new TradeInfo();
        dto.CopyTo(ref tradeInfo);

        // Assert
        Assert.Equal(timestamp, tradeInfo.Info.EventTime);
        Assert.Equal("ETHUSDT", tradeInfo.Info.Symbol.ToString());
        Assert.Equal(dto.TradeId, tradeInfo.Trade.Id);
        Assert.Equal(dto.Price, tradeInfo.Trade.Price);
        Assert.Equal(dto.Volume, tradeInfo.Trade.Volume);
        Assert.Equal(dto.TakerIsBuyer, tradeInfo.Trade.TakerIsBuyer);
    }

    [Fact]
    public void RoundTrip_ShouldReturnEquivalentObject()
    {
        // Arrange
        var symbol = Str16.FromAscii("BNBUSDT");
        // Use a timestamp with microsecond precision to ensure round-trip works
        var timestamp = new DateTime(2023, 10, 27, 10, 0, 0, 123, DateTimeKind.Utc).AddMicroseconds(456);
        
        var original = new TradeInfo
        {
            Info = new DataInfo(symbol, timestamp),
            Trade = new Trade
            {
                Id = Guid.NewGuid(),
                Price = 225.4,
                Volume = 10.0,
                TakerIsBuyer = true
            }
        };

        // Act
        var dto = new TradeEntity();
        dto.CopyFrom(in original);
        var roundTripped = new TradeInfo();
        dto.CopyTo(ref roundTripped);

        // Assert
        Assert.Equal(original.Info.Symbol, roundTripped.Info.Symbol);
        Assert.Equal(original.Info.Timestamp, roundTripped.Info.Timestamp);
        Assert.Equal(original.Info.EventTime, roundTripped.Info.EventTime);
        Assert.Equal(original.Trade.Id, roundTripped.Trade.Id);
        Assert.Equal(original.Trade.Price, roundTripped.Trade.Price);
        Assert.Equal(original.Trade.Volume, roundTripped.Trade.Volume);
        Assert.Equal(original.Trade.TakerIsBuyer, roundTripped.Trade.TakerIsBuyer);
        Assert.Equal(original, roundTripped);
    }
}
