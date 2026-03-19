namespace Microservices.Shared.Domain;

public interface IAsyncPublisher<in T>
{
    ValueTask PublishAsync(T value, CancellationToken ct);
}

public interface IAsyncDistributor<in T>
    : IAsyncPublisher<T>
{
}
