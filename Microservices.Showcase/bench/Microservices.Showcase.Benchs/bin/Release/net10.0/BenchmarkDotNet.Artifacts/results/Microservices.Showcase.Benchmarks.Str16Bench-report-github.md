```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD Ryzen 7 6800H with Radeon Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.4, X64 NativeAOT x86-64-v3
  Job-PHKGVJ : .NET 10.0.4, X64 NativeAOT x86-64-v3

IterationCount=10  RunStrategy=Throughput  WarmupCount=5  

```
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
