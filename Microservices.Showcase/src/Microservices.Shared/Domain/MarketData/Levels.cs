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
public record struct Levels10
{
    private const int ItemCount = 10;
    
    public Items Asks;
    public Items Bids;
    
    public PriceLevel BestAsk => Asks[0];
    public PriceLevel BestBid => Bids[0];
    
    [InlineArray(ItemCount)]
    public struct Items
    {
        public PriceLevel _;
    }

    public double AsksVolume => SumVolume(ref Asks);
    public double BidsVolume => SumVolume(ref Bids);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumVolume(ref Items items)
    {
        var res = 0.0;
        for (var k = 0; k < ItemCount; ++k)
            res += items[k].Volume;
        return res;
    }
}

public record struct Levels10Info
{
    public DataInfo Info;

    public Levels10 Levels;
}
#endregion
