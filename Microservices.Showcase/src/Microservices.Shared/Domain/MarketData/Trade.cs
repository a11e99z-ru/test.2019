namespace Microservices.Shared.Domain.MarketData;

public record struct Trade
{
    public Guid Id; //> i	string	Trade ID

    public double Price; //> p	string	Trade price

    private double _vol;
    public double Volume //> v	string	Trade size
    {
        readonly get => Math.Abs(_vol);
        set => _vol = TakerIsBuyer ? -value : value;
    }

    //OPT dont want memory for bool flag
    public bool TakerIsBuyer //> S	string	Side of taker. Buy,Sell
    {
        readonly get => _vol < 0;
        set => _vol = value ? -Volume : Volume;
    }

    //> L	string	Direction of price change. Unique field for Perps & futures
    //> BT	boolean	Whether it is a block trade order or not
    //> RPI	boolean	Whether it is a RPI trade or not
    //> seq	integer	cross sequence
    //> mP	string	Mark price, unique field for option
    //> iP	string	Index price, unique field for option
    //> mIv	string	Mark iv, unique field for option
    //> iv	string	iv, unique field for option}
}

public record struct TradeInfo
{
    public DataInfo Info;

    public Trade Trade;
}
