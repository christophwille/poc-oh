```
// * Summary *

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3374/23H2/2023Update/SunValley3)
AMD Ryzen 7 PRO 6850U with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.202
  [Host]     : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2


| Method               | Mean       | Error    | StdDev   |
|--------------------- |-----------:|---------:|---------:|
| SharpTreeViewVariant | 1,708.8 ns | 18.96 ns | 17.73 ns |
| LibVariant           |   496.6 ns |  9.66 ns |  9.03 ns |

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 ns   : 1 Nanosecond (0.000000001 sec)

// ***** BenchmarkRunner: End *****
Run time: 00:00:36 (36.17 sec), executed benchmarks: 2

Global total time: 00:00:41 (41.31 sec), executed benchmarks: 2
```