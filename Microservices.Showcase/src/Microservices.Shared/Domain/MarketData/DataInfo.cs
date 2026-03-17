namespace Microservices.Shared.Domain.MarketData;

public record struct DataInfo(Str16 Symbol, long TimestampMcs)
{
    public DateTime Timestamp => DateTime.UnixEpoch.AddMicroseconds(TimestampMcs);
}
