using SampleStuff.ToTest;
using Shouldly;
using Testcontainers.MsSql;

namespace TestcontainersPlayground;

public class OneTestWithEFNewDb
{
    // Don't do this - recreating a new container in every test
    [Fact]
    public async Task Do_Everything_In_One_Go()
    {
        var msSqlContainer = new MsSqlBuilder().Build();
        await msSqlContainer.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = msSqlContainer.GetConnectionString().Replace("Database=master", $"Database=testdb");

        using var sqlDb = await StaticDbOperations.CreateDatabaseAsync(connectionString);

        int affected = await StaticDbOperations.PerformDbOperationsAsync(sqlDb);
        affected.ShouldBe(3);

        await msSqlContainer.DisposeAsync();
    }
}
