using SampleStuff.ToTest;
using Shouldly;

namespace TestcontainersPlayground;

public class NicelyWrappedIAsyncLifetime : CustomSqlContainerAsyncLifetime
{
    public NicelyWrappedIAsyncLifetime(ITestOutputHelper testOutputHelper) : base("testdb", testOutputHelper)
    {
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
