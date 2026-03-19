using Microservices.Shared.Domain;
using Xunit;

namespace Microservices.Shared.Tests.Domain;

public class ExtensionsTests
{
    private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
    
    [Fact]
    public void DateTime_ToFromMilliseconds()
    {
        const long PerYear = 33_557_600L * 1_000; 
        
        var time = UnixEpoch.AddMilliseconds(1);
        Assert.True(time.ToMilliseconds() == 1);
        Assert.Equal(time.ToMilliseconds().FromMilliseconds(), time);

        time = UnixEpoch.AddMilliseconds(-1);
        Assert.True(time.ToMilliseconds() == -1);
        Assert.Equal(time.ToMilliseconds().FromMilliseconds(), time);
        
        time = UnixEpoch.AddMilliseconds(0);
        Assert.True(time.ToMilliseconds() == 0);
        Assert.Equal(time.ToMilliseconds().FromMilliseconds(), time);

        time = UnixEpoch.AddMilliseconds(2 * PerYear);
        Assert.True(time.ToMilliseconds() == 2 * PerYear);
        Assert.Equal(time.ToMilliseconds().FromMilliseconds(), time);
    }

    [Fact]
    public void DateTime_ToFromMicroseconds()
    {
        const long PerYear = 33_557_600L * 1_000_000; 
        
        var time = UnixEpoch.AddMicroseconds(1);
        Assert.True(time.ToMicroseconds() == 1);
        Assert.Equal(time.ToMicroseconds().FromMicroseconds(), time);

        time = UnixEpoch.AddMicroseconds(-1);
        Assert.True(time.ToMicroseconds() == -1);
        Assert.Equal(time.ToMicroseconds().FromMicroseconds(), time);
        
        time = UnixEpoch.AddMicroseconds(0);
        Assert.True(time.ToMicroseconds() == 0);
        Assert.Equal(time.ToMicroseconds().FromMicroseconds(), time);

        time = UnixEpoch.AddMilliseconds(123);
        Assert.True(time.ToMicroseconds() == 123_000);
        Assert.Equal(time.ToMicroseconds().FromMicroseconds(), time);

        time = UnixEpoch.AddMicroseconds(2 * PerYear);
        Assert.True(time.ToMicroseconds() == 2 * PerYear);
        Assert.Equal(time.ToMicroseconds().FromMicroseconds(), time);

        Assert.Equal((time.ToMicroseconds() / 1_000).FromMilliseconds(), time);
    }
}
