using Microservices.Shared.Helpers;
using Xunit;

namespace Microservices.Shared.Tests.Helpers;

public class DeferTests
{
    [Fact]
    public void Defer_CalledInNormalPath()
    {
        var cnt = 0;
        {
            using var _ = Defer.It(() => cnt++);
        }
        Assert.Equal(1, cnt);

        {
            using var _ = Defer.It(() => cnt++);
        }
        Assert.Equal(2, cnt);
    }

    [Fact]
    public void Defer_CalledInExceptionPath()
    {
        var cnt = 0;
        try
        {
            using var _ = Defer.It(() => cnt++);
            throw new Exception();
        }
        catch {}
        Assert.Equal(1, cnt);

        try
        {
            using var _ = Defer.It(() => cnt++);
            //throw new Exception();
        }
        catch {}
        Assert.Equal(2, cnt);
    }

    [Fact]
    public void Defer_CalledInEachIteration()
    {
        const int N = 10;
        int cnt = 0;
        for (var n = 0; n < N; ++n)
        {
            using var _ = Defer.It(() => cnt++);
        }
        Assert.Equal(10, cnt);
    }
    
    [Fact]
    public void Defer_NotCalledWithoutUsing()
    {
        var cnt = 0;
        {
            /*using*/ var _ = Defer.It(() => cnt++);
        }
        Assert.Equal(0, cnt);
    }
}
