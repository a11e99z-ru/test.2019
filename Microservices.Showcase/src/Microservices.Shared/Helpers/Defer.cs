namespace Microservices.Shared.Helpers;

/*
// instead

    var arr = ArrayPool<byte>.Shared.Rent( 4 * 1024 );
    try
    {
        // do something that can throw
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(arr);
    }

// we can use

    var arr = ArrayPool<byte>.Shared.Rent( 4 * 1024 );
    using var _ = Defer.It(arr, it => ArrayPool<byte>.Shared.Return(it)); // no lambda captures, no GC
    // do something that can throw
*/

public readonly record struct Defer(Action Free)
    : IDisposable
{
    public readonly void Dispose() => Free();

    public static Defer It(Action free)
        => new(free);
    public static Defer<T> It<T>(T arg, Action<T> todo)
        => new(arg, todo);
    public static Defer<T1, T2> It<T1, T2>(T1 arg1, T2 arg2, Action<T1, T2> todo)
        => new(arg1, arg2, todo);
    public static Defer<T1, T2, T3> It<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<T1, T2, T3> todo)
        => new(arg1, arg2, arg3, todo);
}

public readonly record struct Defer<T>(T Arg, Action<T> Free)
    : IDisposable
{
    public void Dispose() => Free(Arg);
}

public readonly record struct Defer<T1, T2>(T1 Arg1, T2 Arg2, Action<T1, T2> Free)
    : IDisposable
{
    public void Dispose() => Free(Arg1, Arg2);
}

public readonly record struct Defer<T1, T2, T3>(T1 Arg1, T2 Arg2, T3 Arg3, Action<T1, T2, T3> Free)
    : IDisposable
{
    public void Dispose() => Free(Arg1, Arg2, Arg3);
}
