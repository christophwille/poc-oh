using SampleStuff.ToTest;
using Shouldly;

namespace TestcontainersPlayground;

public class GoingWithFixturesFixture : CustomFixtureWithIHost
{
    public GoingWithFixturesFixture() : base("testdb")
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        using var sqlDb = await StaticDbOperations.CreateDatabaseAsync(ConnectionString);
    }
}

// TIP: If you batch read tests in a collection, then you can parallelize as much as you want
[CollectionDefinition(nameof(MySequentialTestsCollection), DisableParallelization = true)]
public class MySequentialTestsCollection : ICollectionFixture<GoingWithFixturesFixture>
{
}

[Collection(nameof(MySequentialTestsCollection))]
public class GoingWithFixturesTests : CustomTestcontainersFixture<GoingWithFixturesFixture>
{
    public GoingWithFixturesTests(GoingWithFixturesFixture fixture, ITestOutputHelper output)
    : base(fixture, output)
    {
    }

    // This test will have side effects on the db
    [Fact]
    public async Task Create_Db_InTest_AndQuery()
    {
        var dbCtx = StaticDbOperations.GetContext(fixture.ConnectionString);
        int affected = await StaticDbOperations.PerformDbOperationsAsync(dbCtx);
        affected.ShouldBe(3);
    }

    // TIP: Parallelize even write tests thanks to tx isolation
    [Fact]
    public async Task HowTo_Parallelize_WriteOperations()
    {
        var dbCtx = StaticDbOperations.GetContext(fixture.ConnectionString);

        // Start a transaction, it will be rolled back automatically at the end of the test
        _ = await dbCtx.Database.BeginTransactionAsync();

        int affected = await StaticDbOperations.PerformDbOperationsAsync(dbCtx);
        affected.ShouldBe(3);
    }
}