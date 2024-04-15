```
// * Summary *

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3447/23H2/2023Update/SunValley3)
AMD Ryzen 7 PRO 6850U with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.300-preview.24203.14
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2


| Method               | Mean       | Error    | StdDev  |
|--------------------- |-----------:|---------:|--------:|
| SharpTreeViewVariant | 1,649.0 ns | 10.62 ns | 9.41 ns |
| LibVariant           |   734.5 ns | 10.71 ns | 9.49 ns |
| LexicoVariant        |   595.4 ns |  7.87 ns | 7.36 ns |

// * Hints *
Outliers
  NaturalSortBenchmarks.SharpTreeViewVariant: Default -> 1 outlier  was  removed (1.99 us)
  NaturalSortBenchmarks.LibVariant: Default           -> 1 outlier  was  removed (772.77 ns)

// * Legends *
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 ns   : 1 Nanosecond (0.000000001 sec)

// ***** BenchmarkRunner: End *****
Run time: 00:01:00 (60.19 sec), executed benchmarks: 3

Global total time: 00:01:05 (65.98 sec), executed benchmarks: 3
```