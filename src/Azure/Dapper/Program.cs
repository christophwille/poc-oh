using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

using var db = new SqliteBloggingContext();
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();

using var sqlDb = new SqlServerBloggingContext();
await sqlDb.Database.EnsureDeletedAsync();
await sqlDb.Database.EnsureCreatedAsync();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<SqliteBloggingContext>();
builder.Services.AddDbContext<SqlServerBloggingContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapGet("/dbtest", async ([FromServices] SqliteBloggingContext sqliteDb, [FromServices] SqlServerBloggingContext sqlServerDb, HttpContext context) =>
{
    sqlServerDb.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
    await sqlServerDb.SaveChangesAsync();

    var blog = await sqlServerDb.Blogs
        .OrderBy(b => b.BlogId)
        .TagWith("Getting published blog posts")
        .FirstAsync();

    blog.Url = "https://devblogs.microsoft.com/dotnet";
    blog.Posts.Add(new Post { Title = "Hello World", Content = "I wrote an app using EF Core!" });
    await sqlServerDb.SaveChangesAsync();

    // Get a specific blog by primary key
    var builder = new SqlBuilder();
    builder.Select("[BlogId]");
    builder.Select("[Url]");

    DynamicParameters parameters = new DynamicParameters();
    parameters.Add("@blogId", blog.BlogId, DbType.Int32, ParameterDirection.Input);
    builder.Where("[BlogId] = @blogId", parameters);

    var builderTemplate = builder.AddTemplate("Select /**select**/ from [Blogs] /**where**/");

    using (var conn = new SqlConnection("Server=(localdb)\\mssqllocaldb;Database=BloggingDemo;Trusted_Connection=True;MultipleActiveResultSets=true"))
    {
        var dpBlog = await conn.QueryFirstOrDefaultAsync<DapperCustomBlogRecord>(builderTemplate.RawSql, builderTemplate.Parameters);

        // (await conn.QueryAsync<DapperCustomRecord>(sql, parameters)).ToList();

        // Query multiple tables in one go
        string sql = @"
                SELECT BlogId, Url from Blogs;
                SELECT Title, Content from Posts;
                ";

        using (var multi = await conn.QueryMultipleAsync(sql))
        {
            var blogs = (await multi.ReadAsync<DapperCustomBlogRecord>()).ToList();
            var posts = (await multi.ReadAsync<DapperCustomPostRecord>()).ToList();
        }
    }

})
.WithName("DbTest")
.WithOpenApi();

app.Run();

internal record DapperCustomBlogRecord(int BlogId, string Url);
internal record DapperCustomPostRecord(string Title, string Content);