using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Extensions.Logging;

namespace TestcontainersPlayground
{
    public static class ITestOutputHelperExtensions
    {
        public static ILogger CreateILogger(this ITestOutputHelper output)
        {
            return (new XUnitLoggerProvider(output, appendScope: false)).CreateLogger("standardoutputlogger");
        }
    }
}
