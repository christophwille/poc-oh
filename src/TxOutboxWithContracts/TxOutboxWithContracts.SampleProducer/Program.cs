using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TxOutboxWithContracts.Data;
using TxOutboxWithContracts.Messages;

var builder = WebApplication.CreateBuilder(args);

// This is a sample! Run Api before Worker
using var sqlDb = new SqlServerBloggingContext();
// await sqlDb.Database.EnsureDeletedAsync(); // <== this will wipe the entire database
await sqlDb.Database.MigrateAsync();

builder.Services.AddDbContext<SqlServerBloggingContext>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/dbtest", async ([FromServices] SqlServerBloggingContext context) =>
{
    using var transaction = await context.Database.BeginTransactionAsync();

    try
    {
        // Create a new blog
        var blog = new Blog { Url = "http://blogs.msdn.com/adonet" };
        context.Add(blog);
        context.AddOutboxMessage(new BlogCreated { Id = blog.BlogId.ToString(), Url = blog.Url });
        await context.SaveChangesAsync();

        // Modify blog url
        var readBackBlog = await context.Blogs.OrderBy(b => b.BlogId).FirstAsync();
        readBackBlog.Url = "https://devblogs.microsoft.com/dotnet";

        // Add a post to the blog
        var post = new Post { Title = "Hello World", Content = "I wrote an app using EF Core!" };
        readBackBlog.Posts.Add(post);
        context.AddOutboxMessage(new PostCreated { Id = post.PostId.ToString(), Title = post.Title });
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
})
.WithName("DbTest");

app.Run();