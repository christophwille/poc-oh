using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ICSharpCode.ILSpy.TreeNodes;
using NaturalSort.Extension;

namespace NaturalSortBenchmark
{
    public class NaturalSortBenchmarks
    {
        private readonly string[] folders = { "folder1", "folder5", "folder10", "folder2", "folder3", "folder22", "folder21" };

        [Benchmark]
        public string[] SharpTreeViewVariant() => folders.OrderBy(x => x, NaturalStringComparer.Instance).ToArray();

        [Benchmark]
        public string[] LibVariant() => folders.OrderBy(x => x, StringComparison.OrdinalIgnoreCase.WithNaturalSort()).ToArray();
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<NaturalSortBenchmarks>();
        }
    }
}
