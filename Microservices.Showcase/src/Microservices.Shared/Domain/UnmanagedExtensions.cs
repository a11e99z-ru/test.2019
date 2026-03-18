using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microservices.Shared.Domain;

public static class UnmanagedExtensions
{
    public static ReadOnlySpan<byte> ToBytes<T>(this ref T value)
        where T : unmanaged
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));

    public static ReadOnlySpan<byte> ToBytes<T>(this T[] value)
        where T : unmanaged
        => MemoryMarshal.AsBytes(value.AsSpan());

    public static ref readonly T AsRef<T>(this ReadOnlySpan<byte> value)
        where T : unmanaged
    {
        Debug.Assert(Unsafe.SizeOf<T>() <= value.Length);
        return ref MemoryMarshal.AsRef<T>(value);
    }

    public static ReadOnlySpan<T> ToSpan<T>(this ReadOnlySpan<byte> value)
        where T : unmanaged
        => MemoryMarshal.Cast<byte, T>(value);
}
