using Microservices.Shared.Domain;

namespace Microservices.Shared.Domain.MarketData;

public record struct DataInfo(Str16 Symbol, long Timestamp)
{
    public DataInfo(Str16 Symbol, DateTime time)
        : this(Symbol, time.ToMicroseconds()) { }

    public DateTime EventTime 
        => DateTime.UnixEpoch.AddMicroseconds(Timestamp);
}
