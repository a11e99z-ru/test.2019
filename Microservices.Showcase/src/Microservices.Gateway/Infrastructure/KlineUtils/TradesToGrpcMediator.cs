using Microservices.Shared.Domain;
using Microservices.Shared.Domain.MarketData;

namespace Microservices.Gateway.Infrastructure.KlineUtils;

public interface IDataMediator
{
    event Action<string> OnError;
    event Action<TradeInfo> OnTrade;
}

// connects Trade producer with gRPC servers/clients
public class TradesToGrpcMediator
    : IDataMediator
    , IAsyncPublisher<TradeInfo>
    , IAsyncDistributor<string>
{
    public event Action<string> OnError;
    public event Action<TradeInfo> OnTrade;
    
    public async ValueTask PublishAsync(TradeInfo value, CancellationToken ct)
        => OnTrade?.Invoke(value);

    public async ValueTask PublishAsync(string value, CancellationToken ct)
        => OnError?.Invoke(value);
}
