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

    [Fact]
    public async Task Create_Db_InTest_AndQuery()
    {
        var sqlDb = StaticDbOperations.GetContext(fixture.ConnectionString);
        int affected = await StaticDbOperations.PerformDbOperationsAsync(sqlDb);
        affected.ShouldBe(3);
    }
}