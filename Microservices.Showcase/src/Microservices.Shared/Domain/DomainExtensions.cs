namespace Microservices.Shared.Domain;

public static class DomainExtensions
{
    public static long ToMilliseconds(this DateTime time)
        => (long)(time - DateTime.UnixEpoch).TotalMilliseconds;
    
    public static long ToMicroseconds(this DateTime time)
        => (long)(time - DateTime.UnixEpoch).TotalMicroseconds;
}
