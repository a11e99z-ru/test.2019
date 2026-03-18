namespace Microservices.Shared.Domain;

// WS=>
// KC=>, DB=>, gRPC=>
public interface IAsyncPublisher<T>
{
    // async cannot have /in/ params
    ValueTask PublishAsync(/*in*/ T value, CancellationToken ct);
}

public interface IAsyncDistributor<T>
    : IAsyncPublisher<T>
{
}
