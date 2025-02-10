using Microsoft.Extensions.Hosting;

namespace TestcontainersPlayground
{
    public class CustomFixtureWithIHost : CustomSqlContainerAsyncLifetime
    {
        public CustomFixtureWithIHost(string targetDbName, ITestOutputHelper? testOutputHelper = null)
            : base(targetDbName, testOutputHelper)
        {
        }

        public IHost? TestHost { get; protected set; } = null;

        public override async ValueTask DisposeAsync()
        {
            if (TestHost != null) TestHost.Dispose();
            await base.DisposeAsync();
        }
    }
}
