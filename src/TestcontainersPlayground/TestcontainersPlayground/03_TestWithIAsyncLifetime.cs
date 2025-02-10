using SampleStuff.ToTest;
using Shouldly;
using Testcontainers.MsSql;

namespace TestcontainersPlayground;

public class TestWithIAsyncLifetime : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();
    private string ConnectionString
     => _msSqlContainer.GetConnectionString().Replace("Database=master", $"Database=testdb");

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    // Don't do this - if you have multiple tests, guess what will happen...
    [Fact]
    public async Task Create_Db_InTest_AndQuery()
    {
        using var sqlDb = await StaticDbOperations.CreateDatabaseAsync(ConnectionString);

        int affected = await StaticDbOperations.PerformDbOperationsAsync(sqlDb);
        affected.ShouldBe(3);
    }
}
