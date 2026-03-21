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
}

public record struct TradeInfo
{
    public DataInfo Info;

    public Trade Trade;
}
