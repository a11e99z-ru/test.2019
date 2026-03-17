```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD Ryzen 7 6800H with Radeon Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
  [Host]     : .NET 10.0.4, X64 NativeAOT x86-64-v3
  Job-PHKGVJ : .NET 10.0.4, X64 NativeAOT x86-64-v3

IterationCount=10  RunStrategy=Throughput  WarmupCount=5  
Categories=Read  

```
| Method          | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
| ReadUnsafe      | 4.962 ns | 0.0750 ns | 0.0496 ns |  1.00 |    0.01 |         - |          NA |
| ReadSafe        | 7.150 ns | 0.0195 ns | 0.0129 ns |  1.44 |    0.01 |         - |          NA |
| ReadSpan        | 7.293 ns | 0.0595 ns | 0.0354 ns |  1.47 |    0.02 |         - |          NA |
| ReadSpan2       | 4.738 ns | 0.0445 ns | 0.0265 ns |  0.96 |    0.01 |         - |          NA |
| ReadJsonUnsafe  | 1.224 ns | 0.0449 ns | 0.0297 ns |  0.25 |    0.01 |         - |          NA |
| ReadJsonUnsafe2 | 1.216 ns | 0.0314 ns | 0.0208 ns |  0.25 |    0.00 |         - |          NA |
