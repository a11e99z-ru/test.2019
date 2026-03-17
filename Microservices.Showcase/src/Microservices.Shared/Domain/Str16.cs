using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Microservices.Shared.Domain;

/// <summary>
/// Short ASCII string with 16 chars.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct Str16
    : IEquatable<Str16>
{
    public const int MaxLength = 16;
    
    private const int BitMaxLength = 1 << MaxLength;
    
    [FieldOffset(0)] private Vector128<byte> _str;

    [FieldOffset(0)] private long lo;
    [FieldOffset(8)] private long hi;

    [FieldOffset(0)] private fixed byte _buf[MaxLength];

    public static Str16 Empty => new(Vector128<byte>.Zero);

    public readonly int Length
    {
        get
        {
            var m = Sse2.CompareEqual(_str, Vector128<byte>.Zero);
            return BitOperations.TrailingZeroCount(Sse2.MoveMask(m) | BitMaxLength);
        }
    }
        
    public readonly ReadOnlySpan<byte> Span 
        => new(Unsafe.AsPointer(in _buf[0]), Length);

    public readonly Vector128<byte> SseData => _str;

    public byte this[int index] => _buf[index];
    public byte this[Index index] => _buf[index.GetOffset(Length)];

    private Str16(Vector128<byte> data) => _str = data;

    public Str16(ReadOnlySpan<byte> span)
    {
        _str = Vector128<byte>.Zero;
        var sp = new Span<byte>(Unsafe.AsPointer(in _buf[0]), MaxLength);
        span[..Math.Min(span.Length, MaxLength)].CopyTo(sp);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Str16 FromAscii(ReadOnlySpan<char> span)
    {
        var res = Empty;
        var len = Math.Min(span.Length, MaxLength);
        for (var k = 0; k < len; ++k)
            res._buf[k] = (byte)span[k];
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Str16 FromBytes(ReadOnlySpan<byte> span)
    {
        var res = Empty;
        var sp = new Span<byte>(res._buf, MaxLength);
        span[..Math.Min(span.Length, MaxLength)].CopyTo(sp);
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Str16 FromBytesUnsafe(ReadOnlySpan<byte> span) // dont read behind borders
    {
        var len = Math.Min(span.Length, MaxLength);
        if (len == 0) return Empty;
        var str = Vector128.LoadUnsafe(in Unsafe.AsRef(in span[0]));
        str = Sse2.And(str, Zeroes[len]);
        return new(str);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Str16 FromJsonUnsafe(ReadOnlySpan<byte> span, out int consumed)
    {
        // span should start after '\"'
        Debug.Assert(((byte*)Unsafe.AsPointer(in Unsafe.AsRef(in span[0])))[-1] == '\"');
        
        var str = Vector128.LoadUnsafe(in Unsafe.AsRef(in span[0]));
        var m = Sse2.CompareEqual(str, Vector128.Create((byte)'\"'));
        consumed = BitOperations.TrailingZeroCount(Sse2.MoveMask(m) | BitMaxLength);
        str = Sse2.And(str, Zeroes[consumed]);
        return new(str);
    }

    public readonly override int GetHashCode()
        => (lo ^ (hi << 1)).GetHashCode();

    public readonly bool Equals(Str16 other) => _str == other._str;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Str16 other && Equals(other);

    public override string ToString()
        => new((sbyte*)Unsafe.AsPointer(in _buf[0]), 0, Length);
    //public string ToCachedString() => FindNameFor(this); //TODO supports dic for noGC

    public static bool operator ==(Str16 a, Str16 b) => a.Equals(b);
    public static bool operator !=(Str16 a, Str16 b) => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public readonly Str16 ToLower()
    {
        // find letters A..Z and make it lower
        var str = _str.As<byte, sbyte>();
        var am1 = Vector128.Create((sbyte)('A' - 1));
        var geA = Sse2.CompareGreaterThan(str, am1);
        var zp1 = Vector128.Create((sbyte)('Z' + 1));
        var leZ = Sse2.CompareLessThan(str, zp1);
        var m = Sse2.And(geA, leZ);
        m = Sse2.And(m, Vector128.Create((sbyte)('a' - 'A')));
        return new(Sse2.Add(m, str).As<sbyte, byte>());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public readonly Str16 ToUpper()
    {
        // find letters a..z and make it upper
        var str = _str.As<byte, sbyte>();
        var am1 = Vector128.Create((sbyte)('a' - 1));
        var geA = Sse2.CompareGreaterThan(str, am1);
        var zp1 = Vector128.Create((sbyte)('z' + 1));
        var leZ = Sse2.CompareLessThan(str, zp1);
        var m = Sse2.And(geA, leZ);
        m = Sse2.And(m, Vector128.Create((sbyte)('a' - 'A')));
        return new(Sse2.Subtract(str, m).As<sbyte, byte>());
    }

    #region internals
    private const byte S00 = 0, Sff = 0xff;
    private static readonly Vector128<byte>[] Zeroes =
    [
        Vector128.Create(S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 0
        Vector128.Create(Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 1
        Vector128.Create(Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 2
        Vector128.Create(Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 3
        Vector128.Create(Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 4
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 5
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 6
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00, S00), // 7
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00, S00), // 8
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00, S00), // 9
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00, S00), // 10
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00, S00), // 11
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00, S00), // 12
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00, S00), // 13
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00, S00), // 14
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, S00), // 15
        Vector128.Create(Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff, Sff), // 16
    ];
    #endregion
}
