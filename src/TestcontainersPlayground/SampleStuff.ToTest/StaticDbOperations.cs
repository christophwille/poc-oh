namespace SampleStuff.ToTest
{
    public static class StaticDbOperations
    {
        public static async Task<SqlServerBloggingContext> CreateDatabaseAsync(string connectionString)
        {
            var sqlDb = new SqlServerBloggingContext()
            {
                ConnectionString = connectionString
            };

            await sqlDb.Database.EnsureCreatedAsync();

            return sqlDb;
        }

        public static SqlServerBloggingContext GetContext(string connectionString)
        {
            var sqlDb = new SqlServerBloggingContext()
            {
                ConnectionString = connectionString
            };

            return sqlDb;
        }

        public static async Task<int> PerformDbOperationsAsync(SqlServerBloggingContext sqlDb)
        {
            sqlDb.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
            int affected = await sqlDb.SaveChangesAsync();

            var blog = sqlDb.Blogs
                .OrderBy(b => b.BlogId)
                .First();

            blog.Url = "https://devblogs.microsoft.com/dotnet";
            blog.Posts.Add(new Post { Title = "Hello World", Content = "I wrote an app using EF Core!" });
            affected += await sqlDb.SaveChangesAsync();

            return affected;
        }
    }
}
