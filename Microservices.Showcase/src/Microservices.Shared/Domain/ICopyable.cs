namespace Microservices.Shared.Domain;

public interface ICopyable<T>
    where T : struct
{
    void CopyFrom(in T value);
    void CopyTo(ref T value);
}
