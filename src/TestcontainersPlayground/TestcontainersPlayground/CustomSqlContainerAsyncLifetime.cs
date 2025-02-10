using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace TestcontainersPlayground
{
    public class CustomSqlContainerAsyncLifetime : IAsyncLifetime
    {
        protected readonly ILogger? logger;

        private readonly MsSqlContainer _msSqlContainer;
        private readonly string targetDbName;

        public CustomSqlContainerAsyncLifetime(string targetDbName, ITestOutputHelper? testOutputHelper)
        {
            this.targetDbName = targetDbName;
            this.logger = testOutputHelper?.CreateILogger();

            var builder = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-CU15-GDR1-ubuntu-22.04");

            if (logger != null)
                builder = builder.WithLogger(logger);

            _msSqlContainer = builder.Build();
        }

        public virtual async ValueTask InitializeAsync()
        {
            await _msSqlContainer.StartAsync();
        }

        public virtual async ValueTask DisposeAsync()
        {
            await _msSqlContainer.StopAsync();
        }

        public string ConnectionString => _msSqlContainer.GetConnectionString().Replace("Database=master", $"Database={targetDbName}");
        public string TargetDbName => this.targetDbName;

        public IConfiguration GetConnectionStringsConfiguration()
        {
            var testConfiguration = new Dictionary<string, string?>
            {
                {$"ConnectionStrings:{targetDbName}", this.ConnectionString}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(testConfiguration)
                .Build();
        }
    }
}
