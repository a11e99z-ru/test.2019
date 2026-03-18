using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Microservices.Shared.Domain;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace Microservices.Showcase.Benchmarks;

[MemoryDiagnoser(true)]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, iterationCount: 10)]
public class Str16Bench
{
    private static readonly byte[] Bytes = """btc/USDT","f":2,"""u8.ToArray();
    private static readonly Str16 BtcUsdt = Str16.FromBytes(Bytes.AsSpan()[..8]);  
    
    [Benchmark(Baseline = true), BenchmarkCategory("ToLower")]
    public Str16 Str16_ToLower() => BtcUsdt.ToLower();

    [Benchmark, BenchmarkCategory("ToLower")]
    public Str16 ManualToLower()
    {
        const uint CmpZ = 'Z' - 'A', AddC = 'a' - 'A';
        Span<byte> buf = stackalloc byte[Str16.MaxLength];
        var len = BtcUsdt.Length;
        for (var k = 0; k < len; ++k)
        {
            var ch = BtcUsdt[k];
            buf[k] = (uint)(ch - (byte)'A') <= CmpZ ? (byte)(ch + AddC) : ch;
        }
        return new(buf[..len]);
    }
    
    [Benchmark(Baseline = true), BenchmarkCategory("HashCode")]
    public int Str16_GetHashCode() => BtcUsdt.GetHashCode();

    [Benchmark, BenchmarkCategory("HashCode")]
    public int HashCode_Vec128() // shows cost of native Vec128.HashCode 
        => BtcUsdt.SseData.GetHashCode();

    [Benchmark, BenchmarkCategory("HashCode")]
    public int HashCode_CombineTwoInt64()
    {
        var sse = BtcUsdt.SseData;
        ref var i64 = ref Unsafe.As<Vector128<byte>, long>(ref sse);
        return HashCode.Combine(i64, Unsafe.Add(ref i64, 1));
    }

    [Benchmark(Baseline = true), BenchmarkCategory("RawData")]
    public Vector128<byte> Str16_SseData() => BtcUsdt.SseData;

    // shows cost of Span(.Length)
    [Benchmark, BenchmarkCategory("RawData")]
    public int Str16_Span() => BtcUsdt.Span.Length;

    [Benchmark(Baseline = true), BenchmarkCategory("Read")]
    public Str16 Str16_FromBytesUnsafe() 
        => Str16.FromBytesUnsafe(Bytes.AsSpan()[..8]);
    
    [Benchmark, BenchmarkCategory("Read")]
    public Str16 Str16_FromBytes() 
        => Str16.FromBytes(Bytes.AsSpan()[..8]);

    [Benchmark(Baseline = true), BenchmarkCategory("Json")]
    public int Str16_FromJsonUnsafe_ShortStr()
    {
        var json = """usdt","pi":3.1415926535,"""u8; // 4 chars
        var str = Str16.FromJsonUnsafe(json, out var len);
        return len;
    }

    [Benchmark, BenchmarkCategory("Json")]
    public int Str16_FromJsonUnsafe_LongStr()
    {
        var json = """btcUSDT_240501","f":2,"""u8; // 14 chars
        var str = Str16.FromJsonUnsafe(json, out var len);
        return len;
    }

    [Benchmark, BenchmarkCategory("Json")]
    public int Span_ReadFromJson_ShortStr()
    {
        var json = """usdt","pi":3.1415926535,"""u8; // 4 chars
        // we expect '\"'
        return json[..json.IndexOf((byte)'\"')].Length; // really find Span then it Length
    }

    [Benchmark, BenchmarkCategory("Json")]
    public int Span_ReadFromJson_LongStr()
    {
        var json = """btcUSDT_240501","f":2,"""u8; // 14 chars
        // we expect '\"'
        return json[..json.IndexOf((byte)'\"')].Length; // really find Span then it Length
    }
}

/* Results on AMD 6800H
| Method                        | Categories | Mean       | Error     | StdDev    | Ratio  | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |----------- |-----------:|----------:|----------:|-------:|--------:|----------:|------------:|
| Str16_GetHashCode             | HashCode   |  0.2997 ns | 0.0119 ns | 0.0079 ns |   1.00 |    0.04 |         - |          NA |
| HashCode_Vec128               | HashCode   | 46.9429 ns | 1.5248 ns | 1.0086 ns | 156.74 |    5.07 |         - |          NA |
| HashCode_CombineTwoInt64      | HashCode   |  5.7837 ns | 0.0341 ns | 0.0203 ns |  19.31 |    0.49 |         - |          NA |
|                               |            |            |           |           |        |         |           |             |
| Str16_FromJsonUnsafe_ShortStr | Json       |  0.6735 ns | 0.0104 ns | 0.0069 ns |   1.00 |    0.01 |         - |          NA |
| Str16_FromJsonUnsafe_LongStr  | Json       |  0.6629 ns | 0.0164 ns | 0.0109 ns |   0.98 |    0.02 |         - |          NA |
| Span_ReadFromJson_ShortStr    | Json       |  1.6212 ns | 0.0322 ns | 0.0192 ns |   2.41 |    0.04 |         - |          NA |
| Span_ReadFromJson_LongStr     | Json       |  1.6503 ns | 0.0281 ns | 0.0186 ns |   2.45 |    0.04 |         - |          NA |
|                               |            |            |           |           |        |         |           |             |
| Str16_SseData                 | RawData    |  0.2622 ns | 0.0129 ns | 0.0085 ns |   1.00 |    0.04 |         - |          NA |
| Str16_Span                    | RawData    |  0.2202 ns | 0.0078 ns | 0.0052 ns |   0.84 |    0.03 |         - |          NA |
|                               |            |            |           |           |        |         |           |             |
| Str16_FromBytesUnsafe         | Read       |  1.6303 ns | 0.0268 ns | 0.0178 ns |   1.00 |    0.01 |         - |          NA |
| Str16_FromBytes               | Read       |  5.0846 ns | 0.0505 ns | 0.0334 ns |   3.12 |    0.04 |         - |          NA |
|                               |            |            |           |           |        |         |           |             |
| Str16_ToLower                 | ToLower    |  0.8600 ns | 0.0122 ns | 0.0072 ns |   1.00 |    0.01 |         - |          NA |
| ManualToLower                 | ToLower    | 12.2919 ns | 0.1157 ns | 0.0766 ns |  14.29 |    0.14 |         - |          NA |
*/
