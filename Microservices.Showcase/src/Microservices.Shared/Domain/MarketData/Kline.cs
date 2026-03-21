namespace Microservices.Shared.Domain.MarketData;

public record struct Kline
{
    public double Open;
    public double High;
    public double Low;
    public double Close;

    public double Volume;
    public double Value;
}

public record struct KlineInfo
{
    public DataInfo Info;
    public KlinePeriod Period;

    public DateTime OpenTime => Info.EventTime; // includes
    public DateTime CloseTime => Info.EventTime.AddSeconds(Period.ToSeconds()); // excludes

    public Kline Kline;
}

public enum KlinePeriod : int
{
    None,
    
    Sec1 = 1,
    Sec2 = Sec1 * 2,
    Sec3 = Sec1 * 3,
    Sec5 = Sec1 * 5,
    Sec10 = Sec1 * 10,
    Sec15 = Sec1 * 15,
    Sec20 = Sec1 * 20,
    Sec30 = Sec1 * 30,
    
    Min1 = Sec1 * 60,
    Min2 = Min1 * 2,
    Min3 = Min1 * 3,
    Min5 = Min1 * 5,
    Min10 = Min1 * 10,
    Min15 = Min1 * 15,
    Min20 = Min1 * 20,
    Min30 = Min1 * 30,
    
    Hour1 = Min1 * 60,
    Hour2 = Hour1 * 2,
    Hour3 = Hour1 * 3,
    Hour4 = Hour1 * 4,
    Hour6 = Hour1 * 6,
    Hour8 = Hour1 * 8,
    Hour12 = Min1 * 12,
	
    Day1 = Hour1 * 24,
    Day2 = Day1 * 2,
    Day3 = Day1 * 3,

    Week1 = Day1 * 7,
    Week2 = Week1 * 2,

    Month1 = 10_000_000,
    Month3 = Month1 * 3,
    Month6 = Month1 * 6,
    
    Year1 = Month1 * 12,
}

public static class KlineExtensions
{
    extension(KlinePeriod period)
    {
        public int ToMonths()
            => (int)period / (int)KlinePeriod.Month1;

        public int ToSeconds()
            => (int)period < (int)KlinePeriod.Month1
                ? (int)period
                : throw new ArgumentException("Months cannot be converted to exactly seconds.");
    }

    public static KlinePeriod ToKlinePeriod(this int seconds)
        => (KlinePeriod)seconds;
}
