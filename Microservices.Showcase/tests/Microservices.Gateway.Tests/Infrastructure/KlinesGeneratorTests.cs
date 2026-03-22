using Microservices.Gateway.Infrastructure;
using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;
using Xunit;

namespace Microservices.Gateway.Tests.Infrastructure;

public class KlinesGeneratorTests
{
    [Fact]
    public void Update_ShouldGenerateKlines_WhenPeriodChanges()
    {
        // Arrange
        var period = KlinePeriod.Sec10;
        var generator = new KlinesGenerator(period);
        var btc = Str16.FromAscii("BTCUSDT");
        var eth = Str16.FromAscii("ETHUSDT");

        // 1st trade initializes: 10:00:00.000 (Symbol: BTC)
        var t1 = new TradeInfo 
        { 
            Info = new DataInfo(btc, 1000000000), // 1000s = 1_000_000_000 mcs
            Trade = new Trade { Price = 100, Volume = 1, TakerIsBuyer = true }
        };
        
        // Act & Assert 1
        var klines1 = generator.Update(new[] { t1 });
        Assert.Equal(0, klines1.Count); // Initializing first bar, no kline returned yet

        // 2nd trade same symbol, same period: 10:00:05.000 (Symbol: BTC)
        var t2 = new TradeInfo 
        { 
            Info = new DataInfo(btc, 1005000000), 
            Trade = new Trade { Price = 105, Volume = 2, TakerIsBuyer = true }
        };

        // Act & Assert 2
        var klines2 = generator.Update(new[] { t2 });
        Assert.Equal(0, klines2.Count); // Still same bar, no kline returned

        // 3rd trade same symbol, NEW period: 10:00:11.000 (Symbol: BTC)
        var t3 = new TradeInfo 
        { 
            Info = new DataInfo(btc, 1011000000), 
            Trade = new Trade { Price = 110, Volume = 3, TakerIsBuyer = true }
        };

        // Act & Assert 3
        var klines3 = generator.Update(new[] { t3 });
        Assert.Single(klines3); // Bar 10:00:00 should be returned
        var k3 = klines3[0];
        Assert.Equal(btc, k3.Info.Symbol);
        Assert.Equal(1000000000, k3.Info.Timestamp);
        Assert.Equal(100, k3.Kline.Open);
        Assert.Equal(105, k3.Kline.High);
        Assert.Equal(100, k3.Kline.Low);
        Assert.Equal(105, k3.Kline.Close);
        Assert.Equal(3, k3.Kline.Volume);
        Assert.Equal(310, k3.Kline.Value);

        // 4th trade DIFFERENT symbol: 10:00:12.000 (Symbol: ETH)
        var t4 = new TradeInfo 
        { 
            Info = new DataInfo(eth, 1012000000), 
            Trade = new Trade { Price = 2000, Volume = 0.5, TakerIsBuyer = false }
        };

        // Act & Assert 4
        var klines4 = generator.Update(new[] { t4 });
        Assert.Equal(0, klines4.Count); // First bar for ETH, no kline returned
    }
}
