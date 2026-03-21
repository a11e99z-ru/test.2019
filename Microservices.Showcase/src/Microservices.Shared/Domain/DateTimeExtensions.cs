namespace Microservices.Shared.Domain;

public static class DateTimeExtensions
{
    public static long ToMilliseconds(this DateTime time)
        => (long)(time - DateTime.UnixEpoch).TotalMilliseconds;
    
    public static long ToMicroseconds(this DateTime time)
        => (long)(time - DateTime.UnixEpoch).TotalMicroseconds;

    public static DateTime FromMilliseconds(this long millis)
        => DateTime.UnixEpoch.AddMilliseconds(millis);

    public static DateTime FromMicroseconds(this long micros)
        => DateTime.UnixEpoch.AddMicroseconds(micros);
}
