using NaturalSort.Extension;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // https://www.fileformat.info/info/unicode/char/0a68/index.htm
            string[] arrOriginal = { "A", "A10", "A11", "Z", "A\u0a68", "A\u0a68\u0a68", };
            var arrNatSortExt = arrOriginal.OrderBy(x => x, StringComparison.CurrentCultureIgnoreCase.WithNaturalSort()).ToArray();

            var arrWinApiSorted = arrOriginal.OrderBy(x => x, NaturalStringComparer.Instance).ToArray();

            // https://github.com/dotnet/core/blob/main/release-notes/10.0/preview/preview1/libraries.md#numeric-ordering-for-string-comparison
            StringComparer numericStringComparer = StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.NumericOrdering);
            var arrNet10NumbericSorted = arrOriginal.Order(numericStringComparer).ToArray();

            arrOriginal.Dump();
            arrNatSortExt.Dump();
            arrWinApiSorted.Dump();
            arrNet10NumbericSorted.Dump();
        }

        static void Dump(this string[] arr)
        {
            Console.WriteLine(String.Join(", ", arr));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
    public sealed class NaturalStringComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int StrCmpLogicalW(string psz1, string psz2);

        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }
    }
}
