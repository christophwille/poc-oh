```
// * Summary *

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3374/23H2/2023Update/SunValley3)
AMD Ryzen 7 PRO 6850U with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.202
  [Host]     : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2


| Method               | Mean       | Error   | StdDev  |
|--------------------- |-----------:|--------:|--------:|
| SharpTreeViewVariant | 1,670.3 ns | 4.38 ns | 3.42 ns |
| LibVariant           |   483.0 ns | 4.45 ns | 4.16 ns |
| LexicoVariant        |   597.3 ns | 4.67 ns | 4.14 ns |

// * Hints *
Outliers
  NaturalSortBenchmarks.SharpTreeViewVariant: Default -> 3 outliers were removed (1.69 us..1.70 us)
  NaturalSortBenchmarks.LexicoVariant: Default        -> 1 outlier  was  removed (613.67 ns)

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 ns   : 1 Nanosecond (0.000000001 sec)

// ***** BenchmarkRunner: End *****
Run time: 00:00:55 (55.33 sec), executed benchmarks: 3

Global total time: 00:00:59 (59.67 sec), executed benchmarks: 3
```