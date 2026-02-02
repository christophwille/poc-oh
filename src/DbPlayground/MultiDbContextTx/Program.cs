#define COMPOUNDTX

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiDbContextTx;

var connectionString = "data source=.;initial catalog=multitxsample;MultipleActiveResultSets=True;Trusted_Connection=True;Trust Server Certificate=true";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register SqlConnection as scoped so it can be shared between DbContexts
        services.AddScoped(_ => new SqlConnection(connectionString));

        // Register SqlServerBloggingContext using the injected SqlConnection
        services.AddDbContext<SqlServerBloggingContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SqlConnection>();
            options.UseSqlServer(connection);
        });

        // Register SqlServerEmailContext using the injected SqlConnection
        services.AddDbContext<SqlServerEmailContext>((sp, options) =>
        {
            var connection = sp.GetRequiredService<SqlConnection>();
            options.UseSqlServer(connection);
        });

        services.AddScoped<MultiTxUnitOfWork>();
    })
    .Build();

// Create a scope to resolve scoped services
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var connection = services.GetRequiredService<SqlConnection>();
var bloggingContext = services.GetRequiredService<SqlServerBloggingContext>();
var emailContext = services.GetRequiredService<SqlServerEmailContext>();

await connection.OpenAsync();

await bloggingContext.Database.MigrateAsync();
await emailContext.Database.MigrateAsync();

// Begin a transaction on the shared connection

#if !COMPOUNDTX
    await using var transaction = await connection.BeginTransactionAsync();
#else
var compoundTx = services.GetRequiredService<MultiTxUnitOfWork>();
await using var transaction = await compoundTx.BeginTransactionAsync();
#endif

try
{
#if !COMPOUNDTX
    // Set the transaction on both DbContexts
    await bloggingContext.Database.UseTransactionAsync(transaction);
    await emailContext.Database.UseTransactionAsync(transaction);
#endif

    // Insert one blog with 2 blog entries
    var blog = new Blog
    {
        Url = "https://example.com/blog"
    };
    blog.Posts.Add(new Post { Title = "First Post", Content = "Content of the first post" });
    blog.Posts.Add(new Post { Title = "Second Post", Content = "Content of the second post" });

    bloggingContext.Blogs.Add(blog);
    await bloggingContext.SaveChangesAsync();

    // Insert one email
    var email = new Email
    {
        To = "user@example.com",
        Subject = "Welcome!",
        Body = "Welcome to our blog platform at: " + DateTime.Now.ToString()
    };

    emailContext.Emails.Add(email);
    await emailContext.SaveChangesAsync();

    // Commit the transaction
    await transaction.CommitAsync();

    Console.WriteLine("Transaction committed successfully!");
    Console.WriteLine($"Blog '{blog.Url}' with {blog.Posts.Count} posts and email to '{email.To}' were inserted.");
}
catch (Exception ex)
{
    // Rollback on failure
    await transaction.RollbackAsync();
    Console.WriteLine($"Transaction rolled back due to error: {ex.Message}");
    throw;
}
