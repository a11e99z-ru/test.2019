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
[SimpleJob(RunStrategy.Throughput, warmupCount: 5, iterationCount: 10)]
public class Str16Bench
{
    private static readonly byte[] Bytes = "btc-USDT\"........"u8.ToArray();
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
    
    [Benchmark(Baseline = true), BenchmarkCategory("Read")]
    public Str16 Str16_FromBytesUnsafe() 
        => Str16.FromBytesUnsafe(Bytes.AsSpan()[..8]);
    
    [Benchmark, BenchmarkCategory("Read")]
    public Str16 Str16_FromBytes() 
        => Str16.FromBytes(Bytes.AsSpan()[..8]);

    [Benchmark, BenchmarkCategory("Read")]
    public Str16 Str16_FromJsonUnsafe() 
        => Str16.FromJsonUnsafe(Bytes.AsSpan(), out var _);

    [Benchmark, BenchmarkCategory("Read")]
    public int Span_ReadFromJsonTillQuote()
    {
        var sp = Bytes.AsSpan();
        // we expect '\"'
        return sp[..sp.IndexOf((byte)'\"')].Length; // really find Span then it Length
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
}

/* Results on AMD 6800H
| Method                     | Categories | Mean       | Error     | StdDev    | Ratio  | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |----------- |-----------:|----------:|----------:|-------:|--------:|----------:|------------:|
| Str16_GetHashCode          | HashCode   |  0.2355 ns | 0.0099 ns | 0.0066 ns |   1.00 |    0.04 |         - |          NA |
| HashCode_Vec128            | HashCode   | 43.6321 ns | 0.5607 ns | 0.3336 ns | 185.39 |    5.13 |         - |          NA |
| HashCode_CombineTwoInt64   | HashCode   |  5.6751 ns | 0.0257 ns | 0.0153 ns |  24.11 |    0.65 |         - |          NA |
|                            |            |            |           |           |        |         |           |             |
| Str16_SseData              | RawData    |  0.2976 ns | 0.0052 ns | 0.0034 ns |   1.00 |    0.02 |         - |          NA |
| Str16_Span                 | RawData    |  0.2751 ns | 0.0058 ns | 0.0038 ns |   0.92 |    0.02 |         - |          NA |
|                            |            |            |           |           |        |         |           |             |
| Str16_FromBytesUnsafe      | Read       |  1.5710 ns | 0.0313 ns | 0.0207 ns |   1.00 |    0.02 |         - |          NA |
| Str16_FromBytes            | Read       |  4.9357 ns | 0.0286 ns | 0.0189 ns |   3.14 |    0.04 |         - |          NA |
| Str16_FromJsonUnsafe       | Read       |  2.0481 ns | 0.0230 ns | 0.0152 ns |   1.30 |    0.02 |         - |          NA |
| Span_ReadFromJsonTillQuote | Read       |  2.2558 ns | 0.0235 ns | 0.0156 ns |   1.44 |    0.02 |         - |          NA |
|                            |            |            |           |           |        |         |           |             |
| Str16_ToLower              | ToLower    |  0.8453 ns | 0.0055 ns | 0.0029 ns |   1.00 |    0.00 |         - |          NA |
| ManualToLower              | ToLower    | 17.2080 ns | 0.0951 ns | 0.0566 ns |  20.36 |    0.09 |         - |          NA | 
*/
