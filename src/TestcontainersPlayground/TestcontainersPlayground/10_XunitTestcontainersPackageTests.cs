using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleStuff.ToTest;
using Shouldly;
using System.Data.Common;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;

namespace TestcontainersPlayground
{
    // This is modified from Testcontainers docs https://dotnet.testcontainers.org/test_frameworks/xunit_net/
    public sealed partial class _10_XunitTestcontainersPackageTests(ITestOutputHelper testOutputHelper)
    : DbContainerTest<PostgreSqlBuilder, PostgreSqlContainer>(testOutputHelper)
    {
        protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
        {
            // https://hub.docker.com/_/postgres
            return builder
                .WithImage("postgres:17.4");
        }

        public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;

        protected override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(continueOnCapturedContext: false);

            var sqlDb = new NpgsqlBloggingContext()
            {
                ConnectionString = this.ConnectionString
            };

            await sqlDb.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task Test1()
        {
            var sqlDb = new NpgsqlBloggingContext()
            {
                ConnectionString = this.ConnectionString
            };

            sqlDb.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
            int affected = await sqlDb.SaveChangesAsync();

            var blog = await sqlDb.Blogs
                .OrderBy(b => b.BlogId)
                .FirstAsync();

            blog.Url.ShouldBe("http://blogs.msdn.com/adonet");
        }
    }
}
