using SampleStuff.ToTest;
using Shouldly;
using Testcontainers.MsSql;

namespace TestcontainersPlayground;

public class AsSimpleAsItGets
{
    // Obviously, don't this - recreating a new container in every test
    [Fact]
    public async Task Do_Everything_In_One_Go()
    {
        var msSqlContainer = new MsSqlBuilder().Build();
        await msSqlContainer.StartAsync(TestContext.Current.CancellationToken);

        var connectionString = msSqlContainer.GetConnectionString().Replace("Database=master", $"Database=testdb");
        using var sqlDb = new SqlServerBloggingContext()
        {
            ConnectionString = connectionString
        };

        await sqlDb.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        sqlDb.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
        int affected = await sqlDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        affected.ShouldBe(1);

        var blog = sqlDb.Blogs
            .OrderBy(b => b.BlogId)
            .First();

        blog.Url = "https://devblogs.microsoft.com/dotnet";
        blog.Posts.Add(new Post { Title = "Hello World", Content = "I wrote an app using EF Core!" });
        affected = await sqlDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        affected.ShouldBe(2);

        await msSqlContainer.DisposeAsync();
    }
}
