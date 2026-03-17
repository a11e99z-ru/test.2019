using System.Runtime.CompilerServices;

namespace Microservices.Shared.Domain.MarketData;

#region Level1
public record struct Level1
{
    public PriceLevel BestAsk;
    public PriceLevel BestBid;
}

public record struct Level1Info
{
    public DataInfo Info;

    public Level1 Level;
}
#endregion

#region Levels10
public record struct Levels
{
    public Items Asks = new();
    public Items Bids = new();
    
    public Levels() { }

    public PriceLevel BestAsk => Asks[0];
    public PriceLevel BestBid => Bids[0];
    
    [InlineArray(10)]
    public struct Items
    {
        public PriceLevel _;
    }
}

public record struct Levels10Info
{
    public DataInfo Info;

    public Levels Levels;
}
#endregion
