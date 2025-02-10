using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleStuff.ToTest;
using Shouldly;

namespace TestcontainersPlayground;

public class FixturesAndHostFixture : CustomFixtureWithIHost
{
    public FixturesAndHostFixture() : base("testdb")
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        using var sqlDb = await StaticDbOperations.CreateDatabaseAsync(ConnectionString);

        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Services.AddDbContext<SqlServerBloggingContext>(options =>
        {
            options.UseSqlServer(ConnectionString);
        });
        TestHost = hostBuilder.Build();
    }
}

public class FixturesAndHostFixtureTests : CustomTestcontainersFixture<FixturesAndHostFixture>
{
    public FixturesAndHostFixtureTests(FixturesAndHostFixture fixture, ITestOutputHelper output)
    : base(fixture, output)
    {
    }

    [Fact]
    public async Task Create_Db_InTest_AndQuery()
    {
        using (var dbContext = fixture.TestHost.Services.GetRequiredService<SqlServerBloggingContext>())
        {
            int affected = await StaticDbOperations.PerformDbOperationsAsync(dbContext);
            affected.ShouldBe(3);
        }
    }
}